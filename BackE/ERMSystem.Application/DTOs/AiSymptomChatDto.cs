namespace ERMSystem.Application.DTOs;

public class AiSymptomChatRequestDto
{
    /// <summary>Latest message from the user.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional prior turns in the conversation (oldest first).
    /// Each turn has role ("user" | "assistant") and content.
    /// </summary>
    public IReadOnlyList<AiChatTurnDto> History { get; set; } = Array.Empty<AiChatTurnDto>();
}

public class AiChatTurnDto
{
    /// <summary>"user" or "assistant"</summary>
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class AiSymptomChatResponseDto
{
    public string Answer { get; set; } = string.Empty;
    public IReadOnlyList<string> RecommendedSpecialties { get; set; } = Array.Empty<string>();
    public string UrgencyLevel { get; set; } = "unknown";
    public bool AiRuntimeAvailable { get; set; }
    public IReadOnlyList<AiSymptomKnowledgeMatchDto> Matches { get; set; } = Array.Empty<AiSymptomKnowledgeMatchDto>();
}

public class AiSymptomKnowledgeMatchDto
{
    public string Title { get; set; } = string.Empty;
    public string RecommendedSpecialty { get; set; } = string.Empty;
    public IReadOnlyList<string> PossibleConditions { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> EmergencySigns { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> MatchedSymptoms { get; set; } = Array.Empty<string>();
    public double Score { get; set; }
}
