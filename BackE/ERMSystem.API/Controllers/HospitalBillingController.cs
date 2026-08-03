using System.Security.Claims;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERMSystem.API.Controllers;

[ApiController]
[Route("api/hospital-billing")]
[Authorize]
public class HospitalBillingController : ControllerBase
{
    private readonly IHospitalBillingService _hospitalBillingService;
    private readonly IHospitalPaymentGatewayService _hospitalPaymentGatewayService;

    public HospitalBillingController(
        IHospitalBillingService hospitalBillingService,
        IHospitalPaymentGatewayService hospitalPaymentGatewayService)
    {
        _hospitalBillingService = hospitalBillingService;
        _hospitalPaymentGatewayService = hospitalPaymentGatewayService;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.HospitalBilling.Read)]
    public async Task<IActionResult> GetWorklist([FromQuery] HospitalInvoiceWorklistRequestDto request, CancellationToken ct)
    {
        var result = await _hospitalBillingService.GetWorklistAsync(
            request,
            ResolveCurrentRole(),
            ResolveCurrentUsername(),
            ct);
        return Ok(result);
    }

    [HttpGet("eligible-encounters")]
    [Authorize(Policy = AppPermissions.HospitalBilling.Read)]
    public async Task<IActionResult> GetEligibleEncounters(CancellationToken ct)
    {
        var result = await _hospitalBillingService.GetEligibleEncountersAsync(
            ResolveCurrentRole(),
            ResolveCurrentUsername(),
            ct);
        return Ok(result);
    }

    [HttpGet("encounter-preview/{encounterId:guid}")]
    [Authorize(Policy = AppPermissions.HospitalBilling.Read)]
    public async Task<IActionResult> GetEncounterPreview(Guid encounterId, CancellationToken ct)
    {
        var result = await _hospitalBillingService.GetEncounterPreviewAsync(encounterId, ct);
        if (result == null)
        {
            return NotFound(new { message = "Khong tim thay encounter." });
        }
        return Ok(result);
    }

    [HttpGet("{invoiceId:guid}")]
    [Authorize(Policy = AppPermissions.HospitalBilling.Read)]
    public async Task<IActionResult> GetById(Guid invoiceId, CancellationToken ct)
    {
        var result = await _hospitalBillingService.GetByIdAsync(
            invoiceId,
            ResolveCurrentRole(),
            ResolveCurrentUsername(),
            ct);
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
            var validation = _hospitalPaymentGatewayService.ValidateCallback(new HospitalPaymentGatewayCallbackValidationRequest
            {
                InvoiceId = request.InvoiceId,
                ProviderName = request.GatewayProvider,
                PaymentReference = request.PaymentReference,
                ExternalTransactionId = request.ExternalTransactionId,
                GatewayStatus = request.GatewayStatus,
                Amount = request.Amount,
                GatewayEventId = request.GatewayEventId,
                GatewayTimestampUtc = request.GatewayTimestampUtc,
                Headers = Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase)
            });

            request.GatewayProvider = validation.ResolvedProviderName;
            if (!validation.IsValid)
            {
                return Unauthorized(new { message = validation.FailureReason });
            }

            var result = await _hospitalBillingService.ConfirmPaymentCallbackAsync(
                request,
                null,
                "gateway-webhook",
                false,
                null,
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

    [HttpGet("payment-return")]
    [AllowAnonymous]
    public IActionResult PaymentReturn([FromQuery] PaymentGatewayReturnQuery query)
    {
        var provider = NormalizeQueryValue(query.GatewayProvider)
                       ?? NormalizeQueryValue(query.VnpGatewayProvider)
                       ?? _hospitalPaymentGatewayService.GetDefaultProvider();
        var paymentReference = NormalizeQueryValue(query.PaymentReference)
                               ?? NormalizeQueryValue(query.VnpTxnRef);
        var externalTransactionId = NormalizeQueryValue(query.ExternalTransactionId)
                                    ?? NormalizeQueryValue(query.VnpTransactionNo);
        var gatewayStatus = NormalizeQueryValue(query.GatewayStatus)
                            ?? NormalizeQueryValue(query.VnpResponseCode)
                            ?? NormalizeQueryValue(query.VnpTransactionStatus);

        return Ok(new
        {
            message = "Payment return received. Final payment state is confirmed by signed gateway webhook/IPN.",
            provider,
            query.InvoiceId,
            paymentReference,
            externalTransactionId,
            gatewayStatus,
            amount = query.Amount ?? query.VnpAmount,
            isAuthoritative = false
        });
    }

    [HttpPost("payment-callbacks/simulate")]
    [Authorize(Policy = AppPermissions.HospitalBilling.CollectPayment)]
    public async Task<IActionResult> SimulatePaymentCallback([FromBody] ConfirmHospitalPaymentCallbackDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        request.GatewayProvider ??= _hospitalPaymentGatewayService.GetDefaultProvider();
        request.GatewayEventId ??= $"SIM-{Guid.NewGuid():N}";
        request.GatewayTimestampUtc ??= DateTime.UtcNow;

        try
        {
            var result = await _hospitalBillingService.ConfirmPaymentCallbackAsync(
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                true,
                null,
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
        var result = await _hospitalBillingService.GetReconciliationSummaryAsync(
            ResolveCurrentRole(),
            ResolveCurrentUsername(),
            ct);
        return Ok(result);
    }

    [HttpPost("reconciliation/preview")]
    [Authorize(Policy = AppPermissions.HospitalBilling.Read)]
    public async Task<IActionResult> PreviewReconciliation([FromBody] HospitalPaymentReconciliationPreviewRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalBillingService.PreviewReconciliationAsync(
                request,
                ResolveCurrentRole(),
                ResolveCurrentUsername(),
                ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("reconciliation/apply")]
    [Authorize(Policy = AppPermissions.HospitalBilling.CollectPayment)]
    public async Task<IActionResult> ApplyReconciliation([FromBody] HospitalPaymentReconciliationApplyRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _hospitalBillingService.ApplyReconciliationAsync(
                request,
                ResolveActorUserId(),
                ResolveActorUsername(),
                ResolveCurrentRole(),
                ResolveCurrentUsername(),
                ct);
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
        => User.FindFirstValue(ClaimTypes.Name)
           ?? User.FindFirstValue(ClaimTypes.Upn)
           ?? User.FindFirstValue("unique_name");

    private string ResolveCurrentRole()
        => User.FindFirstValue(ClaimTypes.Role)
           ?? User.FindFirstValue("role")
           ?? string.Empty;

    private string? ResolveCurrentUsername()
        => ResolveActorUsername();

    private static string? NormalizeQueryValue(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

public class PaymentGatewayReturnQuery
{
    public Guid? InvoiceId { get; set; }
    public string? GatewayProvider { get; set; }
    public string? PaymentReference { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string? GatewayStatus { get; set; }
    public decimal? Amount { get; set; }

    [FromQuery(Name = "vnp_GatewayProvider")]
    public string? VnpGatewayProvider { get; set; }

    [FromQuery(Name = "vnp_TxnRef")]
    public string? VnpTxnRef { get; set; }

    [FromQuery(Name = "vnp_TransactionNo")]
    public string? VnpTransactionNo { get; set; }

    [FromQuery(Name = "vnp_ResponseCode")]
    public string? VnpResponseCode { get; set; }

    [FromQuery(Name = "vnp_TransactionStatus")]
    public string? VnpTransactionStatus { get; set; }

    [FromQuery(Name = "vnp_Amount")]
    public decimal? VnpAmount { get; set; }
}
