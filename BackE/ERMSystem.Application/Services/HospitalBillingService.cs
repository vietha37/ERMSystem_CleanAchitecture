using System.Text.Json;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;

namespace ERMSystem.Application.Services;

public class HospitalBillingService : IHospitalBillingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHospitalBillingRepository _hospitalBillingRepository;
    private readonly IHospitalIdentityBridgeService _hospitalIdentityBridgeService;
    private readonly IBusinessMetricsRecorder _businessMetricsRecorder;
    private readonly IComplianceAuditRecorder _complianceAuditRecorder;
    private readonly IDashboardQueryCache _dashboardQueryCache;

    public HospitalBillingService(
        IHospitalBillingRepository hospitalBillingRepository,
        IHospitalIdentityBridgeService hospitalIdentityBridgeService,
        IBusinessMetricsRecorder businessMetricsRecorder,
        IComplianceAuditRecorder complianceAuditRecorder,
        IDashboardQueryCache dashboardQueryCache)
    {
        _hospitalBillingRepository = hospitalBillingRepository;
        _hospitalIdentityBridgeService = hospitalIdentityBridgeService;
        _businessMetricsRecorder = businessMetricsRecorder;
        _complianceAuditRecorder = complianceAuditRecorder;
        _dashboardQueryCache = dashboardQueryCache;
    }

    public Task<PaginatedResult<HospitalInvoiceSummaryDto>> GetWorklistAsync(
        HospitalInvoiceWorklistRequestDto request,
        CancellationToken ct = default)
        => _hospitalBillingRepository.GetWorklistAsync(request, ct);

    public async Task<HospitalInvoiceDetailDto?> GetByIdAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _hospitalBillingRepository.GetByIdAsync(invoiceId, ct);
        return invoice == null ? null : MapDetail(invoice);
    }

    public Task<HospitalBillingEligibleEncounterDto[]> GetEligibleEncountersAsync(CancellationToken ct = default)
        => _hospitalBillingRepository.GetEligibleEncountersAsync(ct);

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
        var insurance = Math.Max(0, request.InsuranceAmount);
        var total = Math.Max(0, subtotal - discount - insurance);
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
        var gatewayProvider = NormalizeText(request.GatewayProvider) ?? "MockGateway";
        var paymentReference = string.IsNullOrWhiteSpace(request.PaymentReference)
            ? GeneratePaymentReference(nowUtc)
            : request.PaymentReference.Trim();
        var externalTransactionId = NormalizeText(request.ExternalTransactionId)
            ?? $"EXT-{Guid.NewGuid():N}";

        await _hospitalBillingRepository.AddPaymentAsync(new HospitalPaymentCreateCommand
        {
            PaymentId = paymentId,
            InvoiceId = invoiceId,
            PaymentReference = paymentReference,
            PaymentMethod = request.PaymentMethod.Trim(),
            Amount = request.Amount,
            PaymentStatus = "Pending",
            PaidAtUtc = null,
            ReceivedByUserId = actorHospitalUserId,
            ExternalTransactionId = externalTransactionId
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
                gatewayProvider,
                paymentReference,
                paymentMethod = request.PaymentMethod.Trim(),
                amount = request.Amount,
                externalTransactionId,
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
            GatewayProvider = gatewayProvider,
            PaymentReference = paymentReference,
            PaymentMethod = request.PaymentMethod.Trim(),
            Amount = request.Amount,
            PaymentStatus = "Pending",
            ExternalTransactionId = externalTransactionId,
            CheckoutToken = $"CHK-{Guid.NewGuid():N}",
            InstructionText = $"Gateway {gatewayProvider} can callback co chu ky de xac nhan giao dich {paymentReference} cho hoa don {invoice.InvoiceNumber}.",
            CallbackMode = "SignedWebhook",
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
        var normalizedGatewayStatus = NormalizeGatewayStatus(gatewayStatus);
        var gatewayProvider = NormalizeText(request.GatewayProvider) ?? "MockGateway";
        var gatewayEventId = NormalizeText(request.GatewayEventId)
            ?? throw new InvalidOperationException("Gateway event id khong hop le.");
        var gatewayTimestampUtc = request.GatewayTimestampUtc?.ToUniversalTime()
            ?? throw new InvalidOperationException("Gateway timestamp khong hop le.");

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
                    $"InvoiceId={request.InvoiceId}; InvoiceNumber={invoice.InvoiceNumber}; PaymentReference={payment.PaymentReference}; GatewayProvider={gatewayProvider}; GatewayEventId={gatewayEventId}; PaymentStatus={payment.PaymentStatus}; CallbackMode={(isSimulation ? "Simulation" : "Webhook")}; GatewayTimestampUtc={gatewayTimestampUtc:O}.",
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
            ["callback_mode"] = isSimulation ? "simulation" : "webhook"
        });

        await _complianceAuditRecorder.RecordAsync(
            actorUserId,
            actorUsername ?? "gateway",
            "InvoicePaymentCallbackProcessed",
            normalizedGatewayStatus == "Captured" ? "Info" : "Warning",
            $"InvoiceId={request.InvoiceId}; InvoiceNumber={invoice.InvoiceNumber}; PaymentReference={payment.PaymentReference}; GatewayProvider={gatewayProvider}; GatewayEventId={gatewayEventId}; GatewayStatus={normalizedGatewayStatus}; CallbackMode={(isSimulation ? "Simulation" : "Webhook")}; GatewayTimestampUtc={gatewayTimestampUtc:O}.",
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

    public async Task<HospitalPaymentReconciliationSummaryDto> GetReconciliationSummaryAsync(CancellationToken ct = default)
    {
        var snapshot = await _hospitalBillingRepository.GetReconciliationSnapshotAsync(ct);
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
                Amount = x.Amount,
                PaymentStatus = x.PaymentStatus,
                PaidAtLocal = x.PaidAtUtc.HasValue ? ConvertUtcToClinicLocal(x.PaidAtUtc.Value) : null,
                ReceivedByUsername = x.ReceivedByUsername,
                ExternalTransactionId = x.ExternalTransactionId
            }).ToList()
        };
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

    private static string NormalizeGatewayStatus(string gatewayStatus)
    {
        if (string.Equals(gatewayStatus, "Captured", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(gatewayStatus, "Success", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(gatewayStatus, "Succeeded", StringComparison.OrdinalIgnoreCase))
        {
            return "Captured";
        }

        if (string.Equals(gatewayStatus, "Failed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(gatewayStatus, "Declined", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(gatewayStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return "Failed";
        }

        throw new InvalidOperationException("Gateway status khong hop le cho callback thanh toan.");
    }

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
        => $"INV-{nowUtc:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

    private static string GeneratePaymentReference(DateTime nowUtc)
        => $"PAY-{nowUtc:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

    private static string GenerateRefundReference(DateTime nowUtc)
        => $"REF-{nowUtc:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

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
