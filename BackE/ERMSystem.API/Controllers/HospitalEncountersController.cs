using System.Security.Claims;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMSystem.API.Controllers;

[ApiController]
[Route("api/hospital-encounters")]
[Authorize]
public class HospitalEncountersController : ControllerBase
{
    private readonly IHospitalEncounterService _hospitalEncounterService;

    public HospitalEncountersController(IHospitalEncounterService hospitalEncounterService)
    {
        _hospitalEncounterService = hospitalEncounterService;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.HospitalEncounters.Read)]
    public async Task<IActionResult> GetWorklist(
        [FromQuery] HospitalEncounterWorklistRequestDto request,
        CancellationToken ct)
    {
        var result = await _hospitalEncounterService.GetWorklistAsync(
            request,
            ResolveCurrentRole(),
            ResolveCurrentUsername(),
            ct);
        return Ok(result);
    }

    [HttpGet("eligible-appointments")]
    [Authorize(Policy = AppPermissions.HospitalEncounters.Read)]
    public async Task<IActionResult> GetEligibleAppointments(CancellationToken ct)
    {
        var result = await _hospitalEncounterService.GetEligibleAppointmentsAsync(
            ResolveCurrentRole(),
            ResolveCurrentUsername(),
            ct);
        return Ok(result);
    }

    [HttpGet("{encounterId:guid}")]
    [Authorize(Policy = AppPermissions.HospitalEncounters.Read)]
    public async Task<IActionResult> GetById(Guid encounterId, CancellationToken ct)
    {
        var result = await _hospitalEncounterService.GetByIdAsync(
            encounterId,
            ResolveCurrentRole(),
            ResolveCurrentUsername(),
            ct);
        if (result == null)
        {
            return NotFound(new { message = "Khong tim thay encounter." });
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AppPermissions.HospitalEncounters.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateHospitalEncounterDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalEncounterService.CreateAsync(
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                ct);
            return CreatedAtAction(nameof(GetById), new { encounterId = result.EncounterId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{encounterId:guid}")]
    [Authorize(Policy = AppPermissions.HospitalEncounters.Update)]
    public async Task<IActionResult> Update(
        Guid encounterId,
        [FromBody] UpdateHospitalEncounterDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalEncounterService.UpdateAsync(
                encounterId,
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                ct);
            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay encounter can cap nhat." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{encounterId:guid}/approve")]
    [Authorize(Policy = AppPermissions.HospitalEncounters.Update)]
    public async Task<IActionResult> Approve(
        Guid encounterId,
        [FromBody] ApproveHospitalEncounterDto request,
        CancellationToken ct)
    {
        try
        {
            var result = await _hospitalEncounterService.ApproveAsync(
                encounterId,
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                ct);
            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay encounter can duyet." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{encounterId:guid}/sign")]
    [Authorize(Policy = AppPermissions.HospitalEncounters.Update)]
    public async Task<IActionResult> Sign(
        Guid encounterId,
        [FromBody] SignHospitalEncounterDto request,
        CancellationToken ct)
    {
        try
        {
            var result = await _hospitalEncounterService.SignAsync(
                encounterId,
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                ct);
            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay encounter can ky xac nhan." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{encounterId:guid}/attachments")]
    [Authorize(Policy = AppPermissions.HospitalEncounters.Update)]
    public async Task<IActionResult> AddAttachment(
        Guid encounterId,
        [FromBody] AddHospitalEncounterAttachmentDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _hospitalEncounterService.AddAttachmentAsync(
            encounterId,
            request,
            ResolveActorUserId(),
            ResolveActorUsername(),
            ct);

        if (result == null)
        {
            return NotFound(new { message = "Khong tim thay encounter de dinh kem tai lieu." });
        }

        return Ok(result);
    }

    [HttpPost("{encounterId:guid}/attachments/upload")]
    [Authorize(Policy = AppPermissions.HospitalEncounters.Update)]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> UploadAttachment(
        Guid encounterId,
        [FromForm] UploadHospitalEncounterAttachmentForm request,
        CancellationToken ct)
    {
        if (request.File == null || request.File.Length <= 0)
        {
            return BadRequest(new { message = "Vui long chon tep tai lieu de tai len." });
        }

        await using var stream = request.File.OpenReadStream();
        try
        {
            var result = await _hospitalEncounterService.UploadAttachmentAsync(
                encounterId,
                request.DocumentType ?? "EncounterAttachment",
                request.File.FileName,
                request.File.ContentType,
                request.File.Length,
                stream,
                ResolveActorUserId(),
                ResolveActorUsername(),
                ct);

            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay encounter de tai len tai lieu." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{encounterId:guid}/attachments/{attachmentId:guid}/content")]
    [Authorize(Policy = AppPermissions.HospitalEncounters.Read)]
    public async Task<IActionResult> DownloadAttachment(
        Guid encounterId,
        Guid attachmentId,
        CancellationToken ct)
    {
        var content = await _hospitalEncounterService.GetAttachmentContentAsync(encounterId, attachmentId, ct);
        if (content == null)
        {
            return NotFound(new { message = "Khong tim thay noi dung tai lieu dinh kem." });
        }

        return File(content.Content, content.ContentType, content.FileName, enableRangeProcessing: true);
    }

    [HttpGet("{encounterId:guid}/attachments/{attachmentId:guid}/download-ticket")]
    [Authorize(Policy = AppPermissions.HospitalEncounters.Read)]
    public async Task<IActionResult> CreateDownloadTicket(
        Guid encounterId,
        Guid attachmentId,
        CancellationToken ct)
    {
        var result = await _hospitalEncounterService.CreateAttachmentDownloadTicketAsync(encounterId, attachmentId, ct);
        if (result == null)
        {
            return NotFound(new { message = "Khong tim thay tai lieu dinh kem de tao ticket tai xuong." });
        }

        if (string.IsNullOrWhiteSpace(result.DownloadUrl) &&
            !string.IsNullOrWhiteSpace(result.AccessToken))
        {
            result.DownloadUrl =
                Url.ActionLink(nameof(DownloadAttachmentByTicket), values: new { accessToken = result.AccessToken })
                ?? $"/api/hospital-encounters/attachments/download-by-ticket?accessToken={Uri.EscapeDataString(result.AccessToken)}";
        }

        return Ok(result);
    }

    [HttpGet("attachments/download-by-ticket")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadAttachmentByTicket(
        [FromQuery] string accessToken,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return BadRequest(new { message = "Access token cua tai lieu khong hop le." });
        }

        var content = await _hospitalEncounterService.GetAttachmentContentByTicketAsync(accessToken, ct);
        if (content == null)
        {
            return NotFound(new { message = "Download ticket khong hop le hoac da het han." });
        }

        return File(content.Content, content.ContentType, content.FileName, enableRangeProcessing: true);
    }

    private Guid? ResolveActorUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(ClaimTypes.Name)
                     ?? User.FindFirstValue("sub");

        return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
    }

    private string? ResolveActorUsername()
    {
        return User.FindFirstValue(ClaimTypes.Name)
               ?? User.FindFirstValue(ClaimTypes.Upn)
               ?? User.FindFirstValue("unique_name");
    }

    private string ResolveCurrentRole()
        => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    private string? ResolveCurrentUsername()
        => User.FindFirstValue(ClaimTypes.Name)
           ?? User.FindFirstValue(ClaimTypes.Upn)
           ?? User.FindFirstValue("unique_name");

    public sealed class UploadHospitalEncounterAttachmentForm
    {
        public string? DocumentType { get; set; }
        public IFormFile? File { get; set; }
    }
}
