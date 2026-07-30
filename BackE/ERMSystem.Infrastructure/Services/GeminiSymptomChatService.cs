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

public class GeminiSymptomChatService : IAiSymptomChatService
{
    private readonly HttpClient _httpClient;
    private readonly AiSymptomChatOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GeminiSymptomChatService> _logger;

    private const string GeminiBaseUrl = "https://generativelanguage.googleapis.com";

    // System instruction sent once — Gemini will follow this throughout the conversation
    private const string SystemInstruction =
        """
        Bạn là trợ lý y tế AI của ERM Hospital, chuyên sàng lọc triệu chứng và hướng dẫn bệnh nhân.

        QUAN TRỌNG - Quy tắc bắt buộc:
        1. Chỉ trả lời bằng tiếng Việt, rõ ràng, dễ hiểu.
        2. Phân tích ĐÚNG triệu chứng người dùng mô tả — không suy diễn sai.
        3. KHÔNG khẳng định chẩn đoán cuối cùng; luôn nói "có thể", "khả năng".
        4. KHÔNG kê đơn thuốc, liều dùng cụ thể hoặc hướng dẫn tự điều trị nguy hiểm.
        5. Luôn khuyến nghị đặt lịch khám bác sĩ.
        6. Nếu dữ kiện không đủ, hỏi tối đa 2-3 câu để làm rõ.
        7. Nhớ toàn bộ lịch sử cuộc trò chuyện để trả lời có ngữ cảnh.

        Cấu trúc mỗi câu trả lời (dùng khi phân tích triệu chứng):
        **Tóm tắt:** [tóm tắt ngắn triệu chứng]
        **Khả năng bệnh lý:** [2-4 khả năng, không nói chắc]
        **Dấu hiệu nguy hiểm:** [cần đi cấp cứu ngay nếu có]
        **Chuyên khoa gợi ý:** [chuyên khoa phù hợp]
        **Lưu ý:** [khuyến nghị tiếp theo hoặc câu hỏi làm rõ]

        Nếu người dùng hỏi chủ đề không liên quan y tế, từ chối lịch sự và hướng về tư vấn sức khỏe.
        """;

