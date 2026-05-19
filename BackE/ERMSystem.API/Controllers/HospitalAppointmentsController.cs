using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMSystem.API.Controllers;

[ApiController]
[Route("api/hospital-appointments")]
public class HospitalAppointmentsController : ControllerBase
{
    private readonly IHospitalAppointmentService _hospitalAppointmentService;

    public HospitalAppointmentsController(IHospitalAppointmentService hospitalAppointmentService)
    {
        _hospitalAppointmentService = hospitalAppointmentService;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.Appointments.Read)]
    public async Task<IActionResult> GetWorklist(
        [FromQuery] HospitalAppointmentWorklistRequestDto request,
        CancellationToken ct)
    {
        var result = await _hospitalAppointmentService.GetWorklistAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("{appointmentId:guid}/check-in")]
    [Authorize(Policy = AppPermissions.Appointments.CheckIn)]
    public async Task<IActionResult> CheckIn(
        Guid appointmentId,
        [FromBody] HospitalAppointmentCheckInRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var result = await _hospitalAppointmentService.CheckInAsync(appointmentId, request, ct);
            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay lich hen can check-in." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{appointmentId:guid}/status")]
    [Authorize(Policy = AppPermissions.Appointments.StatusUpdate)]
    public async Task<IActionResult> UpdateStatus(
        Guid appointmentId,
        [FromBody] HospitalAppointmentStatusUpdateRequestDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalAppointmentService.UpdateStatusAsync(appointmentId, request, ct);
            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay lich hen can cap nhat." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{appointmentId:guid}/cancel")]
    [Authorize(Policy = AppPermissions.Appointments.StatusUpdate)]
    public async Task<IActionResult> Cancel(
        Guid appointmentId,
        [FromBody] HospitalAppointmentCancelRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var result = await _hospitalAppointmentService.CancelAsync(appointmentId, request, ct);
            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay lich hen can huy." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{appointmentId:guid}/reschedule")]
    [Authorize(Policy = AppPermissions.Appointments.StatusUpdate)]
    public async Task<IActionResult> Reschedule(
        Guid appointmentId,
        [FromBody] HospitalAppointmentRescheduleRequestDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalAppointmentService.RescheduleAsync(appointmentId, request, ct);
            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay lich hen can doi lich." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("public-booking")]
    [AllowAnonymous]
    public async Task<IActionResult> BookPublicAppointment(
        [FromBody] PublicHospitalAppointmentBookingRequestDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalAppointmentService.BookPublicAppointmentAsync(request, ct);
            return Ok(result);
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
}
