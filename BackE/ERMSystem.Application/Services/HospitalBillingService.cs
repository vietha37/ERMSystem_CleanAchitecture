using System.Text.Json;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Utilities;

namespace ERMSystem.Application.Services;

public class HospitalBillingService : IHospitalBillingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHospitalBillingRepository _hospitalBillingRepository;
    private readonly IHospitalIdentityBridgeService _hospitalIdentityBridgeService;
    private readonly IHospitalPaymentGatewayService _hospitalPaymentGatewayService;
    private readonly IBusinessMetricsRecorder _businessMetricsRecorder;
    private readonly IComplianceAuditRecorder _complianceAuditRecorder;
    private readonly IDashboardQueryCache _dashboardQueryCache;
    private readonly IHospitalDoctorWorklistRepository _hospitalDoctorWorklistRepository;

    public HospitalBillingService(
        IHospitalBillingRepository hospitalBillingRepository,
        IHospitalIdentityBridgeService hospitalIdentityBridgeService,
        IHospitalPaymentGatewayService hospitalPaymentGatewayService,
        IBusinessMetricsRecorder businessMetricsRecorder,
        IComplianceAuditRecorder complianceAuditRecorder,
        IDashboardQueryCache dashboardQueryCache,
        IHospitalDoctorWorklistRepository hospitalDoctorWorklistRepository)
    {
        _hospitalBillingRepository = hospitalBillingRepository;
        _hospitalIdentityBridgeService = hospitalIdentityBridgeService;
        _hospitalPaymentGatewayService = hospitalPaymentGatewayService;
        _businessMetricsRecorder = businessMetricsRecorder;
        _complianceAuditRecorder = complianceAuditRecorder;
        _dashboardQueryCache = dashboardQueryCache;
        _hospitalDoctorWorklistRepository = hospitalDoctorWorklistRepository;
    }

    public Task<PaginatedResult<HospitalInvoiceSummaryDto>> GetWorklistAsync(
        HospitalInvoiceWorklistRequestDto request,
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default)
        => GetScopedWorklistAsync(request, currentRole, currentUsername, ct);

    public async Task<HospitalInvoiceDetailDto?> GetByIdAsync(
        Guid invoiceId,
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default)
    {
        var invoice = await _hospitalBillingRepository.GetByIdAsync(invoiceId, ct);
        if (invoice == null)
        {
            return null;
        }

        if (!await CanAccessDoctorScopedDataAsync(invoice.DoctorProfileId, currentRole, currentUsername, ct))
        {
            return null;
        }

        return MapDetail(invoice);
    }

    public async Task<HospitalBillingEligibleEncounterDto[]> GetEligibleEncountersAsync(string currentRole, string? currentUsername, CancellationToken ct = default)
    {
        var doctorProfileId = await ResolveScopedDoctorProfileIdAsync(currentRole, currentUsername, ct);
        return await _hospitalBillingRepository.GetEligibleEncountersAsync(doctorProfileId, ct);
    }

    public Task<HospitalBillingEncounterSnapshot?> GetEncounterPreviewAsync(Guid encounterId, CancellationToken ct = default)
    {
        return _hospitalBillingRepository.GetEncounterForInvoiceAsync(encounterId, ct);
    }

    public async Task<HospitalInvoiceDetailDto> CreateInvoiceAsync(CreateHospitalInvoiceDto request, CancellationToken ct = default)
    {
        var encounter = await _hospitalBillingRepository.GetEncounterForInvoiceAsync(request.EncounterId, ct)
            ?? throw new KeyNotFoundException("Khong tim thay encounter de lap hoa don.");

        if (encounter.ExistingInvoiceId.HasValue)
        {
            throw new InvalidOperationException("Encounter nay da co hoa don.");
        }

        if (encounter.BillableLines.Length == 0)
        {
            throw new InvalidOperationException("Encounter nay chua co dich vu nao de lap hoa don.");
        }

        var subtotal = encounter.BillableLines.Sum(x => x.LineAmount);
        var discount = Math.Max(0, request.DiscountAmount);
        var insurance = 0m;
        var total = Math.Max(0, subtotal - discount);
        var nowUtc = DateTime.UtcNow;
        var invoiceId = Guid.NewGuid();

        await _hospitalBillingRepository.AddInvoiceAsync(new HospitalInvoiceCreateCommand
        {
            InvoiceId = invoiceId,
            InvoiceNumber = GenerateInvoiceNumber(nowUtc),
            PatientId = encounter.PatientId,
            EncounterId = encounter.EncounterId,
            InvoiceStatus = "Issued",
            CurrencyCode = "VND",
            SubtotalAmount = subtotal,
            DiscountAmount = discount,
            InsuranceAmount = insurance,
            TotalAmount = total,
            IssuedAtUtc = nowUtc
        }, ct);

        foreach (var line in encounter.BillableLines)
        {
            await _hospitalBillingRepository.AddInvoiceItemAsync(new HospitalInvoiceItemCreateCommand
            {
                InvoiceItemId = Guid.NewGuid(),
                InvoiceId = invoiceId,
                ServiceCatalogId = line.ServiceCatalogId,
                ItemType = line.ItemType,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineAmount = line.LineAmount,
                ReferenceType = line.ReferenceType,
                ReferenceId = line.ReferenceId
            }, ct);
        }

        await _hospitalBillingRepository.AddOutboxMessageAsync(new HospitalBillingOutboxCreateCommand
        {
            OutboxMessageId = Guid.NewGuid(),
            AggregateType = "Invoice",
            AggregateId = invoiceId,
            EventType = "InvoiceIssued.v1",
            PayloadJson = JsonSerializer.Serialize(new
            {
                invoiceId,
                encounter.EncounterId,
                encounter.EncounterNumber,
                encounter.PatientId,
                encounter.PatientName,
                encounter.MedicalRecordNumber,
                phone = encounter.PatientPhone,
                email = encounter.PatientEmail,
                subtotal,
                discount,
                insurance,
                total,
                issuedAtUtc = nowUtc
            }, JsonOptions),
            Status = "Pending",
            AvailableAtUtc = nowUtc
        }, ct);

        await _hospitalBillingRepository.SaveChangesAsync(ct);
        await _dashboardQueryCache.InvalidateAsync(ct);

        _businessMetricsRecorder.IncrementEvent("hospital_billing", "invoice_issued", new Dictionary<string, string?>
        {
            ["line_count"] = encounter.BillableLines.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });

        var created = await _hospitalBillingRepository.GetByIdAsync(invoiceId, ct)
            ?? throw new InvalidOperationException("Khong the tai lai hoa don sau khi tao.");

        return MapDetail(created);
    }

    public async Task<HospitalPaymentIntentDto?> CreatePaymentIntentAsync(
        Guid invoiceId,
        CreateHospitalPaymentIntentDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default)
    {
        var invoice = await _hospitalBillingRepository.GetByIdAsync(invoiceId, ct);
        if (invoice == null)
        {
            return null;
        }

        if (invoice.InvoiceStatus == "Paid")
        {
            throw new InvalidOperationException("Hoa don nay da thanh toan du, khong can tao giao dich moi.");
        }

        var paidAmount = CalculateNetPaidAmount(invoice);
        var balance = invoice.TotalAmount - paidAmount;
        if (request.Amount > balance)
        {
            throw new InvalidOperationException("So tien giao dich cho thanh toan vuot qua cong no con lai.");
        }

        var actorHospitalUserId = await _hospitalIdentityBridgeService.ResolveHospitalUserIdAsync(actorUserId, actorUsername, ct);
        var nowUtc = DateTime.UtcNow;
        var paymentId = Guid.NewGuid();
        var paymentReference = string.IsNullOrWhiteSpace(request.PaymentReference)
            ? GeneratePaymentReference(nowUtc)
            : request.PaymentReference.Trim();
        var gatewayPreparation = _hospitalPaymentGatewayService.PreparePaymentIntent(new HospitalPaymentGatewayIntentRequest
        {
            InvoiceId = invoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            PaymentReference = paymentReference,
            PaymentMethod = request.PaymentMethod.Trim(),
            Amount = request.Amount,
            RequestedProvider = request.GatewayProvider,
            ExistingExternalTransactionId = request.ExternalTransactionId
        });

        await _hospitalBillingRepository.AddPaymentAsync(new HospitalPaymentCreateCommand
        {
            PaymentId = paymentId,
            InvoiceId = invoiceId,
            PaymentReference = paymentReference,
            PaymentMethod = request.PaymentMethod.Trim(),
            GatewayProvider = gatewayPreparation.ProviderName,
            Amount = request.Amount,
            PaymentStatus = "Pending",
            PaidAtUtc = null,
            ReceivedByUserId = actorHospitalUserId,
            ExternalTransactionId = gatewayPreparation.ExternalTransactionId
        }, ct);

        await _hospitalBillingRepository.AddOutboxMessageAsync(new HospitalBillingOutboxCreateCommand
        {
            OutboxMessageId = Guid.NewGuid(),
            AggregateType = "Invoice",
            AggregateId = invoiceId,
            EventType = "InvoicePaymentIntentCreated.v1",
            PayloadJson = JsonSerializer.Serialize(new
            {
                invoiceId,
                invoice.InvoiceNumber,
                gatewayProvider = gatewayPreparation.ProviderName,
                paymentReference,
                paymentMethod = request.PaymentMethod.Trim(),
                amount = request.Amount,
                externalTransactionId = gatewayPreparation.ExternalTransactionId,
                createdAtUtc = nowUtc
            }, JsonOptions),
            Status = "Pending",
            AvailableAtUtc = nowUtc
        }, ct);

        await _hospitalBillingRepository.SaveChangesAsync(ct);

        _businessMetricsRecorder.IncrementEvent("hospital_billing", "payment_intent_created", new Dictionary<string, string?>
        {
            ["payment_method"] = request.PaymentMethod.Trim()
        });

        return new HospitalPaymentIntentDto
        {
            PaymentId = paymentId,
            InvoiceId = invoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            GatewayProvider = gatewayPreparation.ProviderName,
            PaymentReference = paymentReference,
            PaymentMethod = request.PaymentMethod.Trim(),
            Amount = request.Amount,
            PaymentStatus = "Pending",
            ExternalTransactionId = gatewayPreparation.ExternalTransactionId,
            CheckoutToken = gatewayPreparation.CheckoutToken,
            CheckoutUrl = gatewayPreparation.CheckoutUrl,
            InstructionText = gatewayPreparation.InstructionText,
            CallbackMode = gatewayPreparation.CallbackMode,
            CreatedAtLocal = ConvertUtcToClinicLocal(nowUtc)
        };
    }

    public async Task<HospitalInvoiceDetailDto?> ReceivePaymentAsync(
        Guid invoiceId,
        ReceiveHospitalPaymentDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default)
    {
        var invoice = await _hospitalBillingRepository.GetByIdAsync(invoiceId, ct);
        if (invoice == null)
        {
            return null;
        }

        if (invoice.InvoiceStatus == "Paid")
        {
            throw new InvalidOperationException("Hoa don nay da duoc thanh toan du.");
        }

        var paidAmount = CalculateNetPaidAmount(invoice);
        var balance = invoice.TotalAmount - paidAmount;
        if (request.Amount > balance)
        {
            throw new InvalidOperationException("So tien thanh toan vuot qua cong no con lai.");
        }

        var actorHospitalUserId = await _hospitalIdentityBridgeService.ResolveHospitalUserIdAsync(actorUserId, actorUsername, ct);
        var nowUtc = DateTime.UtcNow;

        await _hospitalBillingRepository.AddPaymentAsync(new HospitalPaymentCreateCommand
        {
            PaymentId = Guid.NewGuid(),
            InvoiceId = invoiceId,
            PaymentReference = string.IsNullOrWhiteSpace(request.PaymentReference)
                ? GeneratePaymentReference(nowUtc)
                : request.PaymentReference.Trim(),
            PaymentMethod = request.PaymentMethod.Trim(),
            GatewayProvider = ResolveManualGatewayProvider(
                request.PaymentMethod,
                request.ExternalTransactionId),
            Amount = request.Amount,
            PaymentStatus = "Captured",
            PaidAtUtc = nowUtc,
            ReceivedByUserId = actorHospitalUserId,
            ExternalTransactionId = NormalizeText(request.ExternalTransactionId)
        }, ct);

        var newPaidAmount = paidAmount + request.Amount;
        var newStatus = ResolveInvoiceStatus(invoice.TotalAmount, newPaidAmount);
        await _hospitalBillingRepository.UpdateInvoiceAmountsAsync(
            invoiceId,
            newStatus,
            invoice.SubtotalAmount,
            invoice.DiscountAmount,
            invoice.InsuranceAmount,
            invoice.TotalAmount,
            ct);

        await _hospitalBillingRepository.AddOutboxMessageAsync(new HospitalBillingOutboxCreateCommand
        {
            OutboxMessageId = Guid.NewGuid(),
            AggregateType = "Invoice",
            AggregateId = invoiceId,
            EventType = "InvoicePaymentReceived.v1",
            PayloadJson = JsonSerializer.Serialize(new
            {
                invoiceId,
                invoice.InvoiceNumber,
                patientId = invoice.PatientId,
                patientName = invoice.PatientName,
                medicalRecordNumber = invoice.MedicalRecordNumber,
                phone = invoice.PatientPhone,
                email = invoice.PatientEmail,
                amount = request.Amount,
                paymentMethod = request.PaymentMethod.Trim(),
                paymentReference = string.IsNullOrWhiteSpace(request.PaymentReference)
                    ? null
                    : request.PaymentReference.Trim(),
                paidAtUtc = nowUtc,
                receivedByUserId = actorHospitalUserId,
                invoiceStatus = newStatus
            }, JsonOptions),
            Status = "Pending",
            AvailableAtUtc = nowUtc
        }, ct);

        await _hospitalBillingRepository.SaveChangesAsync(ct);
        await _dashboardQueryCache.InvalidateAsync(ct);

        _businessMetricsRecorder.IncrementEvent("hospital_billing", "payment_received", new Dictionary<string, string?>
        {
            ["invoice_status"] = newStatus,
            ["payment_method"] = request.PaymentMethod.Trim()
        });

        var updated = await _hospitalBillingRepository.GetByIdAsync(invoiceId, ct)
            ?? throw new InvalidOperationException("Khong the tai lai hoa don sau khi ghi nhan thanh toan.");

        return MapDetail(updated);
    }

    public async Task<HospitalInvoiceDetailDto?> ConfirmPaymentCallbackAsync(
        ConfirmHospitalPaymentCallbackDto request,
        Guid? actorUserId,
        string? actorUsername,
        bool isSimulation,
        string? callbackSource,
        CancellationToken ct = default)
    {
        var invoice = await _hospitalBillingRepository.GetByIdAsync(request.InvoiceId, ct);
        if (invoice == null)
        {
            return null;
        }

        var payment = await _hospitalBillingRepository.GetPaymentAsync(request.InvoiceId, request.PaymentReference, ct)
            ?? throw new InvalidOperationException("Khong tim thay giao dich doi soat theo payment reference.");

        var gatewayStatus = request.GatewayStatus.Trim();
        var normalizedGatewayStatus = _hospitalPaymentGatewayService.NormalizeGatewayStatus(request.GatewayProvider, gatewayStatus);
        var gatewayProvider = NormalizeText(request.GatewayProvider) ?? "MockGateway";
        var gatewayEventId = NormalizeText(request.GatewayEventId)
            ?? throw new InvalidOperationException("Gateway event id khong hop le.");
        var gatewayTimestampUtc = request.GatewayTimestampUtc?.ToUniversalTime()
            ?? throw new InvalidOperationException("Gateway timestamp khong hop le.");
        var callbackModeLabel = ResolveCallbackModeLabel(isSimulation, callbackSource);

        if (payment.PaymentStatus == "Refunded")
        {
            throw new InvalidOperationException("Giao dich da hoan tien, khong the nhan callback thanh toan moi.");
        }

        if (request.Amount.HasValue && request.Amount.Value != payment.Amount)
        {
            throw new InvalidOperationException("So tien callback khong khop voi giao dich cho xu ly.");
        }

        if (!string.Equals(payment.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(payment.PaymentStatus, normalizedGatewayStatus, StringComparison.OrdinalIgnoreCase))
            {
                await _complianceAuditRecorder.RecordAsync(
                    actorUserId,
                    actorUsername ?? "gateway",
                    "InvoicePaymentCallbackDuplicate",
                    "Info",
                    $"InvoiceId={request.InvoiceId}; InvoiceNumber={invoice.InvoiceNumber}; PaymentReference={payment.PaymentReference}; GatewayProvider={gatewayProvider}; GatewayEventId={gatewayEventId}; PaymentStatus={payment.PaymentStatus}; CallbackMode={callbackModeLabel}; GatewayTimestampUtc={gatewayTimestampUtc:O}.",
                    ct);

                return MapDetail(invoice);
            }

            throw new InvalidOperationException(
                $"Khong the ap callback {normalizedGatewayStatus} cho giao dich dang o trang thai {payment.PaymentStatus}.");
        }

        var nowUtc = DateTime.UtcNow;
        await _hospitalBillingRepository.UpdatePaymentAsync(new HospitalPaymentUpdateCommand
        {
            PaymentId = payment.PaymentId,
            GatewayProvider = gatewayProvider,
            PaymentStatus = normalizedGatewayStatus,
            PaidAtUtc = normalizedGatewayStatus == "Captured" ? nowUtc : null,
            ReceivedByUserId = payment.ReceivedByUserId,
            ExternalTransactionId = NormalizeText(request.ExternalTransactionId) ?? payment.ExternalTransactionId
        }, ct);

        if (normalizedGatewayStatus == "Captured")
        {
            var newPaidAmount = CalculateNetPaidAmount(invoice) + payment.Amount;
            var newStatus = ResolveInvoiceStatus(invoice.TotalAmount, newPaidAmount);
            await _hospitalBillingRepository.UpdateInvoiceAmountsAsync(
                request.InvoiceId,
                newStatus,
                invoice.SubtotalAmount,
                invoice.DiscountAmount,
                invoice.InsuranceAmount,
                invoice.TotalAmount,
                ct);

            await _hospitalBillingRepository.AddOutboxMessageAsync(new HospitalBillingOutboxCreateCommand
            {
                OutboxMessageId = Guid.NewGuid(),
                AggregateType = "Invoice",
                AggregateId = request.InvoiceId,
                EventType = "InvoicePaymentCaptured.v1",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    invoiceId = request.InvoiceId,
                    invoice.InvoiceNumber,
                    payment.PaymentReference,
                    amount = payment.Amount,
                    externalTransactionId = NormalizeText(request.ExternalTransactionId) ?? payment.ExternalTransactionId,
                    paidAtUtc = nowUtc,
                    invoiceStatus = newStatus
                }, JsonOptions),
                Status = "Pending",
                AvailableAtUtc = nowUtc
            }, ct);
        }
        else
        {
            await _hospitalBillingRepository.AddOutboxMessageAsync(new HospitalBillingOutboxCreateCommand
            {
                OutboxMessageId = Guid.NewGuid(),
                AggregateType = "Invoice",
                AggregateId = request.InvoiceId,
                EventType = "InvoicePaymentFailed.v1",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    invoiceId = request.InvoiceId,
                    invoice.InvoiceNumber,
                    gatewayProvider,
                    gatewayEventId,
                    payment.PaymentReference,
                    amount = payment.Amount,
                    externalTransactionId = NormalizeText(request.ExternalTransactionId) ?? payment.ExternalTransactionId,
                    failedAtUtc = nowUtc,
                    invoiceStatus = invoice.InvoiceStatus
                }, JsonOptions),
                Status = "Pending",
                AvailableAtUtc = nowUtc
            }, ct);
        }

        await _hospitalBillingRepository.SaveChangesAsync(ct);
        await _dashboardQueryCache.InvalidateAsync(ct);

        _businessMetricsRecorder.IncrementEvent("hospital_billing", "payment_callback_processed", new Dictionary<string, string?>
        {
            ["gateway_status"] = normalizedGatewayStatus,
            ["gateway_provider"] = gatewayProvider,
            ["callback_mode"] = callbackModeLabel.ToLowerInvariant()
        });

        await _complianceAuditRecorder.RecordAsync(
            actorUserId,
            actorUsername ?? "gateway",
            "InvoicePaymentCallbackProcessed",
            normalizedGatewayStatus == "Captured" ? "Info" : "Warning",
            $"InvoiceId={request.InvoiceId}; InvoiceNumber={invoice.InvoiceNumber}; PaymentReference={payment.PaymentReference}; GatewayProvider={gatewayProvider}; GatewayEventId={gatewayEventId}; GatewayStatus={normalizedGatewayStatus}; CallbackMode={callbackModeLabel}; GatewayTimestampUtc={gatewayTimestampUtc:O}.",
            ct);

        var updated = await _hospitalBillingRepository.GetByIdAsync(request.InvoiceId, ct)
            ?? throw new InvalidOperationException("Khong the tai lai hoa don sau callback thanh toan.");

        return MapDetail(updated);
    }

    public async Task<HospitalInvoiceDetailDto?> RefundPaymentAsync(
        Guid invoiceId,
        RefundHospitalPaymentDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default)
    {
        var invoice = await _hospitalBillingRepository.GetByIdAsync(invoiceId, ct);
        if (invoice == null)
        {
            return null;
        }

        var refundableAmount = CalculateNetPaidAmount(invoice);
        if (refundableAmount <= 0)
        {
            throw new InvalidOperationException("Hoa don nay khong con so tien hop le de hoan.");
        }

        if (request.Amount > refundableAmount)
        {
            throw new InvalidOperationException("So tien hoan vuot qua tong da thu chua hoan.");
        }

        var actorHospitalUserId = await _hospitalIdentityBridgeService.ResolveHospitalUserIdAsync(actorUserId, actorUsername, ct);
        var nowUtc = DateTime.UtcNow;
        var normalizedReason = NormalizeText(request.Reason)
            ?? throw new InvalidOperationException("Ly do hoan tien khong hop le.");

        await _hospitalBillingRepository.AddPaymentAsync(new HospitalPaymentCreateCommand
        {
            PaymentId = Guid.NewGuid(),
            InvoiceId = invoiceId,
            PaymentReference = string.IsNullOrWhiteSpace(request.PaymentReference)
                ? GenerateRefundReference(nowUtc)
                : request.PaymentReference.Trim(),
            PaymentMethod = request.PaymentMethod.Trim(),
            GatewayProvider = ResolveManualGatewayProvider(
                request.PaymentMethod,
                request.ExternalTransactionId,
                invoice.Payments
                    .Where(x => !string.IsNullOrWhiteSpace(x.GatewayProvider) &&
                                string.Equals(x.PaymentMethod, request.PaymentMethod.Trim(), StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.PaidAtUtc)
                    .Select(x => x.GatewayProvider)
                    .FirstOrDefault()),
            Amount = -request.Amount,
            PaymentStatus = "Refunded",
            PaidAtUtc = nowUtc,
            ReceivedByUserId = actorHospitalUserId,
            ExternalTransactionId = NormalizeText(request.ExternalTransactionId)
        }, ct);

        var netPaidAfterRefund = refundableAmount - request.Amount;
        var newStatus = ResolveInvoiceStatus(invoice.TotalAmount, netPaidAfterRefund);
        await _hospitalBillingRepository.UpdateInvoiceAmountsAsync(
            invoiceId,
            newStatus,
            invoice.SubtotalAmount,
            invoice.DiscountAmount,
            invoice.InsuranceAmount,
            invoice.TotalAmount,
            ct);

        await _hospitalBillingRepository.AddOutboxMessageAsync(new HospitalBillingOutboxCreateCommand
        {
            OutboxMessageId = Guid.NewGuid(),
            AggregateType = "Invoice",
            AggregateId = invoiceId,
            EventType = "InvoiceRefunded.v1",
            PayloadJson = JsonSerializer.Serialize(new
            {
                invoiceId,
                invoice.InvoiceNumber,
                patientId = invoice.PatientId,
                patientName = invoice.PatientName,
                medicalRecordNumber = invoice.MedicalRecordNumber,
                phone = invoice.PatientPhone,
                email = invoice.PatientEmail,
                amount = request.Amount,
                paymentMethod = request.PaymentMethod.Trim(),
                paymentReference = string.IsNullOrWhiteSpace(request.PaymentReference)
                    ? null
                    : request.PaymentReference.Trim(),
                refundedAtUtc = nowUtc,
                reason = normalizedReason,
                receivedByUserId = actorHospitalUserId,
                invoiceStatus = newStatus
            }, JsonOptions),
            Status = "Pending",
            AvailableAtUtc = nowUtc
        }, ct);

        await _hospitalBillingRepository.SaveChangesAsync(ct);
        await _dashboardQueryCache.InvalidateAsync(ct);

        _businessMetricsRecorder.IncrementEvent("hospital_billing", "payment_refunded", new Dictionary<string, string?>
        {
            ["invoice_status"] = newStatus,
            ["payment_method"] = request.PaymentMethod.Trim()
        });

        await _complianceAuditRecorder.RecordAsync(
            actorUserId,
            actorUsername ?? "unknown",
            "InvoiceRefunded",
            "Warning",
            $"InvoiceId={invoiceId}; InvoiceNumber={invoice.InvoiceNumber}; Amount={request.Amount}; PaymentMethod={request.PaymentMethod.Trim()}; InvoiceStatus={newStatus}; Reason={normalizedReason}.",
            ct);

        var updated = await _hospitalBillingRepository.GetByIdAsync(invoiceId, ct)
            ?? throw new InvalidOperationException("Khong the tai lai hoa don sau khi ghi nhan hoan tien.");

        return MapDetail(updated);
    }

    public async Task<HospitalPaymentReconciliationSummaryDto> GetReconciliationSummaryAsync(
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default)
    {
        var doctorProfileId = await ResolveScopedDoctorProfileIdAsync(currentRole, currentUsername, ct);
        var snapshot = await _hospitalBillingRepository.GetReconciliationSnapshotAsync(doctorProfileId, ct);
        return new HospitalPaymentReconciliationSummaryDto
        {
            GeneratedAtLocal = ConvertUtcToClinicLocal(snapshot.GeneratedAtUtc),
            PendingPayments = snapshot.PendingPayments,
            CapturedPayments = snapshot.CapturedPayments,
            FailedPayments = snapshot.FailedPayments,
            RefundedPayments = snapshot.RefundedPayments,
            PendingAmount = snapshot.PendingAmount,
            CapturedAmount = snapshot.CapturedAmount,
            FailedAmount = snapshot.FailedAmount,
            RefundedAmount = snapshot.RefundedAmount,
            MissingExternalTransactionCount = snapshot.MissingExternalTransactionCount,
            RecentPayments = snapshot.RecentPayments.Select(x => new HospitalPaymentDto
            {
                PaymentId = x.PaymentId,
                PaymentReference = x.PaymentReference,
                PaymentMethod = x.PaymentMethod,
                GatewayProvider = x.GatewayProvider,
                Amount = x.Amount,
                PaymentStatus = x.PaymentStatus,
                PaidAtLocal = x.PaidAtUtc.HasValue ? ConvertUtcToClinicLocal(x.PaidAtUtc.Value) : null,
                ReceivedByUsername = x.ReceivedByUsername,
                ExternalTransactionId = x.ExternalTransactionId
            }).ToList()
        };
    }

    public async Task<HospitalPaymentReconciliationPreviewDto> PreviewReconciliationAsync(
        HospitalPaymentReconciliationPreviewRequestDto request,
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default)
    {
        var doctorProfileId = await ResolveScopedDoctorProfileIdAsync(currentRole, currentUsername, ct);
        if (string.Equals(currentRole, "Doctor", StringComparison.OrdinalIgnoreCase) && !doctorProfileId.HasValue)
        {
            return new HospitalPaymentReconciliationPreviewDto
            {
                GatewayProvider = NormalizeText(request.GatewayProvider) ?? _hospitalPaymentGatewayService.GetDefaultProvider(),
                GeneratedAtLocal = ConvertUtcToClinicLocal(DateTime.UtcNow),
                TotalPartnerItems = request.Items.Count
            };
        }

        var gatewayProvider = NormalizeText(request.GatewayProvider) ?? _hospitalPaymentGatewayService.GetDefaultProvider();
        var paymentReferences = request.Items
            .Select(x => NormalizeText(x.PaymentReference))
            .Where(x => x is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var externalTransactionIds = request.Items
            .Select(x => NormalizeText(x.ExternalTransactionId))
            .Where(x => x is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var localPayments = await _hospitalBillingRepository.FindPaymentsForReconciliationAsync(
            new HospitalPaymentReconciliationLookupQuery
            {
                DoctorProfileId = doctorProfileId,
                PaymentReferences = paymentReferences,
                ExternalTransactionIds = externalTransactionIds
            },
            ct);

        var byExternalTransactionId = localPayments
            .Where(x => !string.IsNullOrWhiteSpace(x.ExternalTransactionId))
            .GroupBy(x => x.ExternalTransactionId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(p => p.PaidAtUtc).First(), StringComparer.OrdinalIgnoreCase);
        var byPaymentReference = localPayments
            .GroupBy(x => x.PaymentReference, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(p => p.PaidAtUtc).First(), StringComparer.OrdinalIgnoreCase);

        var previewItems = new List<HospitalPaymentReconciliationPreviewItemDto>(request.Items.Count);

        foreach (var item in request.Items)
        {
            var normalizedReference = NormalizeText(item.PaymentReference);
            var normalizedExternalTransactionId = NormalizeText(item.ExternalTransactionId);
            var normalizedPartnerStatus = _hospitalPaymentGatewayService.NormalizeGatewayStatus(gatewayProvider, item.GatewayStatus);

            HospitalPaymentRecordSnapshot? localPayment = null;
            if (normalizedExternalTransactionId is not null)
            {
                byExternalTransactionId.TryGetValue(normalizedExternalTransactionId, out localPayment);
            }

            if (localPayment is null && normalizedReference is not null)
            {
                byPaymentReference.TryGetValue(normalizedReference, out localPayment);
            }

            if (localPayment is null)
            {
                previewItems.Add(new HospitalPaymentReconciliationPreviewItemDto
                {
                    PartnerRecordId = NormalizeText(item.PartnerRecordId),
                    PaymentReference = normalizedReference,
                    ExternalTransactionId = normalizedExternalTransactionId,
                    PartnerAmount = item.Amount,
                    PartnerStatus = item.GatewayStatus.Trim(),
                    NormalizedPartnerStatus = normalizedPartnerStatus,
                    ResolutionCode = "MISSING_LOCAL_PAYMENT",
                    ResolutionMessage = "Khong tim thay giao dich noi bo khop payment reference hoac external transaction id."
                });
                continue;
            }

            var isAmountMismatch = localPayment.Amount != item.Amount;
            var isStatusMismatch = !string.Equals(localPayment.PaymentStatus, normalizedPartnerStatus, StringComparison.OrdinalIgnoreCase);
            var isMatched = !isAmountMismatch && !isStatusMismatch;

            previewItems.Add(new HospitalPaymentReconciliationPreviewItemDto
            {
                PartnerRecordId = NormalizeText(item.PartnerRecordId),
                PaymentReference = normalizedReference,
                ExternalTransactionId = normalizedExternalTransactionId,
                PartnerAmount = item.Amount,
                PartnerStatus = item.GatewayStatus.Trim(),
                NormalizedPartnerStatus = normalizedPartnerStatus,
                LocalPaymentId = localPayment.PaymentId,
                LocalPaymentReference = localPayment.PaymentReference,
                LocalExternalTransactionId = localPayment.ExternalTransactionId,
                LocalGatewayProvider = localPayment.GatewayProvider,
                LocalAmount = localPayment.Amount,
                LocalPaymentStatus = localPayment.PaymentStatus,
                IsMatched = isMatched,
                IsAmountMismatch = isAmountMismatch,
                IsStatusMismatch = isStatusMismatch,
                ResolutionCode = isMatched
                    ? "MATCHED"
                    : isAmountMismatch && isStatusMismatch
                        ? "AMOUNT_AND_STATUS_MISMATCH"
                        : isAmountMismatch
                            ? "AMOUNT_MISMATCH"
                            : "STATUS_MISMATCH",
                ResolutionMessage = isMatched
                    ? "Giao dich doi soat khop giua doi tac va he thong."
                    : isAmountMismatch && isStatusMismatch
                        ? "Lech ca so tien va trang thai giua doi tac va he thong."
                        : isAmountMismatch
                            ? "So tien doi tac va he thong khong khop."
                            : "Trang thai doi tac va he thong khong khop."
            });
        }

        return new HospitalPaymentReconciliationPreviewDto
        {
            GatewayProvider = gatewayProvider,
            GeneratedAtLocal = ConvertUtcToClinicLocal(DateTime.UtcNow),
            TotalPartnerItems = previewItems.Count,
            MatchedItems = previewItems.Count(x => x.IsMatched),
            MissingLocalPayments = previewItems.Count(x => x.ResolutionCode == "MISSING_LOCAL_PAYMENT"),
            AmountMismatchItems = previewItems.Count(x => x.IsAmountMismatch),
            StatusMismatchItems = previewItems.Count(x => x.IsStatusMismatch),
            Items = previewItems
        };
    }

    public async Task<HospitalPaymentReconciliationApplyResultDto> ApplyReconciliationAsync(
        HospitalPaymentReconciliationApplyRequestDto request,
        Guid? actorUserId,
        string? actorUsername,
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default)
    {
        var doctorProfileId = await ResolveScopedDoctorProfileIdAsync(currentRole, currentUsername, ct);
        if (string.Equals(currentRole, "Doctor", StringComparison.OrdinalIgnoreCase) && !doctorProfileId.HasValue)
        {
            return new HospitalPaymentReconciliationApplyResultDto
            {
                GatewayProvider = NormalizeText(request.GatewayProvider) ?? _hospitalPaymentGatewayService.GetDefaultProvider(),
                AppliedAtLocal = ConvertUtcToClinicLocal(DateTime.UtcNow),
                TotalPartnerItems = request.Items.Count,
                SkippedCount = request.Items.Count
            };
        }

        var gatewayProvider = NormalizeText(request.GatewayProvider) ?? _hospitalPaymentGatewayService.GetDefaultProvider();
        var paymentReferences = request.Items
            .Select(x => NormalizeText(x.PaymentReference))
            .Where(x => x is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var externalTransactionIds = request.Items
            .Select(x => NormalizeText(x.ExternalTransactionId))
            .Where(x => x is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var localPayments = await _hospitalBillingRepository.FindPaymentsForReconciliationAsync(
            new HospitalPaymentReconciliationLookupQuery
            {
                DoctorProfileId = doctorProfileId,
                PaymentReferences = paymentReferences,
                ExternalTransactionIds = externalTransactionIds
            },
            ct);

        var byExternalTransactionId = localPayments
            .Where(x => !string.IsNullOrWhiteSpace(x.ExternalTransactionId))
            .GroupBy(x => x.ExternalTransactionId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(p => p.PaidAtUtc).First(), StringComparer.OrdinalIgnoreCase);
        var byPaymentReference = localPayments
            .GroupBy(x => x.PaymentReference, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(p => p.PaidAtUtc).First(), StringComparer.OrdinalIgnoreCase);

        var seenPaymentIds = new HashSet<Guid>();
        var results = new List<HospitalPaymentReconciliationApplyItemResultDto>(request.Items.Count);

        foreach (var item in request.Items)
        {
            var normalizedReference = NormalizeText(item.PaymentReference);
            var normalizedExternalTransactionId = NormalizeText(item.ExternalTransactionId);
            var normalizedPartnerStatus = _hospitalPaymentGatewayService.NormalizeGatewayStatus(gatewayProvider, item.GatewayStatus);

            HospitalPaymentRecordSnapshot? localPayment = null;
            if (normalizedExternalTransactionId is not null)
            {
                byExternalTransactionId.TryGetValue(normalizedExternalTransactionId, out localPayment);
            }

            if (localPayment is null && normalizedReference is not null)
            {
                byPaymentReference.TryGetValue(normalizedReference, out localPayment);
            }

            if (localPayment is null)
            {
                results.Add(new HospitalPaymentReconciliationApplyItemResultDto
                {
                    PartnerRecordId = NormalizeText(item.PartnerRecordId),
                    PaymentReference = normalizedReference,
                    ExternalTransactionId = normalizedExternalTransactionId,
                    ActionCode = "SKIPPED_MISSING_LOCAL_PAYMENT",
                    Message = "Khong tim thay giao dich noi bo de ap doi soat."
                });
                continue;
            }

            if (!seenPaymentIds.Add(localPayment.PaymentId))
            {
                results.Add(new HospitalPaymentReconciliationApplyItemResultDto
                {
                    PartnerRecordId = NormalizeText(item.PartnerRecordId),
                    PaymentReference = normalizedReference,
                    ExternalTransactionId = normalizedExternalTransactionId,
                    LocalPaymentId = localPayment.PaymentId,
                    LocalInvoiceId = localPayment.InvoiceId,
                    FinalLocalPaymentStatus = localPayment.PaymentStatus,
                    ActionCode = "SKIPPED_DUPLICATE_INPUT",
                    Message = "Giao dich noi bo nay da duoc xu ly boi mot dong doi soat truoc do trong cung request."
                });
                continue;
            }

            if (localPayment.Amount != item.Amount)
            {
                results.Add(new HospitalPaymentReconciliationApplyItemResultDto
                {
                    PartnerRecordId = NormalizeText(item.PartnerRecordId),
                    PaymentReference = normalizedReference,
                    ExternalTransactionId = normalizedExternalTransactionId,
                    LocalPaymentId = localPayment.PaymentId,
                    LocalInvoiceId = localPayment.InvoiceId,
                    FinalLocalPaymentStatus = localPayment.PaymentStatus,
                    ActionCode = "SKIPPED_AMOUNT_MISMATCH",
                    Message = "So tien doi tac khong khop voi giao dich noi bo, khong ap doi soat."
                });
                continue;
            }

            if (request.PendingOnly && !string.Equals(localPayment.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new HospitalPaymentReconciliationApplyItemResultDto
                {
                    PartnerRecordId = NormalizeText(item.PartnerRecordId),
                    PaymentReference = normalizedReference,
                    ExternalTransactionId = normalizedExternalTransactionId,
                    LocalPaymentId = localPayment.PaymentId,
                    LocalInvoiceId = localPayment.InvoiceId,
                    FinalLocalPaymentStatus = localPayment.PaymentStatus,
                    ActionCode = "SKIPPED_NOT_PENDING",
                    Message = "Chi cho phep ap doi soat cho giao dich Pending trong request hien tai."
                });
                continue;
            }

            if (string.Equals(localPayment.PaymentStatus, normalizedPartnerStatus, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new HospitalPaymentReconciliationApplyItemResultDto
                {
                    PartnerRecordId = NormalizeText(item.PartnerRecordId),
                    PaymentReference = normalizedReference,
                    ExternalTransactionId = normalizedExternalTransactionId,
                    LocalPaymentId = localPayment.PaymentId,
                    LocalInvoiceId = localPayment.InvoiceId,
                    FinalLocalPaymentStatus = localPayment.PaymentStatus,
                    ActionCode = "SKIPPED_ALREADY_MATCHED",
                    Message = "Trang thai giao dich noi bo da khop voi doi tac."
                });
                continue;
            }

            try
            {
                var callbackResult = await ConfirmPaymentCallbackAsync(
                    new ConfirmHospitalPaymentCallbackDto
                    {
                        InvoiceId = localPayment.InvoiceId,
                        GatewayProvider = gatewayProvider,
                        GatewayEventId = NormalizeText(item.PartnerRecordId) ?? $"RECON-{Guid.NewGuid():N}",
                        GatewayTimestampUtc = item.PaidAtUtc?.ToUniversalTime() ?? DateTime.UtcNow,
                        PaymentReference = localPayment.PaymentReference,
                        ExternalTransactionId = normalizedExternalTransactionId ?? localPayment.ExternalTransactionId,
                        GatewayStatus = item.GatewayStatus,
                        Amount = item.Amount
                    },
                    actorUserId,
                    actorUsername,
                    true,
                    "ReconciliationApply",
                    ct);

                var finalLocalPaymentStatus = callbackResult?.Payments
                    .FirstOrDefault(x => x.PaymentId == localPayment.PaymentId)?.PaymentStatus
                    ?? normalizedPartnerStatus;

                results.Add(new HospitalPaymentReconciliationApplyItemResultDto
                {
                    PartnerRecordId = NormalizeText(item.PartnerRecordId),
                    PaymentReference = normalizedReference,
                    ExternalTransactionId = normalizedExternalTransactionId,
                    LocalPaymentId = localPayment.PaymentId,
                    LocalInvoiceId = localPayment.InvoiceId,
                    FinalLocalPaymentStatus = finalLocalPaymentStatus,
                    ActionCode = "APPLIED",
                    Message = "Da ap trang thai doi soat vao giao dich noi bo."
                });
            }
            catch (InvalidOperationException ex)
            {
                results.Add(new HospitalPaymentReconciliationApplyItemResultDto
                {
                    PartnerRecordId = NormalizeText(item.PartnerRecordId),
                    PaymentReference = normalizedReference,
                    ExternalTransactionId = normalizedExternalTransactionId,
                    LocalPaymentId = localPayment.PaymentId,
                    LocalInvoiceId = localPayment.InvoiceId,
                    FinalLocalPaymentStatus = localPayment.PaymentStatus,
                    ActionCode = "ERROR_APPLY_FAILED",
                    Message = ex.Message
                });
            }
        }

        return new HospitalPaymentReconciliationApplyResultDto
        {
            GatewayProvider = gatewayProvider,
            AppliedAtLocal = ConvertUtcToClinicLocal(DateTime.UtcNow),
            TotalPartnerItems = results.Count,
            AppliedCount = results.Count(x => x.ActionCode == "APPLIED"),
            SkippedCount = results.Count(x => x.ActionCode.StartsWith("SKIPPED_", StringComparison.Ordinal)),
            ErrorCount = results.Count(x => x.ActionCode.StartsWith("ERROR_", StringComparison.Ordinal)),
            Items = results
        };
    }

    private async Task<PaginatedResult<HospitalInvoiceSummaryDto>> GetScopedWorklistAsync(
        HospitalInvoiceWorklistRequestDto request,
        string currentRole,
        string? currentUsername,
        CancellationToken ct)
    {
        var doctorProfileId = await ResolveScopedDoctorProfileIdAsync(currentRole, currentUsername, ct);
        if (string.Equals(currentRole, "Doctor", StringComparison.OrdinalIgnoreCase) && !doctorProfileId.HasValue)
        {
            return new PaginatedResult<HospitalInvoiceSummaryDto>(
                Array.Empty<HospitalInvoiceSummaryDto>(),
                0,
                request.PageNumber,
                request.PageSize);
        }

        request.DoctorProfileId = doctorProfileId;
        return await _hospitalBillingRepository.GetWorklistAsync(request, ct);
    }

    private async Task<Guid?> ResolveScopedDoctorProfileIdAsync(
        string currentRole,
        string? currentUsername,
        CancellationToken ct)
    {
        if (!string.Equals(currentRole, "Doctor", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(currentUsername))
        {
            return null;
        }

        var doctorProfile = await _hospitalDoctorWorklistRepository.ResolveDoctorByUsernameAsync(currentUsername, ct);
        return doctorProfile?.DoctorProfileId;
    }

    private async Task<bool> CanAccessDoctorScopedDataAsync(
        Guid? doctorProfileId,
        string currentRole,
        string? currentUsername,
        CancellationToken ct)
    {
        var scopedDoctorProfileId = await ResolveScopedDoctorProfileIdAsync(currentRole, currentUsername, ct);
        return !scopedDoctorProfileId.HasValue || (doctorProfileId.HasValue && scopedDoctorProfileId.Value == doctorProfileId.Value);
    }

    private static HospitalInvoiceDetailDto MapDetail(HospitalInvoiceAggregateSnapshot invoice)
    {
        var paidAmount = CalculateNetPaidAmount(invoice);

        return new HospitalInvoiceDetailDto
        {
            InvoiceId = invoice.InvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            PatientId = invoice.PatientId,
            PatientName = invoice.PatientName,
            MedicalRecordNumber = invoice.MedicalRecordNumber,
            DoctorProfileId = invoice.DoctorProfileId,
            EncounterId = invoice.EncounterId,
            EncounterNumber = invoice.EncounterNumber,
            DoctorName = invoice.DoctorName,
            SpecialtyName = invoice.SpecialtyName,
            ClinicName = invoice.ClinicName,
            InvoiceStatus = invoice.InvoiceStatus,
            SubtotalAmount = invoice.SubtotalAmount,
            DiscountAmount = invoice.DiscountAmount,
            InsuranceAmount = invoice.InsuranceAmount,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = paidAmount,
            BalanceAmount = invoice.TotalAmount - paidAmount,
            TotalItems = invoice.Items.Length,
            TotalPayments = invoice.Payments.Length,
            IssuedAtLocal = ConvertUtcToClinicLocal(invoice.IssuedAtUtc),
            DueAtLocal = invoice.DueAtUtc.HasValue ? ConvertUtcToClinicLocal(invoice.DueAtUtc.Value) : null,
            Items = invoice.Items.Select(x => new HospitalInvoiceItemDto
            {
                InvoiceItemId = x.InvoiceItemId,
                ServiceCatalogId = x.ServiceCatalogId,
                ItemType = x.ItemType,
                Description = x.Description,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineAmount = x.LineAmount,
                ReferenceType = x.ReferenceType,
                ReferenceId = x.ReferenceId
            }).ToList(),
            Payments = invoice.Payments.Select(x => new HospitalPaymentDto
            {
                PaymentId = x.PaymentId,
                PaymentReference = x.PaymentReference,
                PaymentMethod = x.PaymentMethod,
                GatewayProvider = x.GatewayProvider,
                Amount = x.Amount,
                PaymentStatus = x.PaymentStatus,
                PaidAtLocal = x.PaidAtUtc.HasValue ? ConvertUtcToClinicLocal(x.PaidAtUtc.Value) : null,
                ReceivedByUsername = x.ReceivedByUsername,
                ExternalTransactionId = x.ExternalTransactionId
            }).ToList()
        };
    }

    private static decimal CalculateNetPaidAmount(HospitalInvoiceAggregateSnapshot invoice)
        => invoice.Payments
            .Where(x => x.PaymentStatus is "Captured" or "Refunded")
            .Sum(x => x.Amount);

    private static string ResolveInvoiceStatus(decimal totalAmount, decimal netPaidAmount)
    {
        if (netPaidAmount >= totalAmount)
        {
            return "Paid";
        }

        if (netPaidAmount > 0)
        {
            return "PartiallyPaid";
        }

        return "Issued";
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string GenerateInvoiceNumber(DateTime nowUtc)
        => CompactCodeGenerator.Generate("IV", nowUtc);

    private static string GeneratePaymentReference(DateTime nowUtc)
        => CompactCodeGenerator.Generate("PY", nowUtc);

    private static string GenerateRefundReference(DateTime nowUtc)
        => CompactCodeGenerator.Generate("RF", nowUtc);

    private static string ResolveCallbackModeLabel(bool isSimulation, string? callbackSource)
        => NormalizeText(callbackSource) ?? (isSimulation ? "Simulation" : "Webhook");

    private static string? ResolveManualGatewayProvider(
        string paymentMethod,
        string? externalTransactionId,
        string? fallbackProvider = null)
    {
        if (string.Equals(paymentMethod.Trim(), "Cash", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var normalizedExternalTransactionId = NormalizeText(externalTransactionId);
        if (!string.IsNullOrWhiteSpace(normalizedExternalTransactionId))
        {
            var separatorIndex = normalizedExternalTransactionId.IndexOf('-', StringComparison.Ordinal);
            if (separatorIndex > 0)
            {
                return normalizedExternalTransactionId[..separatorIndex];
            }
        }

        return NormalizeText(fallbackProvider) ?? "ManualExternal";
    }

    private static TimeZoneInfo ResolveClinicTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static DateTime ConvertUtcToClinicLocal(DateTime utcDateTime)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), ResolveClinicTimeZone());
}
