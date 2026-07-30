using ERMSystem.Application.DTOs;

namespace ERMSystem.Application.Interfaces;

public interface IAiSymptomChatService
{
    Task<AiSymptomChatResponseDto> AnalyzeAsync(
        string message,
        IReadOnlyList<AiChatTurnDto> history,
        CancellationToken ct);
}
