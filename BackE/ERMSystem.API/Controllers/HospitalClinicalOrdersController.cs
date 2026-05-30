using System.Security.Claims;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Authorization;
using ERMSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMSystem.API.Controllers;

[ApiController]
[Route("api/hospital-clinical-orders")]
[Authorize]
public class HospitalClinicalOrdersController : ControllerBase
{
    private readonly IHospitalClinicalOrderService _hospitalClinicalOrderService;

    public HospitalClinicalOrdersController(IHospitalClinicalOrderService hospitalClinicalOrderService)
    {
        _hospitalClinicalOrderService = hospitalClinicalOrderService;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.HospitalClinicalOrders.Read)]
    public async Task<IActionResult> GetWorklist(
        [FromQuery] HospitalClinicalOrderWorklistRequestDto request,
        CancellationToken ct)
    {
        var result = await _hospitalClinicalOrderService.GetWorklistAsync(
            request,
            ResolveCurrentRole(),
            ResolveCurrentUsername(),
            ct);
        return Ok(result);
    }

    [HttpGet("eligible-encounters")]
    [Authorize(Policy = AppPermissions.HospitalClinicalOrders.Read)]
    public async Task<IActionResult> GetEligibleEncounters(CancellationToken ct)
    {
        var result = await _hospitalClinicalOrderService.GetEligibleEncountersAsync(
            ResolveCurrentRole(),
            ResolveCurrentUsername(),
            ct);
        return Ok(result);
    }

    [HttpGet("catalog")]
    [Authorize(Policy = AppPermissions.HospitalClinicalOrders.Read)]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
    {
        var result = await _hospitalClinicalOrderService.GetCatalogAsync(ct);
        return Ok(result);
    }

    [HttpGet("{clinicalOrderId:guid}")]
    [Authorize(Policy = AppPermissions.HospitalClinicalOrders.Read)]
    public async Task<IActionResult> GetById(Guid clinicalOrderId, CancellationToken ct)
    {
        var result = await _hospitalClinicalOrderService.GetByIdAsync(
            clinicalOrderId,
            ResolveCurrentRole(),
            ResolveCurrentUsername(),
            ct);
        if (result == null)
        {
            return NotFound(new { message = "Khong tim thay chi dinh can lam sang." });
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AppPermissions.HospitalClinicalOrders.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateHospitalClinicalOrderDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalClinicalOrderService.CreateAsync(
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                ct);
            return CreatedAtAction(nameof(GetById), new { clinicalOrderId = result.ClinicalOrderId }, result);
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

    [HttpPost("{clinicalOrderId:guid}/lab-result")]
    [Authorize(Policy = AppPermissions.HospitalClinicalOrders.Update)]
    public async Task<IActionResult> RecordLabResult(
        Guid clinicalOrderId,
        [FromBody] RecordHospitalLabResultDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalClinicalOrderService.RecordLabResultAsync(
                clinicalOrderId,
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                ct);

            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay chi dinh xet nghiem." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{clinicalOrderId:guid}/imaging-report")]
    [Authorize(Policy = AppPermissions.HospitalClinicalOrders.Update)]
    public async Task<IActionResult> RecordImagingReport(
        Guid clinicalOrderId,
        [FromBody] RecordHospitalImagingReportDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalClinicalOrderService.RecordImagingReportAsync(
                clinicalOrderId,
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                ct);

            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay chi dinh chan doan hinh anh." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
        => User.FindFirstValue(ClaimTypes.Role)
           ?? User.FindFirstValue("role")
           ?? string.Empty;

    private string? ResolveCurrentUsername()
        => ResolveActorUsername();
}