    public GeminiSymptomChatService(
        HttpClient httpClient,
        IOptions<AiSymptomChatOptions> options,
        IHostEnvironment environment,
        ILogger<GeminiSymptomChatService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _options.ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? Environment.GetEnvironmentVariable("AiSymptomChat__ApiKey")
                ?? string.Empty;
        }
        _environment = environment;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(GeminiBaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 120));
    }

    public async Task<AiSymptomChatResponseDto> AnalyzeAsync(
        string message,
        IReadOnlyList<AiChatTurnDto> history,
        CancellationToken ct)
    {
        var normalizedMessage = NormalizeMessage(message);
        if (string.IsNullOrWhiteSpace(normalizedMessage))
            throw new ArgumentException("Vui lòng nhập triệu chứng.", nameof(message));

        // Knowledge base is used for metadata (urgency, specialties) regardless of AI provider
        var matches = FindKnowledgeMatches(normalizedMessage);
        var urgencyLevel = DetermineUrgencyLevel(normalizedMessage, matches);

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return BuildResponse(BuildFallbackAnswer(matches), matches, urgencyLevel, aiRuntimeAvailable: false);
        }

        var modelName = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-2.0-flash-lite" : _options.Model.Trim();
        var requestPath = $"/v1beta/models/{modelName}:generateContent?key={_options.ApiKey}";

        // Build conversation contents (history + current message)
        var contents = BuildContents(history, normalizedMessage, matches);

        var geminiRequest = new GeminiRequest
        {
            SystemInstruction = new GeminiSystemInstruction
            {
                Parts = [new GeminiPart { Text = SystemInstruction }]
            },
            Contents = contents,
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.4f,
                TopP = 0.9f,
                MaxOutputTokens = 1536
            }
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(requestPath, geminiRequest, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Gemini API returned {StatusCode}: {Body}",
                    (int)response.StatusCode,
                    errorBody.Length > 300 ? errorBody[..300] : errorBody);

                return BuildResponse(BuildFallbackAnswer(matches), matches, urgencyLevel, aiRuntimeAvailable: false);
            }

            var payload = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: ct);
            var answer = payload?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(answer))
            {
                return BuildResponse(BuildFallbackAnswer(matches), matches, urgencyLevel, aiRuntimeAvailable: false);
            }

            // Re-evaluate urgency from AI answer if it mentions emergency signs
            var effectiveUrgency = urgencyLevel == "unknown"
                ? DetermineUrgencyFromText(answer)
                : urgencyLevel;

            return BuildResponse(answer, matches, effectiveUrgency, aiRuntimeAvailable: true);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Gemini API request timed out.");
            return BuildResponse(BuildFallbackAnswer(matches), matches, urgencyLevel, aiRuntimeAvailable: false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Cannot reach Gemini API endpoint.");
            return BuildResponse(BuildFallbackAnswer(matches), matches, urgencyLevel, aiRuntimeAvailable: false);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Cannot parse Gemini API response.");
            return BuildResponse(BuildFallbackAnswer(matches), matches, urgencyLevel, aiRuntimeAvailable: false);
        }
    }

    // ── Content builder ───────────────────────────────────────────────────────

    private static List<GeminiContent> BuildContents(
        IReadOnlyList<AiChatTurnDto> history,
        string currentMessage,
        IReadOnlyList<KnowledgeMatch> matches)
    {
        var contents = new List<GeminiContent>();

        // Add prior conversation turns (skip greeting messages and very short assistant msgs)
        foreach (var turn in history.TakeLast(10)) // max 10 turns to stay within token limits
        {
            var geminiRole = turn.Role == "user" ? "user" : "model";
            if (string.IsNullOrWhiteSpace(turn.Content)) continue;

            contents.Add(new GeminiContent
            {
                Role = geminiRole,
                Parts = [new GeminiPart { Text = turn.Content }]
            });
        }

        // Build the current user message — attach knowledge base context if relevant
        var userText = matches.Count > 0
            ? $"""
               [Ngữ cảnh tri thức nội bộ ERM Hospital - chỉ dùng khi phù hợp với triệu chứng thực tế:]
               {BuildKnowledgeContext(matches)}

               ---
               Câu hỏi từ người dùng: {currentMessage}
               """
            : currentMessage;

        contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = [new GeminiPart { Text = userText }]
        });

        return contents;
    }

    // ── Knowledge base ────────────────────────────────────────────────────────

    private IReadOnlyList<KnowledgeMatch> FindKnowledgeMatches(string message)
    {
        var entries = LoadKnowledgeEntries();
        if (entries.Count == 0) return Array.Empty<KnowledgeMatch>();

        var normalizedMessage = NormalizeForSearch(message);
        var tokens = Tokenize(normalizedMessage);

        var scoredMatches = entries
            .Select(entry => ScoreEntry(entry, normalizedMessage, tokens))
            .Where(match => match.Score >= 2.0) // raise threshold to reduce false positives
            .OrderByDescending(match => match.Score)
            .ThenByDescending(match => match.MatchedSymptoms.Count)
            .ThenBy(match => match.Entry.Title)
            .ToArray();

        if (scoredMatches.Length == 0) return Array.Empty<KnowledgeMatch>();

        var topScore = scoredMatches[0].Score;
        var threshold = topScore >= 6 ? topScore * 0.5 : topScore * 0.7;

        return scoredMatches
            .Where(match => match.Score >= threshold)
            .Take(4)
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
            if (string.IsNullOrWhiteSpace(normalizedSymptom)) continue;

            // Require minimum symptom phrase length to reduce noise
            if (normalizedSymptom.Length < 3) continue;

            if (normalizedMessage.Contains(normalizedSymptom, StringComparison.OrdinalIgnoreCase))
            {
                matchedSymptoms.Add(symptom);
                // Multi-word phrases score higher
                score += normalizedSymptom.Contains(' ', StringComparison.Ordinal) ? 4.5 : 2.5;
                continue;
            }

            var symptomTokens = Tokenize(normalizedSymptom);
            if (symptomTokens.Count == 0) continue;

            // Require higher token overlap to reduce false positives
            var overlap = symptomTokens.Count(t => messageTokens.Contains(t) && t.Length >= 3);
            var coverage = overlap / (double)symptomTokens.Count;
            if (coverage >= 0.85)
            {
                matchedSymptoms.Add(symptom);
                score += 1.5 * coverage;
            }
        }

        if (matchedSymptoms.Count >= 2) score += 1.5;
        if (matchedSymptoms.Count >= 3) score += 2.0;

        return new KnowledgeMatch(entry, matchedSymptoms.Distinct().ToArray(), Math.Round(score, 2));
    }

    private IReadOnlyList<SymptomKnowledgeEntry> LoadKnowledgeEntries()
    {
        var path = Path.Combine(_environment.ContentRootPath, "App_Data", "ai", "symptom-knowledge.json");
        if (!File.Exists(path)) return Array.Empty<SymptomKnowledgeEntry>();

        try
        {
            var json = File.ReadAllText(path);
            var entries = JsonSerializer.Deserialize<List<SymptomKnowledgeEntry>>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return (IReadOnlyList<SymptomKnowledgeEntry>?)entries ?? Array.Empty<SymptomKnowledgeEntry>();
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

    // ── Text helpers ──────────────────────────────────────────────────────────

    private string NormalizeMessage(string message)
    {
        var trimmed = message.Trim();
        var maxLength = _options.MaxInputCharacters <= 0 ? 1200 : _options.MaxInputCharacters;
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string NormalizeForSearch(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd');
    }

    private static IReadOnlySet<string> Tokenize(string value)
        => NormalizeForSearch(value)
            .Split([' ', ',', '.', ';', ':', '/', '\\', '-', '_', '\r', '\n', '\t'],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 3) // raise minimum token length
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string DetermineUrgencyLevel(string message, IReadOnlyList<KnowledgeMatch> matches)
    {
        var normalizedMessage = NormalizeForSearch(message);
        var emergencySigns = matches
            .SelectMany(m => m.Entry.EmergencySigns)
            .Concat([
                "đau ngực dữ dội", "đau ngực kéo dài", "khó thở", "ngất",
                "yếu liệt", "co giật", "chảy máu nhiều", "đau bụng dữ dội",
                "tím tái", "mất ý thức", "nói khó", "méo miệng", "bí tiểu", "nôn ra máu"
            ]);

        return emergencySigns.Any(sign =>
            normalizedMessage.Contains(NormalizeForSearch(sign), StringComparison.OrdinalIgnoreCase))
            ? "emergency"
            : matches.Count > 0 ? "routine" : "unknown";
    }

    private static string DetermineUrgencyFromText(string aiAnswer)
    {
        var normalized = NormalizeForSearch(aiAnswer);
        var emergencyKeywords = new[] { "cap cuu", "khan cap", "nguy hiem", "nguy kich", "kho tho", "dau nguc" };
        return emergencyKeywords.Any(k => normalized.Contains(k, StringComparison.OrdinalIgnoreCase))
            ? "emergency"
            : "routine";
    }

    // ── Response builders ─────────────────────────────────────────────────────

    private static AiSymptomChatResponseDto BuildResponse(
        string answer,
        IReadOnlyList<KnowledgeMatch> matches,
        string urgencyLevel,
        bool aiRuntimeAvailable)
        => new()
        {
            Answer = answer.Trim(),
            RecommendedSpecialties = matches
                .Select(m => m.Entry.RecommendedSpecialty)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct().ToArray(),
            UrgencyLevel = urgencyLevel,
            AiRuntimeAvailable = aiRuntimeAvailable,
            Matches = matches.Select(m => new AiSymptomKnowledgeMatchDto
            {
                Title = m.Entry.Title,
                RecommendedSpecialty = m.Entry.RecommendedSpecialty,
                PossibleConditions = m.Entry.PossibleConditions,
                EmergencySigns = m.Entry.EmergencySigns,
                MatchedSymptoms = m.MatchedSymptoms,
                Score = m.Score
            }).ToArray()
        };

    private static string BuildKnowledgeContext(IReadOnlyList<KnowledgeMatch> matches)
    {
        if (matches.Count == 0) return "Không có ngữ cảnh nội bộ phù hợp.";
        return string.Join(Environment.NewLine, matches.Select((m, i) =>
            $"""
            [{i + 1}] {m.Entry.Title} (điểm: {m.Score})
            - Triệu chứng khớp: {FormatList(m.MatchedSymptoms)}
            - Bệnh liên quan: {FormatList(m.Entry.PossibleConditions)}
            - Chuyên khoa: {m.Entry.RecommendedSpecialty}
            - Dấu hiệu nguy hiểm: {FormatList(m.Entry.EmergencySigns)}
            """));
    }

    private static string BuildFallbackAnswer(IReadOnlyList<KnowledgeMatch> matches)
    {
        if (matches.Count == 0)
        {
            return """
            Mình chưa có đủ dữ kiện để gợi ý nhóm bệnh lý phù hợp.

            Bạn hãy mô tả thêm: **triệu chứng chính**, **thời gian xuất hiện**, **mức độ nặng**, tuổi, bệnh nền và có dấu hiệu nguy hiểm như khó thở, đau ngực, ngất, co giật không.

            ⚠️ Thông tin này chỉ mang tính tham khảo và không thay thế chẩn đoán của bác sĩ.
            """;
        }

        var top = matches.Take(3).ToArray();
        var sb = new StringBuilder();
        sb.AppendLine("Dựa trên triệu chứng bạn mô tả, các khả năng cần nghĩ tới:");
        sb.AppendLine();
        for (var i = 0; i < top.Length; i++)
        {
            var m = top[i];
            sb.Append(i + 1).Append(". **").Append(m.Entry.Title).Append("**")
              .Append(" — khớp triệu chứng: ").Append(FormatList(m.MatchedSymptoms))
              .Append(". Có thể liên quan: ").Append(FormatList(m.Entry.PossibleConditions)).AppendLine(".");
        }
        sb.AppendLine();
        sb.Append("**Chuyên khoa gợi ý:** ").AppendLine(FormatList(top.Select(m => m.Entry.RecommendedSpecialty).Distinct()));
        var emergency = top.SelectMany(m => m.Entry.EmergencySigns).Distinct().Take(6).ToArray();
        if (emergency.Length > 0)
        {
            sb.AppendLine();
            sb.Append("⚠️ Nếu có dấu hiệu: ").Append(FormatList(emergency)).AppendLine(" — hãy đến cấp cứu ngay.");
        }
        sb.AppendLine();
        sb.Append("_Thông tin chỉ mang tính tham khảo. Vui lòng đặt lịch khám để được chẩn đoán chính xác._");
        return sb.ToString();
    }

    private static string FormatList(IEnumerable<string> values)
    {
        var items = values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToArray();
        return items.Length == 0 ? "chưa rõ" : string.Join(", ", items);
    }

    // ── Private model types ───────────────────────────────────────────────────

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

    // ── Gemini API models ─────────────────────────────────────────────────────

    private sealed class GeminiRequest
    {
        [JsonPropertyName("system_instruction")]
        public GeminiSystemInstruction? SystemInstruction { get; set; }

        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = [];

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private sealed class GeminiSystemInstruction
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        [JsonPropertyName("topP")]
        public float TopP { get; set; }

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; }
    }

    private sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }
}
