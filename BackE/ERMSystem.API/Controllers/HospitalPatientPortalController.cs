using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.Authorization;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMSystem.API.Controllers
{
    [ApiController]
    [Route("api/hospital-patient-portal")]
    [Authorize(Policy = AppPermissions.HospitalPortal.View)]
    public class HospitalPatientPortalController : ControllerBase
    {
        private readonly IHospitalPatientPortalService _service;

        public HospitalPatientPortalController(IHospitalPatientPortalService service)
        {
            _service = service;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyOverview(CancellationToken ct)
        {
            if (!TryResolveCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "Ngu canh nguoi dung khong hop le." });
            }

            var overview = await _service.GetOverviewByUserIdAsync(userId, ct);
            if (overview == null)
            {
                return NotFound(new { message = "Khong tim thay ho so cong thong tin benh nhan." });
            }

            return Ok(overview);
        }

        [HttpGet("me/visit-history")]
        public async Task<IActionResult> GetMyVisitHistory(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            if (!TryResolveCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "Ngu canh nguoi dung khong hop le." });
            }

            var history = await _service.GetVisitHistoryByUserIdAsync(userId, pageNumber, pageSize, ct);
            if (history == null)
            {
                return NotFound(new { message = "Khong tim thay ho so cong thong tin benh nhan." });
            }

            return Ok(history);
        }

        [HttpPost("me/invoices/{invoiceId:guid}/qr-payment-intents")]
        public async Task<IActionResult> CreateMyQrPaymentIntent(
            Guid invoiceId,
            [FromBody] HospitalPatientPortalQrPaymentIntentRequestDto request,
            CancellationToken ct)
        {
            if (!TryResolveCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "Ngu canh nguoi dung khong hop le." });
            }

            try
            {
                var result = await _service.CreateQrPaymentIntentAsync(userId, invoiceId, request ?? new(), ct);
                if (result == null)
                {
                    return NotFound(new { message = "Khong tim thay hoa don cua benh nhan hien tai." });
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("me/qr-payment-callbacks/simulate")]
        public async Task<IActionResult> SimulateMyQrPaymentCallback(
            [FromBody] ConfirmHospitalPaymentCallbackDto request,
            CancellationToken ct)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Du lieu callback thanh toan khong hop le." });
            }

            if (!TryResolveCurrentUserId(out var userId))
            {
                return Unauthorized(new { message = "Ngu canh nguoi dung khong hop le." });
            }

            try
            {
                var result = await _service.ConfirmQrPaymentAsync(userId, request, ct);
                if (result == null)
                {
                    return NotFound(new { message = "Khong tim thay hoa don cua benh nhan hien tai." });
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private bool TryResolveCurrentUserId(out Guid userId)
        {
            var userIdRaw = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(userIdRaw, out userId);
        }
    }
}
