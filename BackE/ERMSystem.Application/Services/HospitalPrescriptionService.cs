using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;

namespace ERMSystem.Application.Services;

public class HospitalPrescriptionService : IHospitalPrescriptionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] AllowedStatuses = ["Issued", "Dispensed", "Cancelled"];
    private static readonly PrescriptionInteractionRule[] InteractionRules =
    [
        new(
            ["warfarin"],
            ["metronidazole", "clarithromycin", "erythromycin", "co-trimoxazole", "trimethoprim sulfamethoxazole"],
            "Canh bao tuong tac nghiem trong: khang sinh co the lam tang tac dung chong dong cua warfarin, can theo doi INR sat."),
        new(
            ["ibuprofen", "diclofenac", "naproxen", "meloxicam"],
            ["warfarin", "rivaroxaban", "apixaban", "dabigatran", "heparin", "enoxaparin"],
            "Canh bao nguy co xuat huyet: NSAID dung cung thuoc chong dong can duoc danh gia lai."),
        new(
            ["enalapril", "lisinopril", "perindopril", "ramipril", "captopril", "losartan", "valsartan", "telmisartan"],
            ["spironolactone", "potassium chloride", "kali clorid"],
            "Canh bao tang kali mau: ACEi/ARB dung cung spironolactone hoac bo sung kali can theo doi dien giai va chuc nang than."),
        new(
            ["morphine", "fentanyl", "tramadol", "codeine", "oxycodone"],
            ["diazepam", "lorazepam", "alprazolam", "clonazepam", "midazolam"],
            "Canh bao uc che ho hap/an than: opioid dung cung benzodiazepine can can nhac muc do can thiet va theo doi sat."),
        new(
            ["nitroglycerin", "isosorbide mononitrate", "isosorbide dinitrate"],
            ["sildenafil", "tadalafil", "vardenafil"],
            "Canh bao ha huyet ap nghiem trong: nitrate khong nen dung cung thuoc uc che PDE5.")
    ];
    private static readonly PrescriptionTherapeuticClassRule[] TherapeuticClassRules =
    [
        new(
            "NSAID",
            ["ibuprofen", "diclofenac", "naproxen", "meloxicam", "celecoxib", "etoricoxib", "ketorolac"],
            "Canh bao trung nhom giam dau khang viem: khong nen dung dong thoi nhieu NSAID trong cung mot don."),
        new(
            "benzodiazepine",
            ["diazepam", "lorazepam", "alprazolam", "clonazepam", "midazolam"],
            "Canh bao trung nhom an than: phoi hop nhieu benzodiazepine lam tang nguy co uc che ho hap, te nga va lan lu."),
        new(
            "opioid",
            ["morphine", "fentanyl", "tramadol", "codeine", "oxycodone"],
            "Canh bao trung nhom giam dau opioid: can xac nhan day co chu dich dieu tri va theo doi doc tinh."),
        new(
            "ACEi/ARB",
            ["enalapril", "lisinopril", "perindopril", "ramipril", "captopril", "losartan", "valsartan", "telmisartan"],
            "Canh bao trung nhom he renin-angiotensin: khong nen phoi hop nhieu ACEi/ARB neu khong co chi dinh rat ro rang.")
    ];
    private static readonly DiagnosisContextRule[] DiagnosisContextRules =
    [
        new(
            ["viem da day", "loet da day", "xuat huyet tieu hoa", "loet ta trang", "gastritis", "ulcer", "gastrointestinal bleeding"],
            ["ibuprofen", "diclofenac", "naproxen", "meloxicam", "celecoxib", "etoricoxib", "ketorolac"],
            "Canh bao theo benh canh tieu hoa: NSAID co the lam nang viem loet/xuat huyet tieu hoa."),
        new(
            ["suy than", "benh than man", "chronic kidney", "ckd", "renal failure", "tang huyet ap", "hypertension", "suy tim", "heart failure"],
            ["ibuprofen", "diclofenac", "naproxen", "meloxicam", "celecoxib", "etoricoxib", "ketorolac"],
            "Canh bao theo benh canh tim-than: NSAID co the lam xau chuc nang than, giu nuoc va tang huyet ap."),
        new(
            ["dai thao duong", "diabetes"],
            ["prednisone", "prednisolone", "dexamethasone", "methylprednisolone", "hydrocortisone"],
            "Canh bao theo benh canh chuyen hoa: corticosteroid co the lam tang duong huyet, can theo doi glucose sat.")
    ];
    private static readonly PrescriptionInteractionRule[] AdvancedInteractionRules =
    [
        new(
            ["azithromycin", "clarithromycin", "erythromycin", "levofloxacin", "moxifloxacin", "ondansetron"],
            ["amiodarone", "haloperidol", "quetiapine", "sotalol"],
            "Canh bao keo dai QT: phoi hop co the lam tang nguy co loan nhip nguy hiem."),
        new(
            ["tramadol", "sertraline", "fluoxetine", "paroxetine", "escitalopram", "venlafaxine"],
            ["tramadol", "sertraline", "fluoxetine", "paroxetine", "escitalopram", "venlafaxine"],
            "Canh bao hoi chung serotonin: can danh gia nguy co kich dong, run co, tang than nhiet va thay doi tam than."),
        new(
            ["ibuprofen", "diclofenac", "naproxen", "meloxicam", "celecoxib", "etoricoxib", "ketorolac"],
            ["furosemide", "torsemide", "hydrochlorothiazide", "spironolactone"],
            "Canh bao nguy co ton thuong than va mat kiem soat huyet ap khi phoi hop NSAID voi thuoc loi tieu.")
    ];

    private readonly IHospitalPrescriptionRepository _hospitalPrescriptionRepository;
    private readonly IHospitalIdentityBridgeService _hospitalIdentityBridgeService;
    private readonly IBusinessMetricsRecorder _businessMetricsRecorder;

    public HospitalPrescriptionService(
        IHospitalPrescriptionRepository hospitalPrescriptionRepository,
        IHospitalIdentityBridgeService hospitalIdentityBridgeService,
        IBusinessMetricsRecorder businessMetricsRecorder)
    {
        _hospitalPrescriptionRepository = hospitalPrescriptionRepository;
        _hospitalIdentityBridgeService = hospitalIdentityBridgeService;
        _businessMetricsRecorder = businessMetricsRecorder;
    }

    public Task<PaginatedResult<HospitalPrescriptionSummaryDto>> GetWorklistAsync(
        HospitalPrescriptionWorklistRequestDto request,
        CancellationToken ct = default)
        => _hospitalPrescriptionRepository.GetWorklistAsync(request, ct);

    public async Task<HospitalPrescriptionDetailDto?> GetByIdAsync(Guid prescriptionId, CancellationToken ct = default)
    {
        var prescription = await _hospitalPrescriptionRepository.GetByIdAsync(prescriptionId, ct);
        return prescription == null ? null : MapDetail(prescription);
    }

    public Task<HospitalPrescriptionEligibleEncounterDto[]> GetEligibleEncountersAsync(CancellationToken ct = default)
        => _hospitalPrescriptionRepository.GetEligibleEncountersAsync(ct);

    public Task<HospitalMedicineCatalogDto[]> GetMedicineCatalogAsync(CancellationToken ct = default)
        => _hospitalPrescriptionRepository.GetMedicineCatalogAsync(ct);

    public async Task<HospitalPrescriptionDetailDto> CreateAsync(
        CreateHospitalPrescriptionDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default)
    {
        actorUserId = await ResolveHospitalActorUserIdAsync(actorUserId, actorUsername, ct);
        if (request.Items == null || request.Items.Count == 0)
        {
            throw new InvalidOperationException("Don thuoc phai co it nhat mot thuoc.");
        }

        var normalizedStatus = NormalizeStatus(request.Status);
        var encounter = await _hospitalPrescriptionRepository.GetEncounterForPrescriptionAsync(request.EncounterId, ct);
        if (encounter == null)
        {
            throw new KeyNotFoundException("Khong tim thay encounter de phat hanh don thuoc.");
        }

        if (encounter.ExistingPrescriptionId.HasValue)
        {
            throw new InvalidOperationException("Encounter nay da co don thuoc.");
        }

        if (encounter.EncounterStatus is not ("InProgress" or "Finalized" or "Approved"))
        {
            throw new InvalidOperationException("Chi duoc phat hanh don thuoc cho encounter dang kham, da chot ho so hoac da duyet.");
        }

        var medicineIds = request.Items.Select(x => x.MedicineId).Distinct().ToArray();
        var medicines = await _hospitalPrescriptionRepository.GetMedicinesByIdsAsync(medicineIds, ct);
        if (medicines.Length != medicineIds.Length)
        {
            throw new InvalidOperationException("Co thuoc khong ton tai hoac da ngung hoat dong.");
        }

        var medicineLookup = medicines.ToDictionary(x => x.MedicineId, x => x);
        ValidatePrescriptionItems(request.Items, medicineLookup);
        var nowUtc = DateTime.UtcNow;
        var orderHeaderId = Guid.NewGuid();
        var prescriptionId = Guid.NewGuid();

        await _hospitalPrescriptionRepository.AddOrderHeaderAsync(new HospitalPrescriptionOrderHeaderCreateCommand
        {
            OrderHeaderId = orderHeaderId,
            EncounterId = encounter.EncounterId,
            OrderNumber = GenerateOrderNumber(nowUtc),
            OrderCategory = "Pharmacy",
            OrderStatus = "Ordered",
            OrderedByUserId = actorUserId,
            OrderedAtUtc = nowUtc
        }, ct);

        await _hospitalPrescriptionRepository.AddPrescriptionAsync(new HospitalPrescriptionCreateCommand
        {
            PrescriptionId = prescriptionId,
            OrderHeaderId = orderHeaderId,
            PrescriptionNumber = GeneratePrescriptionNumber(nowUtc),
            Status = normalizedStatus,
            Notes = NormalizeText(request.Notes),
            CreatedAtUtc = nowUtc
        }, ct);

        foreach (var item in request.Items)
        {
            var medicine = medicineLookup[item.MedicineId];
            await _hospitalPrescriptionRepository.AddPrescriptionItemAsync(new HospitalPrescriptionItemCreateCommand
            {
                PrescriptionItemId = Guid.NewGuid(),
                PrescriptionId = prescriptionId,
                MedicineId = item.MedicineId,
                DoseInstruction = NormalizeRequiredText(item.DoseInstruction, "Lieu dung"),
                Route = NormalizeText(item.Route),
                Frequency = NormalizeText(item.Frequency),
                DurationDays = item.DurationDays,
                Quantity = item.Quantity,
                UnitPrice = null
            }, ct);
        }

        await _hospitalPrescriptionRepository.AddOutboxMessageAsync(new HospitalPrescriptionOutboxCreateCommand
        {
            OutboxMessageId = Guid.NewGuid(),
            AggregateType = "Prescription",
            AggregateId = prescriptionId,
            EventType = "PrescriptionIssued.v1",
            PayloadJson = JsonSerializer.Serialize(new
            {
                prescriptionId,
                encounter.EncounterId,
                encounter.EncounterNumber,
                encounter.PatientId,
                encounter.PatientName,
                encounter.MedicalRecordNumber,
                phone = encounter.PatientPhone,
                email = encounter.PatientEmail,
                encounter.DoctorProfileId,
                encounter.DoctorName,
                encounter.SpecialtyName,
                status = normalizedStatus,
                items = request.Items.Select(item =>
                {
                    var medicine = medicineLookup[item.MedicineId];
                    return new
                    {
                        item.MedicineId,
                        medicine.DrugCode,
                        medicineName = medicine.Name,
                        item.DoseInstruction,
                        item.Route,
                        item.Frequency,
                        item.DurationDays,
                        item.Quantity
                    };
                }),
                issuedAtUtc = nowUtc
            }, JsonOptions),
            Status = "Pending",
            AvailableAtUtc = nowUtc
        }, ct);

        await _hospitalPrescriptionRepository.SaveChangesAsync(ct);

        _businessMetricsRecorder.IncrementEvent("hospital_prescription", "issued", new Dictionary<string, string?>
        {
            ["status"] = normalizedStatus,
            ["item_count"] = request.Items.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });

        var created = await _hospitalPrescriptionRepository.GetByIdAsync(prescriptionId, ct)
            ?? throw new InvalidOperationException("Khong the tai lai don thuoc sau khi tao.");

        return MapDetail(created);
    }

    public async Task<HospitalPrescriptionDetailDto?> DispenseAsync(
        Guid prescriptionId,
        DispenseHospitalPrescriptionDto request,
        Guid? actorUserId,
        string? actorUsername,
        CancellationToken ct = default)
    {
        actorUserId = await ResolveHospitalActorUserIdAsync(actorUserId, actorUsername, ct);
        var prescription = await _hospitalPrescriptionRepository.GetByIdAsync(prescriptionId, ct);
        if (prescription == null)
        {
            return null;
        }

        if (prescription.Status == "Cancelled")
        {
            throw new InvalidOperationException("Don thuoc da huy khong the cap thuoc.");
        }

        if (prescription.Status == "Dispensed")
        {
            throw new InvalidOperationException("Don thuoc nay da duoc cap thuoc.");
        }

        var nowUtc = DateTime.UtcNow;
        await _hospitalPrescriptionRepository.AddDispensingAsync(new HospitalPrescriptionDispensingCreateCommand
        {
            DispensingId = Guid.NewGuid(),
            PrescriptionId = prescriptionId,
            DispensingStatus = "Dispensed",
            DispensedAtUtc = nowUtc,
            DispensedByUserId = actorUserId,
            Notes = NormalizeText(request.Notes)
        }, ct);

        await _hospitalPrescriptionRepository.UpdatePrescriptionStatusAsync(prescriptionId, "Dispensed", ct);
        await _hospitalPrescriptionRepository.UpdateOrderHeaderStatusAsync(prescription.OrderHeaderId, "Completed", ct);
        await _hospitalPrescriptionRepository.AddOutboxMessageAsync(new HospitalPrescriptionOutboxCreateCommand
        {
            OutboxMessageId = Guid.NewGuid(),
            AggregateType = "Prescription",
            AggregateId = prescriptionId,
            EventType = "PrescriptionDispensed.v1",
            PayloadJson = JsonSerializer.Serialize(new
            {
                prescriptionId,
                prescription.PrescriptionNumber,
                prescription.EncounterId,
                prescription.EncounterNumber,
                prescription.PatientId,
                prescription.PatientName,
                prescription.MedicalRecordNumber,
                phone = prescription.PatientPhone,
                email = prescription.PatientEmail,
                prescription.DoctorProfileId,
                prescription.DoctorName,
                dispensedAtUtc = nowUtc,
                dispensedByUserId = actorUserId,
                notes = NormalizeText(request.Notes)
            }, JsonOptions),
            Status = "Pending",
            AvailableAtUtc = nowUtc
        }, ct);

        await _hospitalPrescriptionRepository.SaveChangesAsync(ct);

        _businessMetricsRecorder.IncrementEvent("hospital_prescription", "dispensed");

        var updated = await _hospitalPrescriptionRepository.GetByIdAsync(prescriptionId, ct)
            ?? throw new InvalidOperationException("Khong the tai lai don thuoc sau khi cap thuoc.");

        return MapDetail(updated);
    }

    public async Task DeleteAsync(Guid prescriptionId, CancellationToken ct = default)
    {
        await _hospitalPrescriptionRepository.DeletePrescriptionAsync(prescriptionId, ct);
        await _hospitalPrescriptionRepository.SaveChangesAsync(ct);
    }

    private Task<Guid?> ResolveHospitalActorUserIdAsync(Guid? actorUserId, string? actorUsername, CancellationToken ct)
        => _hospitalIdentityBridgeService.ResolveHospitalUserIdAsync(actorUserId, actorUsername, ct);

    private static HospitalPrescriptionDetailDto MapDetail(HospitalPrescriptionAggregateSnapshot prescription)
    {
        var warningDetails = BuildPrescriptionWarningDetails(prescription);

        return new HospitalPrescriptionDetailDto
        {
            PrescriptionId = prescription.PrescriptionId,
            PrescriptionNumber = prescription.PrescriptionNumber,
            Status = prescription.Status,
            LatestDispensingId = prescription.LatestDispensingId,
            LatestDispensingStatus = prescription.LatestDispensingStatus,
            EncounterId = prescription.EncounterId,
            EncounterNumber = prescription.EncounterNumber,
            PatientId = prescription.PatientId,
            PatientName = prescription.PatientName,
            MedicalRecordNumber = prescription.MedicalRecordNumber,
            DoctorProfileId = prescription.DoctorProfileId,
            DoctorName = prescription.DoctorName,
            SpecialtyName = prescription.SpecialtyName,
            ClinicName = prescription.ClinicName,
            PrimaryDiagnosisName = prescription.PrimaryDiagnosisName,
            TotalItems = prescription.Items.Length,
            CreatedAtLocal = ConvertUtcToClinicLocal(prescription.CreatedAtUtc),
            DispensedAtLocal = prescription.DispensedAtUtc.HasValue
                ? ConvertUtcToClinicLocal(prescription.DispensedAtUtc.Value)
                : null,
            DispensedByUsername = prescription.DispensedByUsername,
            DispensingNotes = prescription.DispensingNotes,
            Notes = prescription.Notes,
            WarningDetails = warningDetails,
            Warnings = warningDetails.Select(x => x.Message).ToList(),
            DispensingHistory = prescription.DispensingHistory
                .Select(dispensing => new HospitalPrescriptionDispensingHistoryDto
                {
                    DispensingId = dispensing.DispensingId,
                    DispensingStatus = dispensing.DispensingStatus,
                    DispensedAtLocal = dispensing.DispensedAtUtc.HasValue
                        ? ConvertUtcToClinicLocal(dispensing.DispensedAtUtc.Value)
                        : null,
                    DispensedByUserId = dispensing.DispensedByUserId,
                    DispensedByUsername = dispensing.DispensedByUsername,
                    Notes = dispensing.Notes
                })
                .ToList(),
            Items = prescription.Items.Select(item => new HospitalPrescriptionItemDto
            {
                PrescriptionItemId = item.PrescriptionItemId,
                MedicineId = item.MedicineId,
                DrugCode = item.DrugCode,
                MedicineName = item.MedicineName,
                GenericName = item.GenericName,
                Strength = item.Strength,
                DosageForm = item.DosageForm,
                Unit = item.Unit,
                DoseInstruction = item.DoseInstruction,
                Route = item.Route,
                Frequency = item.Frequency,
                DurationDays = item.DurationDays,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };
    }

    private static string NormalizeStatus(string? status)
    {
        var normalized = string.IsNullOrWhiteSpace(status) ? "Issued" : status.Trim();
        if (!AllowedStatuses.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Trang thai don thuoc khong hop le.");
        }

        return AllowedStatuses.First(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeRequiredText(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException($"{fieldName} la truong bat buoc.");
        }

        return normalized;
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static void ValidatePrescriptionItems(
        IReadOnlyCollection<CreateHospitalPrescriptionItemDto> items,
        IReadOnlyDictionary<Guid, HospitalMedicineSnapshot> medicineLookup)
    {
        var duplicateMedicineIds = items
            .GroupBy(x => x.MedicineId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();
        if (duplicateMedicineIds.Length > 0)
        {
            throw new InvalidOperationException("Khong duoc ke trung mot thuoc nhieu lan trong cung don.");
        }

        foreach (var item in items)
        {
            var medicine = medicineLookup[item.MedicineId];
            var doseInstruction = NormalizeRequiredText(item.DoseInstruction, "Lieu dung");
            if (!TryExtractPositiveNumber(doseInstruction, out var dosePerAdministration))
            {
                throw new InvalidOperationException(
                    $"Lieu dung cua thuoc {medicine.Name} phai chua so luong hop le, vi du '1 vien/lần'.");
            }

            if (medicine.IsControlled && item.DurationDays.HasValue && item.DurationDays.Value > 30)
            {
                throw new InvalidOperationException(
                    $"Thuoc kiem soat dac biet {medicine.Name} khong duoc ke qua 30 ngay.");
            }

            var frequency = NormalizeText(item.Frequency);
            if (!string.IsNullOrWhiteSpace(frequency) &&
                item.DurationDays.HasValue &&
                TryExtractPositiveNumber(frequency, out var administrationsPerDay))
            {
                var minimumQuantity = Math.Ceiling(dosePerAdministration * administrationsPerDay * item.DurationDays.Value);
                if (item.Quantity < minimumQuantity)
                {
                    throw new InvalidOperationException(
                        $"So luong thuoc {medicine.Name} khong du cho lieu trinh toi thieu {minimumQuantity:0.##} {medicine.Unit ?? "don vi"}.");
                }
            }
        }
    }

    private static List<HospitalPrescriptionWarningDto> BuildPrescriptionWarningDetails(HospitalPrescriptionAggregateSnapshot prescription)
    {
        var warnings = new List<HospitalPrescriptionWarningDto>();
        var normalizedItems = prescription.Items
            .Select(item => new NormalizedPrescriptionItemSnapshot(
                item,
                NormalizeMedicationDescriptor(item.MedicineName),
                NormalizeMedicationDescriptor(item.GenericName)))
            .ToArray();
        var normalizedDiagnoses = prescription.DiagnosisNames
            .Select(NormalizeMedicationDescriptor)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        var duplicateGenericGroups = normalizedItems
            .Where(x => !string.IsNullOrWhiteSpace(x.Source.GenericName))
            .GroupBy(x => x.Source.GenericName!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Select(item => item.MedicineId).Distinct().Count() > 1)
            .ToArray();

        foreach (var group in duplicateGenericGroups)
        {
            var medicineNames = string.Join(", ", group.Select(x => x.Source.MedicineName).Distinct(StringComparer.OrdinalIgnoreCase));
            AddUniqueWarning(warnings, new HospitalPrescriptionWarningDto
            {
                Code = $"duplicate-generic:{NormalizeCodeToken(group.Key)}",
                Severity = "warning",
                Category = "duplicate-ingredient",
                Message = $"Canh bao trung hoat chat: {group.Key} xuat hien trong cac thuoc {medicineNames}.",
                Recommendation = "Can xac nhan day khong phai ke trung hoat chat ngoai y muon.",
                RelatedMedicines = group.Select(x => x.Source.MedicineName).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            });
        }

        foreach (var rule in InteractionRules)
        {
            if (!TryFindInteractionPair(normalizedItems, rule, out var primaryMatch, out var secondaryMatch))
            {
                continue;
            }

            AddInteractionWarning(
                warnings,
                "major-drug-interaction",
                "critical",
                rule.WarningMessage,
                primaryMatch,
                secondaryMatch,
                "Can can nhac doi phac do hoac them ke hoach theo doi sat.");
        }

        foreach (var rule in AdvancedInteractionRules)
        {
            if (!TryFindInteractionPair(normalizedItems, rule, out var primaryMatch, out var secondaryMatch))
            {
                continue;
            }

            AddInteractionWarning(
                warnings,
                "advanced-interaction",
                rule.WarningMessage.Contains("QT", StringComparison.OrdinalIgnoreCase) ? "critical" : "warning",
                rule.WarningMessage,
                primaryMatch,
                secondaryMatch,
                "Can danh gia nguy co-loi ich va xem xet theo doi lam sang/can lam sang phu hop.");
        }

        foreach (var rule in TherapeuticClassRules)
        {
            var matches = normalizedItems
                .Where(item => rule.Matchers.Any(item.ContainsToken))
                .GroupBy(item => item.MedicineId)
                .Select(group => group.First())
                .ToArray();
            if (matches.Length < 2)
            {
                continue;
            }

            var medicineNames = string.Join(", ", matches.Select(x => x.Source.MedicineName));
            AddUniqueWarning(warnings, new HospitalPrescriptionWarningDto
            {
                Code = $"duplicate-class:{NormalizeCodeToken(rule.ClassName)}",
                Severity = "warning",
                Category = "therapeutic-duplication",
                Message = $"{rule.WarningMessage} Thuoc lien quan: {medicineNames}.",
                Recommendation = "Can xac nhan phoi hop cung nhom co muc tieu dieu tri ro rang.",
                RelatedMedicines = matches.Select(x => x.Source.MedicineName).ToList()
            });
        }

        foreach (var rule in DiagnosisContextRules)
        {
            if (!normalizedDiagnoses.Any(diagnosis => rule.DiagnosisMatchers.Any(matcher => diagnosis.Contains(NormalizeMedicationDescriptor(matcher), StringComparison.Ordinal))))
            {
                continue;
            }

            var matches = normalizedItems
                .Where(item => rule.MedicineMatchers.Any(item.ContainsToken))
                .GroupBy(item => item.MedicineId)
                .Select(group => group.First())
                .ToArray();
            if (matches.Length == 0)
            {
                continue;
            }

            var medicineNames = string.Join(", ", matches.Select(x => x.Source.MedicineName));
            AddUniqueWarning(warnings, new HospitalPrescriptionWarningDto
            {
                Code = $"diagnosis-context:{NormalizeCodeToken(rule.WarningMessage)}",
                Severity = "warning",
                Category = "diagnosis-context",
                Message = $"{rule.WarningMessage} Thuoc lien quan: {medicineNames}.",
                Recommendation = "Can doi chieu chan doan hien tai va muc tieu dieu tri truoc khi giu phac do.",
                RelatedMedicines = matches.Select(x => x.Source.MedicineName).ToList()
            });
        }

        var controlledItems = normalizedItems
            .Where(x => x.Source.DurationDays.HasValue && x.Source.DurationDays.Value >= 14)
            .Where(x => IsControlledSedative(x) || IsControlledAnalgesic(x))
            .ToArray();
        foreach (var item in controlledItems)
        {
            AddUniqueWarning(warnings, new HospitalPrescriptionWarningDto
            {
                Code = $"prolonged-controlled:{item.MedicineId}",
                Severity = "warning",
                Category = "duration-risk",
                Message = $"Canh bao theo doi keo dai: {item.Source.MedicineName} co lieu trinh {item.Source.DurationDays} ngay, can xac nhan chi dinh va ke hoach tai kham.",
                Recommendation = "Can ghi ro moc tai kham, muc tieu dung thuoc va tieu chi giam/ngung thuoc.",
                RelatedMedicines = [item.Source.MedicineName]
            });
        }

        var prolongedSteroids = normalizedItems
            .Where(item => item.Source.DurationDays.HasValue && item.Source.DurationDays.Value >= 14)
            .Where(IsSystemicCorticosteroid)
            .ToArray();
        foreach (var item in prolongedSteroids)
        {
            AddUniqueWarning(warnings, new HospitalPrescriptionWarningDto
            {
                Code = $"prolonged-steroid:{item.MedicineId}",
                Severity = "warning",
                Category = "duration-risk",
                Message = $"Canh bao corticosteroid keo dai: {item.Source.MedicineName} du kien dung {item.Source.DurationDays} ngay, can xem xet du phong bien chung va ke hoach giam lieu neu phu hop.",
                Recommendation = "Can xem xet du phong bien chung va ke hoach taper neu phac do cho phep.",
                RelatedMedicines = [item.Source.MedicineName]
            });
        }

        var acetaminophenItems = normalizedItems
            .Where(item => item.ContainsToken("paracetamol") || item.ContainsToken("acetaminophen"))
            .GroupBy(item => item.MedicineId)
            .Select(group => group.First())
            .ToArray();
        if (acetaminophenItems.Length > 1)
        {
            var medicineNames = string.Join(", ", acetaminophenItems.Select(x => x.Source.MedicineName));
            AddUniqueWarning(warnings, new HospitalPrescriptionWarningDto
            {
                Code = "duplicate-acetaminophen",
                Severity = "critical",
                Category = "duplicate-ingredient",
                Message = $"Canh bao trung thanh phan giam dau-ha sot: paracetamol/acetaminophen co trong cac thuoc {medicineNames}, can tranh vuot lieu toi da hang ngay.",
                Recommendation = "Can tinh tong lieu paracetamol hang ngay va loai bo thuoc trung lap neu khong can thiet.",
                RelatedMedicines = acetaminophenItems.Select(x => x.Source.MedicineName).ToList()
            });
        }

        if (TryCalculateAge(prescription.PatientDateOfBirth, out var patientAge))
        {
            if (patientAge < 18)
            {
                var aspirinItems = normalizedItems.Where(item => item.ContainsToken("aspirin")).ToArray();
                foreach (var item in aspirinItems)
                {
                    AddUniqueWarning(warnings, new HospitalPrescriptionWarningDto
                    {
                        Code = $"age-aspirin:{item.MedicineId}",
                        Severity = "warning",
                        Category = "age-risk",
                        Message = $"Canh bao theo do tuoi: {item.Source.MedicineName} can than trong benh nhan duoi 18 tuoi, dac biet neu co benh canh nhiem virus.",
                        Recommendation = "Can xac nhan chi dinh va can nhac lua chon an toan hon neu phu hop.",
                        RelatedMedicines = [item.Source.MedicineName]
                    });
                }
            }

            if (patientAge < 8)
            {
                var tetracyclineItems = normalizedItems
                    .Where(item => item.ContainsToken("tetracycline") || item.ContainsToken("doxycycline"))
                    .ToArray();
                foreach (var item in tetracyclineItems)
                {
                    AddUniqueWarning(warnings, new HospitalPrescriptionWarningDto
                    {
                        Code = $"age-tetracycline:{item.MedicineId}",
                        Severity = "warning",
                        Category = "age-risk",
                        Message = $"Canh bao theo do tuoi: {item.Source.MedicineName} khong phai lua chon uu tien cho tre nho duoi 8 tuoi neu khong co chi dinh dac biet.",
                        Recommendation = "Can can nhac khang sinh thay the phu hop voi lua tuoi neu co the.",
                        RelatedMedicines = [item.Source.MedicineName]
                    });
                }
            }

            if (patientAge >= 65)
            {
                var sedativeItems = normalizedItems.Where(item => IsControlledSedative(item) || IsControlledAnalgesic(item)).ToArray();
                if (sedativeItems.Length > 0)
                {
                    var medicineNames = string.Join(", ", sedativeItems.Select(x => x.Source.MedicineName));
                    AddUniqueWarning(warnings, new HospitalPrescriptionWarningDto
                    {
                        Code = "geriatric-sedative-risk",
                        Severity = "warning",
                        Category = "geriatric-risk",
                        Message = $"Canh bao nguoi cao tuoi: {medicineNames} co the lam tang nguy co te nga, lan lu va uc che ho hap; can bat dau lieu than trong va danh gia lai som.",
                        Recommendation = "Can uu tien lieu thap, theo doi sat va danh gia lai som sau khi ke don.",
                        RelatedMedicines = sedativeItems.Select(x => x.Source.MedicineName).ToList()
                    });
                }

                var anticholinergicItems = normalizedItems
                    .Where(item => item.ContainsToken("diphenhydramine")
                                   || item.ContainsToken("chlorpheniramine")
                                   || item.ContainsToken("promethazine"))
                    .ToArray();
                if (anticholinergicItems.Length > 0)
                {
                    AddUniqueWarning(warnings, new HospitalPrescriptionWarningDto
                    {
                        Code = "geriatric-anticholinergic-risk",
                        Severity = "warning",
                        Category = "geriatric-risk",
                        Message = $"Canh bao nguoi cao tuoi: {string.Join(", ", anticholinergicItems.Select(x => x.Source.MedicineName))} co the lam tang nguy co lan lu, kho tieu va bi tieu.",
                        Recommendation = "Can can nhac thuoc thay the it tac dung khang cholinergic hon neu phu hop.",
                        RelatedMedicines = anticholinergicItems.Select(x => x.Source.MedicineName).ToList()
                    });
                }

                var nsaidItems = normalizedItems
                    .Where(item => TherapeuticClassRules.First(x => x.ClassName == "NSAID").Matchers.Any(item.ContainsToken))
                    .ToArray();
                var raasItems = normalizedItems
                    .Where(item => TherapeuticClassRules.First(x => x.ClassName == "ACEi/ARB").Matchers.Any(item.ContainsToken))
                    .ToArray();
                var diureticItems = normalizedItems
                    .Where(item => item.ContainsToken("furosemide")
                                   || item.ContainsToken("torsemide")
                                   || item.ContainsToken("hydrochlorothiazide")
                                   || item.ContainsToken("spironolactone"))
                    .ToArray();
                if (nsaidItems.Length > 0 && raasItems.Length > 0 && diureticItems.Length > 0)
                {
                    AddUniqueWarning(warnings, new HospitalPrescriptionWarningDto
                    {
                        Code = "triple-whammy-aki-risk",
                        Severity = "critical",
                        Category = "renal-risk",
                        Message = "Canh bao nguy co suy than cap: phoi hop NSAID, ACEi/ARB va loi tieu trong cung don can duoc danh gia rat chat.",
                        Recommendation = "Can can nhac rut gon phac do va theo doi creatinine, dien giai va huyet ap neu buoc phai dung.",
                        RelatedMedicines = nsaidItems.Select(x => x.Source.MedicineName)
                            .Concat(raasItems.Select(x => x.Source.MedicineName))
                            .Concat(diureticItems.Select(x => x.Source.MedicineName))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList()
                    });
                }
            }
        }

        return warnings;
    }

    private static void AddUniqueWarning(ICollection<HospitalPrescriptionWarningDto> warnings, HospitalPrescriptionWarningDto warning)
    {
        if (warnings.Any(x => string.Equals(x.Code, warning.Code, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        warnings.Add(warning);
    }

    private static void AddInteractionWarning(
        ICollection<HospitalPrescriptionWarningDto> warnings,
        string category,
        string severity,
        string baseMessage,
        NormalizedPrescriptionItemSnapshot primaryMatch,
        NormalizedPrescriptionItemSnapshot secondaryMatch,
        string recommendation)
    {
        AddUniqueWarning(warnings, new HospitalPrescriptionWarningDto
        {
            Code = $"{category}:{NormalizeCodeToken(primaryMatch.Source.MedicineName)}:{NormalizeCodeToken(secondaryMatch.Source.MedicineName)}",
            Severity = severity,
            Category = category,
            Message = $"{baseMessage} Cap thuoc lien quan: {primaryMatch.Source.MedicineName} + {secondaryMatch.Source.MedicineName}.",
            Recommendation = recommendation,
            RelatedMedicines = new[] { primaryMatch.Source.MedicineName, secondaryMatch.Source.MedicineName }
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        });
    }

    private static bool TryFindInteractionPair(
        IReadOnlyCollection<NormalizedPrescriptionItemSnapshot> items,
        PrescriptionInteractionRule rule,
        out NormalizedPrescriptionItemSnapshot primaryMatch,
        out NormalizedPrescriptionItemSnapshot secondaryMatch)
    {
        foreach (var primary in items)
        {
            if (!rule.PrimaryMatchers.Any(matcher => primary.ContainsToken(matcher)))
            {
                continue;
            }

            foreach (var secondary in items)
            {
                if (primary.MedicineId == secondary.MedicineId)
                {
                    continue;
                }

                if (!rule.SecondaryMatchers.Any(matcher => secondary.ContainsToken(matcher)))
                {
                    continue;
                }

                primaryMatch = primary;
                secondaryMatch = secondary;
                return true;
            }
        }

        primaryMatch = default;
        secondaryMatch = default;
        return false;
    }

    private static bool IsControlledSedative(NormalizedPrescriptionItemSnapshot item)
        => item.ContainsToken("diazepam")
           || item.ContainsToken("lorazepam")
           || item.ContainsToken("alprazolam")
           || item.ContainsToken("clonazepam")
           || item.ContainsToken("midazolam");

    private static bool IsControlledAnalgesic(NormalizedPrescriptionItemSnapshot item)
        => item.ContainsToken("morphine")
           || item.ContainsToken("fentanyl")
           || item.ContainsToken("tramadol")
           || item.ContainsToken("codeine")
           || item.ContainsToken("oxycodone");

    private static bool IsSystemicCorticosteroid(NormalizedPrescriptionItemSnapshot item)
        => item.ContainsToken("prednisone")
           || item.ContainsToken("prednisolone")
           || item.ContainsToken("dexamethasone")
           || item.ContainsToken("methylprednisolone")
           || item.ContainsToken("hydrocortisone");

    private static string NormalizeMedicationDescriptor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();
    }

    private static string NormalizeCodeToken(string? value)
        => Regex.Replace((value ?? string.Empty).Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');

    private static bool TryExtractPositiveNumber(string input, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var match = Regex.Match(input, @"(?<!\d)(\d+(?:[.,]\d+)?)");
        if (!match.Success)
        {
            return false;
        }

        var normalized = match.Groups[1].Value.Replace(',', '.');
        return decimal.TryParse(
                   normalized,
                   System.Globalization.NumberStyles.Number,
                   System.Globalization.CultureInfo.InvariantCulture,
                   out value)
               && value > 0;
    }

    private static bool TryCalculateAge(DateOnly dateOfBirth, out int age)
    {
        age = 0;
        if (dateOfBirth == default)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age))
        {
            age--;
        }

        return age >= 0;
    }

    private static string GenerateOrderNumber(DateTime nowUtc)
        => $"ORD-PHA-{nowUtc:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

    private static string GeneratePrescriptionNumber(DateTime nowUtc)
        => $"RX-{nowUtc:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

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

    private readonly record struct PrescriptionInteractionRule(
        string[] PrimaryMatchers,
        string[] SecondaryMatchers,
        string WarningMessage);

    private readonly record struct PrescriptionTherapeuticClassRule(
        string ClassName,
        string[] Matchers,
        string WarningMessage);

    private readonly record struct DiagnosisContextRule(
        string[] DiagnosisMatchers,
        string[] MedicineMatchers,
        string WarningMessage);

    private readonly record struct NormalizedPrescriptionItemSnapshot(
        HospitalPrescriptionItemSnapshot Source,
        string NormalizedMedicineName,
        string NormalizedGenericName)
    {
        public Guid MedicineId => Source.MedicineId;

        public bool ContainsToken(string token)
        {
            var normalizedToken = NormalizeMedicationDescriptor(token);
            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                return false;
            }

            return NormalizedMedicineName.Contains(normalizedToken, StringComparison.Ordinal)
                || NormalizedGenericName.Contains(normalizedToken, StringComparison.Ordinal);
        }
    }
}
