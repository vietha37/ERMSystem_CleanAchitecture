using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERMSystem.API.Controllers;

[ApiController]
[Route("api/ai-symptom-chat")]
[AllowAnonymous]
[EnableRateLimiting("ai-chat-fixed-window")]
public class AiSymptomChatController : ControllerBase
{
    private readonly IAiSymptomChatService _aiSymptomChatService;

    public AiSymptomChatController(IAiSymptomChatService aiSymptomChatService)
    {
        _aiSymptomChatService = aiSymptomChatService;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] AiSymptomChatRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Vui lòng nhập triệu chứng." });
        }

        var history = request.History ?? Array.Empty<AiChatTurnDto>();
        var result = await _aiSymptomChatService.AnalyzeAsync(request.Message, history, ct);
        return Ok(result);
    }
}
