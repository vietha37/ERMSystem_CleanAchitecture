using System.Security.Claims;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Authorization;
using ERMSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMSystem.API.Controllers;

[ApiController]
[Route("api/hospital-prescriptions")]
[Authorize]
public class HospitalPrescriptionsController : ControllerBase
{
    private readonly IHospitalPrescriptionService _hospitalPrescriptionService;

    public HospitalPrescriptionsController(IHospitalPrescriptionService hospitalPrescriptionService)
    {
        _hospitalPrescriptionService = hospitalPrescriptionService;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.Prescriptions.Read)]
    public async Task<IActionResult> GetWorklist(
        [FromQuery] HospitalPrescriptionWorklistRequestDto request,
        CancellationToken ct)
    {
        var result = await _hospitalPrescriptionService.GetWorklistAsync(
            request,
            ResolveCurrentRole(),
            ResolveCurrentUsername(),
            ct);
        return Ok(result);
    }

    [HttpGet("eligible-encounters")]
    [Authorize(Policy = AppPermissions.Prescriptions.Read)]
    public async Task<IActionResult> GetEligibleEncounters(CancellationToken ct)
    {
        var result = await _hospitalPrescriptionService.GetEligibleEncountersAsync(
            ResolveCurrentRole(),
            ResolveCurrentUsername(),
            ct);
        return Ok(result);
    }

    [HttpGet("medicine-catalog")]
    [Authorize(Policy = AppPermissions.Prescriptions.Read)]
    public async Task<IActionResult> GetMedicineCatalog(CancellationToken ct)
    {
        var result = await _hospitalPrescriptionService.GetMedicineCatalogAsync(ct);
        return Ok(result);
    }

    [HttpGet("{prescriptionId:guid}")]
    [Authorize(Policy = AppPermissions.Prescriptions.Read)]
    public async Task<IActionResult> GetById(Guid prescriptionId, CancellationToken ct)
    {
        var result = await _hospitalPrescriptionService.GetByIdAsync(
            prescriptionId,
            ResolveCurrentRole(),
            ResolveCurrentUsername(),
            ct);
        if (result == null)
        {
            return NotFound(new { message = "Khong tim thay don thuoc." });
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AppPermissions.Prescriptions.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateHospitalPrescriptionDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalPrescriptionService.CreateAsync(
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                ct);
            return CreatedAtAction(nameof(GetById), new { prescriptionId = result.PrescriptionId }, result);
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

    [HttpDelete("{prescriptionId:guid}")]
    [Authorize(Policy = AppPermissions.Prescriptions.Delete)]
    public async Task<IActionResult> Delete(Guid prescriptionId, CancellationToken ct)
    {
        try
        {
            await _hospitalPrescriptionService.DeleteAsync(prescriptionId, ct);
            return NoContent();
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

    [HttpPost("{prescriptionId:guid}/dispense")]
    [Authorize(Policy = AppPermissions.Prescriptions.Dispense)]
    public async Task<IActionResult> Dispense(
        Guid prescriptionId,
        [FromBody] DispenseHospitalPrescriptionDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalPrescriptionService.DispenseAsync(
                prescriptionId,
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                ct);

            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay don thuoc." });
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
