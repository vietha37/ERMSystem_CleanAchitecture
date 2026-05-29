using System.Security.Claims;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Authorization;
using ERMSystem.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMSystem.API.Controllers;

[ApiController]
[Route("api/hospital-billing")]
[Authorize]
public class HospitalBillingController : ControllerBase
{
    private readonly IHospitalBillingService _hospitalBillingService;
    private readonly PaymentGatewayCallbackVerifier _paymentGatewayCallbackVerifier;

    public HospitalBillingController(
        IHospitalBillingService hospitalBillingService,
        PaymentGatewayCallbackVerifier paymentGatewayCallbackVerifier)
    {
        _hospitalBillingService = hospitalBillingService;
        _paymentGatewayCallbackVerifier = paymentGatewayCallbackVerifier;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.HospitalBilling.Read)]
    public async Task<IActionResult> GetWorklist([FromQuery] HospitalInvoiceWorklistRequestDto request, CancellationToken ct)
    {
        var result = await _hospitalBillingService.GetWorklistAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("eligible-encounters")]
    [Authorize(Policy = AppPermissions.HospitalBilling.Read)]
    public async Task<IActionResult> GetEligibleEncounters(CancellationToken ct)
    {
        var result = await _hospitalBillingService.GetEligibleEncountersAsync(ct);
        return Ok(result);
    }

    [HttpGet("{invoiceId:guid}")]
    [Authorize(Policy = AppPermissions.HospitalBilling.Read)]
    public async Task<IActionResult> GetById(Guid invoiceId, CancellationToken ct)
    {
        var result = await _hospitalBillingService.GetByIdAsync(invoiceId, ct);
        if (result == null)
        {
            return NotFound(new { message = "Khong tim thay hoa don." });
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AppPermissions.HospitalBilling.Create)]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateHospitalInvoiceDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalBillingService.CreateInvoiceAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { invoiceId = result.InvoiceId }, result);
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

    [HttpPost("{invoiceId:guid}/payment-intents")]
    [Authorize(Policy = AppPermissions.HospitalBilling.CollectPayment)]
    public async Task<IActionResult> CreatePaymentIntent(Guid invoiceId, [FromBody] CreateHospitalPaymentIntentDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalBillingService.CreatePaymentIntentAsync(
                invoiceId,
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                ct);
            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay hoa don." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{invoiceId:guid}/payments")]
    [Authorize(Policy = AppPermissions.HospitalBilling.CollectPayment)]
    public async Task<IActionResult> ReceivePayment(Guid invoiceId, [FromBody] ReceiveHospitalPaymentDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalBillingService.ReceivePaymentAsync(
                invoiceId,
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                ct);
            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay hoa don." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{invoiceId:guid}/refunds")]
    [Authorize(Policy = AppPermissions.HospitalBilling.Refund)]
    public async Task<IActionResult> RefundPayment(Guid invoiceId, [FromBody] RefundHospitalPaymentDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalBillingService.RefundPaymentAsync(
                invoiceId,
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                ct);
            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay hoa don." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("payment-callbacks")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmPaymentCallback([FromBody] ConfirmHospitalPaymentCallbackDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            request.GatewayProvider ??= _paymentGatewayCallbackVerifier.GetDefaultProvider();
            if (!_paymentGatewayCallbackVerifier.TryValidate(request, Request.Headers, out var failureReason))
            {
                return Unauthorized(new { message = failureReason });
            }

            var result = await _hospitalBillingService.ConfirmPaymentCallbackAsync(
                request,
                null,
                "gateway-webhook",
                false,
                ct);
            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay hoa don cho callback thanh toan." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("payment-callbacks/simulate")]
    [Authorize(Policy = AppPermissions.HospitalBilling.CollectPayment)]
    public async Task<IActionResult> SimulatePaymentCallback([FromBody] ConfirmHospitalPaymentCallbackDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        request.GatewayProvider ??= _paymentGatewayCallbackVerifier.GetDefaultProvider();
        request.GatewayEventId ??= $"SIM-{Guid.NewGuid():N}";
        request.GatewayTimestampUtc ??= DateTime.UtcNow;

        try
        {
            var result = await _hospitalBillingService.ConfirmPaymentCallbackAsync(
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                true,
                ct);
            if (result == null)
            {
                return NotFound(new { message = "Khong tim thay hoa don cho callback thanh toan." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("reconciliation/summary")]
    [Authorize(Policy = AppPermissions.HospitalBilling.Read)]
    public async Task<IActionResult> GetReconciliationSummary(CancellationToken ct)
    {
        var result = await _hospitalBillingService.GetReconciliationSummaryAsync(ct);
        return Ok(result);
    }

    private Guid? ResolveActorUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(ClaimTypes.Name)
                     ?? User.FindFirstValue("sub");
        return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
    }

    private string? ResolveActorUsername()
        => User.FindFirstValue(ClaimTypes.Name)
           ?? User.FindFirstValue(ClaimTypes.Upn)
           ?? User.FindFirstValue("unique_name");
}
