using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERMSystem.Infrastructure.Services;

public class OllamaSymptomChatService : IAiSymptomChatService
{
    private readonly HttpClient _httpClient;
    private readonly AiSymptomChatOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<OllamaSymptomChatService> _logger;

    public OllamaSymptomChatService(
        HttpClient httpClient,
        IOptions<AiSymptomChatOptions> options,
        IHostEnvironment environment,
        ILogger<OllamaSymptomChatService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _environment = environment;
        _logger = logger;

        if (Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            _httpClient.BaseAddress = baseUri;
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 180));
    }

    public async Task<AiSymptomChatResponseDto> AnalyzeAsync(string message, CancellationToken ct)
    {
        var normalizedMessage = NormalizeMessage(message);
        if (string.IsNullOrWhiteSpace(normalizedMessage))
        {
            throw new ArgumentException("Vui lòng nhập triệu chứng.", nameof(message));
        }

        var matches = FindKnowledgeMatches(normalizedMessage);
        var urgencyLevel = DetermineUrgencyLevel(normalizedMessage, matches);
        if (!_options.Enabled)
        {
            return BuildResponse(BuildFallbackAnswer(matches), matches, urgencyLevel, aiRuntimeAvailable: false);
        }

        var request = new OllamaGenerateRequest
        {
            Model = string.IsNullOrWhiteSpace(_options.Model) ? "llama3.1:8b" : _options.Model.Trim(),
            Prompt = BuildPrompt(normalizedMessage, matches),
            Stream = false,
            Options = new OllamaModelOptions
            {
                Temperature = 0.15,
                TopP = 0.85
            }
        };

        try
        {
            var path = string.IsNullOrWhiteSpace(_options.GeneratePath) ? "/api/generate" : _options.GeneratePath.Trim();
            using var response = await _httpClient.PostAsJsonAsync(path, request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama symptom chat returned {StatusCode}.", (int)response.StatusCode);
                return BuildResponse(BuildFallbackAnswer(matches), matches, urgencyLevel, aiRuntimeAvailable: false);
            }

            var payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: ct);
            var answer = payload?.Response?.Trim();

            return BuildResponse(
                string.IsNullOrWhiteSpace(answer) ? BuildFallbackAnswer(matches) : answer,
                matches,
                urgencyLevel,
                aiRuntimeAvailable: !string.IsNullOrWhiteSpace(answer));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Ollama symptom chat timed out.");
            return BuildResponse(BuildFallbackAnswer(matches), matches, urgencyLevel, aiRuntimeAvailable: false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Cannot reach local Ollama symptom chat runtime.");
            return BuildResponse(BuildFallbackAnswer(matches), matches, urgencyLevel, aiRuntimeAvailable: false);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Cannot parse Ollama symptom chat response.");
            return BuildResponse(BuildFallbackAnswer(matches), matches, urgencyLevel, aiRuntimeAvailable: false);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Ollama symptom chat runtime is not configured correctly.");
            return BuildResponse(BuildFallbackAnswer(matches), matches, urgencyLevel, aiRuntimeAvailable: false);
        }
    }

    private string NormalizeMessage(string message)
    {
        var trimmed = message.Trim();
        var maxLength = _options.MaxInputCharacters <= 0 ? 1200 : _options.MaxInputCharacters;

        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private IReadOnlyList<KnowledgeMatch> FindKnowledgeMatches(string message)
    {
        var entries = LoadKnowledgeEntries();
        if (entries.Count == 0)
        {
            return Array.Empty<KnowledgeMatch>();
        }

        var normalizedMessage = NormalizeForSearch(message);
        var tokens = Tokenize(normalizedMessage);

        var scoredMatches = entries
            .Select(entry => ScoreEntry(entry, normalizedMessage, tokens))
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenByDescending(match => match.MatchedSymptoms.Count)
            .ThenBy(match => match.Entry.Title)
            .ToArray();

        if (scoredMatches.Length == 0)
        {
            return Array.Empty<KnowledgeMatch>();
        }

        var topScore = scoredMatches[0].Score;
        var threshold = topScore >= 6 ? topScore * 0.45 : topScore;

        return scoredMatches
            .Where(match => match.Score >= threshold)
            .Take(5)
            .ToArray();
    }

    private KnowledgeMatch ScoreEntry(
        SymptomKnowledgeEntry entry,
        string normalizedMessage,
        IReadOnlySet<string> messageTokens)
    {
        var matchedSymptoms = new List<string>();
        double score = 0;

        foreach (var symptom in entry.Symptoms)
        {
            var normalizedSymptom = NormalizeForSearch(symptom);
            if (string.IsNullOrWhiteSpace(normalizedSymptom))
            {
                continue;
            }

            if (normalizedMessage.Contains(normalizedSymptom, StringComparison.OrdinalIgnoreCase))
            {
                matchedSymptoms.Add(symptom);
                score += normalizedSymptom.Contains(' ', StringComparison.Ordinal) ? 4 : 2.5;
                continue;
            }

            var symptomTokens = Tokenize(normalizedSymptom);
            if (symptomTokens.Count == 0)
            {
                continue;
            }

            var overlap = symptomTokens.Count(messageTokens.Contains);
            var coverage = overlap / (double)symptomTokens.Count;
            if (coverage >= 0.75)
            {
                matchedSymptoms.Add(symptom);
                score += 1.5 * coverage;
            }
        }

        if (matchedSymptoms.Count >= 2)
        {
            score += 1.5;
        }

        if (matchedSymptoms.Count >= 3)
        {
            score += 2;
        }

        return new KnowledgeMatch(entry, matchedSymptoms.Distinct().ToArray(), Math.Round(score, 2));
    }

    private IReadOnlyList<SymptomKnowledgeEntry> LoadKnowledgeEntries()
    {
        var path = Path.Combine(_environment.ContentRootPath, "App_Data", "ai", "symptom-knowledge.json");
        if (!File.Exists(path))
        {
            return Array.Empty<SymptomKnowledgeEntry>();
        }

        try
        {
            var json = File.ReadAllText(path);
            var entries = JsonSerializer.Deserialize<List<SymptomKnowledgeEntry>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return entries is null ? Array.Empty<SymptomKnowledgeEntry>() : entries;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Cannot parse AI symptom knowledge file.");
            return Array.Empty<SymptomKnowledgeEntry>();
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Cannot read AI symptom knowledge file.");
            return Array.Empty<SymptomKnowledgeEntry>();
        }
    }

    private static string NormalizeForSearch(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd');
    }

    private static IReadOnlySet<string> Tokenize(string value)
        => NormalizeForSearch(value)
            .Split([' ', ',', '.', ';', ':', '/', '\\', '-', '_', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length >= 2)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string DetermineUrgencyLevel(string message, IReadOnlyList<KnowledgeMatch> matches)
    {
        var normalizedMessage = NormalizeForSearch(message);
        var emergencySigns = matches
            .SelectMany(match => match.Entry.EmergencySigns)
            .Concat(new[]
            {
                "đau ngực dữ dội",
                "đau ngực kéo dài",
                "khó thở",
                "ngất",
                "yếu liệt",
                "co giật",
                "chảy máu nhiều",
                "đau bụng dữ dội",
                "tím tái",
                "mất ý thức",
                "nói khó",
                "méo miệng",
                "bí tiểu",
                "nôn ra máu"
            });

        return emergencySigns.Any(sign =>
            normalizedMessage.Contains(NormalizeForSearch(sign), StringComparison.OrdinalIgnoreCase))
            ? "emergency"
            : matches.Count > 0 ? "routine" : "unknown";
    }

    private static AiSymptomChatResponseDto BuildResponse(
        string answer,
        IReadOnlyList<KnowledgeMatch> matches,
        string urgencyLevel,
        bool aiRuntimeAvailable)
        => new()
        {
            Answer = answer.Trim(),
            RecommendedSpecialties = matches
                .Select(match => match.Entry.RecommendedSpecialty)
                .Where(specialty => !string.IsNullOrWhiteSpace(specialty))
                .Distinct()
                .ToArray(),
            UrgencyLevel = urgencyLevel,
            AiRuntimeAvailable = aiRuntimeAvailable,
            Matches = matches.Select(match => new AiSymptomKnowledgeMatchDto
            {
                Title = match.Entry.Title,
                RecommendedSpecialty = match.Entry.RecommendedSpecialty,
                PossibleConditions = match.Entry.PossibleConditions,
                EmergencySigns = match.Entry.EmergencySigns,
                MatchedSymptoms = match.MatchedSymptoms,
                Score = match.Score
            }).ToArray()
        };

    private static string BuildKnowledgeContext(IReadOnlyList<KnowledgeMatch> matches)
    {
        if (matches.Count == 0)
        {
            return "Chưa tìm thấy ngữ cảnh nội bộ phù hợp.";
        }

        return string.Join(
            Environment.NewLine,
            matches.Select((match, index) =>
                $"""
                Khả năng {index + 1}:
                - Nhóm bệnh lý: {match.Entry.Title}
                - Triệu chứng đã khớp: {FormatList(match.MatchedSymptoms)}
                - Điểm phù hợp nội bộ: {match.Score}
                - Bệnh lý có thể liên quan: {FormatList(match.Entry.PossibleConditions)}
                - Chuyên khoa gợi ý: {match.Entry.RecommendedSpecialty}
                - Dấu hiệu nguy hiểm: {FormatList(match.Entry.EmergencySigns)}
                """));
    }

    private static string BuildFallbackAnswer(IReadOnlyList<KnowledgeMatch> matches)
    {
        if (matches.Count == 0)
        {
            return """
            Mình chưa có đủ dữ kiện để gợi ý nhóm bệnh lý phù hợp.

            Bạn hãy mô tả thêm: triệu chứng chính, thời gian xuất hiện, mức độ nặng, tuổi, bệnh nền, thuốc đang dùng và có dấu hiệu nguy hiểm như khó thở, đau ngực, ngất, co giật hay không.

            Thông tin này chỉ mang tính tham khảo và không thay thế chẩn đoán của bác sĩ.
            """;
        }

        var topMatches = matches.Take(3).ToArray();
        var builder = new StringBuilder();
        builder.AppendLine("Dựa trên triệu chứng bạn mô tả, các khả năng cần nghĩ tới gồm:");
        builder.AppendLine();

        for (var index = 0; index < topMatches.Length; index++)
        {
            var match = topMatches[index];
            builder
                .Append(index + 1)
                .Append(". ")
                .Append(match.Entry.Title)
                .Append(" - phù hợp vì có: ")
                .Append(FormatList(match.MatchedSymptoms))
                .Append(". Có thể liên quan đến: ")
                .Append(FormatList(match.Entry.PossibleConditions))
                .AppendLine(".");
        }

        builder.AppendLine();
        builder
            .Append("Chuyên khoa nên cân nhắc đặt lịch: ")
            .Append(FormatList(topMatches.Select(match => match.Entry.RecommendedSpecialty).Distinct()))
            .AppendLine(".");

        var emergencySigns = topMatches
            .SelectMany(match => match.Entry.EmergencySigns)
            .Distinct()
            .Take(8)
            .ToArray();

        builder.AppendLine();
        builder
            .Append("Nếu có các dấu hiệu như ")
            .Append(FormatList(emergencySigns))
            .AppendLine(", bạn nên đi cấp cứu hoặc liên hệ cơ sở y tế ngay.");

        builder.AppendLine();
        builder.Append("Thông tin này chỉ mang tính tham khảo và không thay thế chẩn đoán của bác sĩ.");

        return builder.ToString();
    }

    private static string BuildPrompt(string message, IReadOnlyList<KnowledgeMatch> matches)
        => $$"""
        Bạn là trợ lý sàng lọc triệu chứng cho website bệnh viện ERM Hospital.
        Chỉ trả lời bằng tiếng Việt.
        Không được khẳng định chẩn đoán cuối cùng.
        Không được kê đơn thuốc, liều dùng, hoặc hướng dẫn tự điều trị nguy hiểm.
        Luôn nói đây chỉ là thông tin tham khảo và bệnh nhân nên đặt lịch khám bác sĩ.

        Nhiệm vụ:
        - Ưu tiên ngữ cảnh nội bộ bên dưới.
        - Đưa ra 2-4 khả năng bệnh lý theo mức phù hợp, không nói chắc chắn.
        - Nêu ngắn gọn lý do khớp triệu chứng.
        - Nêu dấu hiệu nguy hiểm cần đi cấp cứu.
        - Gợi ý chuyên khoa phù hợp.
        - Nếu dữ kiện thiếu, hỏi 2-3 câu làm rõ.

        Ngữ cảnh nội bộ từ cơ sở tri thức triệu chứng - chuyên khoa:
        {{BuildKnowledgeContext(matches)}}

        Triệu chứng người dùng:
        {{message}}

        Hãy trả lời theo cấu trúc:
        1. Tóm tắt triệu chứng.
        2. Khả năng bệnh lý có thể liên quan.
        3. Dấu hiệu nguy hiểm cần đi cấp cứu.
        4. Chuyên khoa phù hợp để đặt lịch.
        5. Câu hỏi cần làm rõ hoặc khuyến nghị tiếp theo.
        """;

    private static string FormatList(IEnumerable<string> values)
    {
        var items = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToArray();

        return items.Length == 0 ? "chưa rõ" : string.Join(", ", items);
    }

    private sealed record KnowledgeMatch(
        SymptomKnowledgeEntry Entry,
        IReadOnlyList<string> MatchedSymptoms,
        double Score);

    private sealed class SymptomKnowledgeEntry
    {
        public string Title { get; set; } = string.Empty;
        public IReadOnlyList<string> Symptoms { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> PossibleConditions { get; set; } = Array.Empty<string>();
        public string RecommendedSpecialty { get; set; } = string.Empty;
        public IReadOnlyList<string> EmergencySigns { get; set; } = Array.Empty<string>();
    }

    private sealed class OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("options")]
        public OllamaModelOptions Options { get; set; } = new();
    }

    private sealed class OllamaModelOptions
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("top_p")]
        public double TopP { get; set; }
    }

    private sealed class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}
