using System.Net.Http.Json;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERMSystem.Infrastructure.Services;

public class OllamaSymptomChatService : IAiSymptomChatService
{
    private const string FriendlyUnavailableMessage =
        "Hệ thống AI đang tạm thời chưa sẵn sàng. Bạn vui lòng thử lại sau hoặc đặt lịch để được nhân viên y tế tư vấn trực tiếp.";

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
                Temperature = 0.2,
                TopP = 0.9
            }
        };

        try
        {
            var path = string.IsNullOrWhiteSpace(_options.GeneratePath) ? "/api/generate" : _options.GeneratePath.Trim();
            using var response = await _httpClient.PostAsJsonAsync(path, request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Ollama symptom chat returned {StatusCode}.",
                    (int)response.StatusCode);
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

        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength];
    }

    private IReadOnlyList<SymptomKnowledgeEntry> FindKnowledgeMatches(string message)
    {
        var entries = LoadKnowledgeEntries();
        if (entries.Count == 0)
        {
            return Array.Empty<SymptomKnowledgeEntry>();
        }

        var normalizedMessage = NormalizeForSearch(message);

        return entries
            .Select(entry => new
            {
                Entry = entry,
                Score = entry.Symptoms.Count(symptom =>
                    normalizedMessage.Contains(NormalizeForSearch(symptom), StringComparison.OrdinalIgnoreCase))
            })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Entry.Title)
            .Take(3)
            .Select(match => match.Entry)
            .ToArray();
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

            return entries is null
                ? Array.Empty<SymptomKnowledgeEntry>()
                : entries;
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

        return builder.ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd');
    }

    private static string DetermineUrgencyLevel(string message, IReadOnlyList<SymptomKnowledgeEntry> matches)
    {
        var normalizedMessage = NormalizeForSearch(message);
        var emergencySigns = matches
            .SelectMany(match => match.EmergencySigns)
            .Concat(new[]
            {
                "đau ngực dữ dội",
                "khó thở",
                "ngất",
                "yếu liệt",
                "co giật",
                "chảy máu nhiều",
                "đau bụng dữ dội",
                "tím tái",
                "mất ý thức"
            });

        return emergencySigns.Any(sign =>
            normalizedMessage.Contains(NormalizeForSearch(sign), StringComparison.OrdinalIgnoreCase))
            ? "emergency"
            : matches.Count > 0
                ? "routine"
                : "unknown";
    }

    private static AiSymptomChatResponseDto BuildResponse(
        string answer,
        IReadOnlyList<SymptomKnowledgeEntry> matches,
        string urgencyLevel,
        bool aiRuntimeAvailable)
        => new()
        {
            Answer = answer.Trim(),
            RecommendedSpecialties = matches
                .Select(match => match.RecommendedSpecialty)
                .Where(specialty => !string.IsNullOrWhiteSpace(specialty))
                .Distinct()
                .ToArray(),
            UrgencyLevel = urgencyLevel,
            AiRuntimeAvailable = aiRuntimeAvailable,
            Matches = matches.Select(match => new AiSymptomKnowledgeMatchDto
            {
                Title = match.Title,
                RecommendedSpecialty = match.RecommendedSpecialty,
                PossibleConditions = match.PossibleConditions,
                EmergencySigns = match.EmergencySigns
            }).ToArray()
        };

    private static string BuildKnowledgeContext(IReadOnlyList<SymptomKnowledgeEntry> matches)
    {
        if (matches.Count == 0)
        {
            return "Chưa tìm thấy ngữ cảnh nội bộ phù hợp.";
        }

        return string.Join(
            Environment.NewLine,
            matches.Select((match, index) =>
                $"""
                Ngữ cảnh {index + 1}:
                - Nhóm: {match.Title}
                - Bệnh lý có thể liên quan: {string.Join(", ", match.PossibleConditions)}
                - Chuyên khoa gợi ý: {match.RecommendedSpecialty}
                - Dấu hiệu nguy hiểm: {string.Join(", ", match.EmergencySigns)}
                """));
    }

    private static string BuildFallbackAnswer(IReadOnlyList<SymptomKnowledgeEntry> matches)
    {
        if (matches.Count == 0)
        {
            return """
            Mình chưa tìm thấy nhóm triệu chứng đủ rõ trong cơ sở tri thức nội bộ.

            Bạn có thể mô tả thêm: triệu chứng chính, thời gian xuất hiện, mức độ nặng, tuổi, bệnh nền, thuốc đang dùng và có dấu hiệu nguy hiểm như khó thở, đau ngực, ngất, co giật hay không.

            Thông tin này chỉ mang tính tham khảo và không thay thế chẩn đoán của bác sĩ.
            """;
        }

        var specialties = string.Join(", ", matches.Select(match => match.RecommendedSpecialty).Distinct());
        var possibleConditions = string.Join(", ", matches.SelectMany(match => match.PossibleConditions).Distinct().Take(6));
        var emergencySigns = string.Join(", ", matches.SelectMany(match => match.EmergencySigns).Distinct().Take(6));

        return $"""
        Hệ thống AI đang tạm thời chưa sẵn sàng, nhưng dựa trên cơ sở tri thức nội bộ, triệu chứng của bạn có thể liên quan đến: {possibleConditions}.

        Chuyên khoa nên cân nhắc đặt lịch: {specialties}.

        Nếu có các dấu hiệu như {emergencySigns}, bạn nên đi cấp cứu hoặc liên hệ cơ sở y tế ngay.

        Thông tin này chỉ mang tính tham khảo và không thay thế chẩn đoán của bác sĩ.
        """;
    }

    private static string BuildPrompt(string message, IReadOnlyList<SymptomKnowledgeEntry> matches)
        => $$"""
        Bạn là trợ lý sàng lọc triệu chứng cho website bệnh viện ERM Hospital.
        Chỉ trả lời bằng tiếng Việt.
        Không được khẳng định chẩn đoán cuối cùng.
        Không được kê đơn thuốc, liều dùng, hoặc hướng dẫn tự điều trị nguy hiểm.
        Luôn nói đây chỉ là thông tin tham khảo và bệnh nhân nên đặt lịch khám bác sĩ.

        Nếu có dấu hiệu nguy hiểm như đau ngực, khó thở, ngất, yếu liệt nửa người,
        co giật, chảy máu nhiều, đau bụng dữ dội, sốt cao kéo dài, tím tái,
        hãy ưu tiên khuyên người dùng đi cấp cứu ngay.

        Ngữ cảnh nội bộ từ cơ sở tri thức triệu chứng - chuyên khoa:
        {{BuildKnowledgeContext(matches)}}

        Triệu chứng người dùng:
        {{message}}

        Hãy trả lời ngắn gọn, dễ hiểu theo cấu trúc:
        1. Tóm tắt triệu chứng.
        2. Các nhóm bệnh lý có thể liên quan.
        3. Dấu hiệu nguy hiểm cần đi cấp cứu.
        4. Chuyên khoa phù hợp để đặt lịch.
        5. Khuyến nghị tiếp theo.
        """;

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
