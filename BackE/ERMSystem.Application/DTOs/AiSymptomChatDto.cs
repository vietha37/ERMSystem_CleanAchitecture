namespace ERMSystem.Application.DTOs;

public class AiSymptomChatRequestDto
{
    public string Message { get; set; } = string.Empty;
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
