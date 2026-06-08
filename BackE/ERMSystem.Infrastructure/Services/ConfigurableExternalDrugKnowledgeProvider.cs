using System.Net.Http.Json;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace ERMSystem.Infrastructure.Services;

public sealed class ConfigurableExternalDrugKnowledgeProvider : IExternalDrugKnowledgeProvider
{
    private readonly HttpClient _httpClient;
    private readonly ExternalDrugKnowledgeOptions _options;

    public ConfigurableExternalDrugKnowledgeProvider(
        HttpClient httpClient,
        IOptions<ExternalDrugKnowledgeOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<IReadOnlyCollection<HospitalPrescriptionWarningDto>> EvaluatePrescriptionAsync(
        HospitalPrescriptionAggregateSnapshot prescription,
        IReadOnlyCollection<HospitalPrescriptionWarningDto> internalWarnings,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return Array.Empty<HospitalPrescriptionWarningDto>();
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ResolveEvaluationUri())
            {
                Content = JsonContent.Create(BuildRequest(prescription, internalWarnings))
            };

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.TryAddWithoutValidation(
                    _options.ApiKeyHeaderName.Trim(),
                    _options.ApiKey.Trim());
            }

            using var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var providerResponse = await response.Content.ReadFromJsonAsync<ExternalDrugKnowledgeResponse>(cancellationToken: ct);
            return providerResponse?.Warnings?
                .Select(MapProviderWarning)
                .Where(warning => !string.IsNullOrWhiteSpace(warning.Message))
                .ToArray()
                ?? Array.Empty<HospitalPrescriptionWarningDto>();
        }
        catch
        {
            if (_options.FailOnProviderError)
            {
                throw;
            }

            return _options.IncludeProviderAvailabilityWarning
                ? [BuildProviderUnavailableWarning()]
                : Array.Empty<HospitalPrescriptionWarningDto>();
        }
    }

    private Uri ResolveEvaluationUri()
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var path = string.IsNullOrWhiteSpace(_options.EvaluationPath)
            ? "/clinical-decision-support/prescriptions/evaluate"
            : _options.EvaluationPath.Trim();

        return new Uri($"{baseUrl}/{path.TrimStart('/')}", UriKind.Absolute);
    }

    private ExternalDrugKnowledgeRequest BuildRequest(
        HospitalPrescriptionAggregateSnapshot prescription,
        IReadOnlyCollection<HospitalPrescriptionWarningDto> internalWarnings)
        => new()
        {
            PrescriptionId = prescription.PrescriptionId,
            PrescriptionNumber = prescription.PrescriptionNumber,
            Patient = new ExternalDrugKnowledgePatient
            {
                PatientId = prescription.PatientId,
                DateOfBirth = prescription.PatientDateOfBirth,
                Gender = prescription.PatientGender,
                Diagnoses = prescription.DiagnosisNames
            },
            Encounter = new ExternalDrugKnowledgeEncounter
            {
                EncounterId = prescription.EncounterId,
                EncounterNumber = prescription.EncounterNumber,
                PrimaryDiagnosisName = prescription.PrimaryDiagnosisName
            },
            Items = prescription.Items.Select(item => new ExternalDrugKnowledgeItem
            {
                MedicineId = item.MedicineId,
                DrugCode = item.DrugCode,
                MedicineName = item.MedicineName,
                GenericName = item.GenericName,
                Strength = item.Strength,
                DosageForm = item.DosageForm,
                DoseInstruction = item.DoseInstruction,
                Route = item.Route,
                Frequency = item.Frequency,
                DurationDays = item.DurationDays,
                Quantity = item.Quantity
            }).ToArray(),
            LabResults = prescription.LabResults.Select(lab => new ExternalDrugKnowledgeLabResult
            {
                AnalyteCode = lab.AnalyteCode,
                AnalyteName = lab.AnalyteName,
                ResultValue = lab.ResultValue,
                Unit = lab.Unit,
                ReferenceRange = lab.ReferenceRange,
                AbnormalFlag = lab.AbnormalFlag,
                VerifiedAtUtc = lab.VerifiedAtUtc
            }).ToArray(),
            InternalWarnings = _options.IncludeInternalWarnings
                ? internalWarnings.ToArray()
                : Array.Empty<HospitalPrescriptionWarningDto>()
        };

    private HospitalPrescriptionWarningDto MapProviderWarning(ExternalDrugKnowledgeWarning warning)
    {
        var providerName = string.IsNullOrWhiteSpace(warning.Provider)
            ? _options.ProviderName.Trim()
            : warning.Provider.Trim();
        var code = string.IsNullOrWhiteSpace(warning.Code)
            ? $"{providerName}:external-drug-warning"
            : $"{providerName}:{warning.Code.Trim()}";

        return new HospitalPrescriptionWarningDto
        {
            Code = code,
            Severity = NormalizeSeverity(warning.Severity),
            Category = string.IsNullOrWhiteSpace(warning.Category)
                ? "external-drug-knowledge"
                : warning.Category.Trim(),
            Message = warning.Message?.Trim() ?? string.Empty,
            Recommendation = warning.Recommendation?.Trim(),
            RelatedMedicines = warning.RelatedMedicines?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? []
        };
    }

    private HospitalPrescriptionWarningDto BuildProviderUnavailableWarning()
        => new()
        {
            Code = $"{_options.ProviderName}:provider-unavailable",
            Severity = "warning",
            Category = "external-drug-knowledge",
            Message = $"Nguon du lieu thuoc ngoai he thong '{_options.ProviderName}' hien khong kha dung, he thong dang chi dung rule noi bo.",
            Recommendation = "Can xac minh lai voi nguon CDS duoc cap phep hoac duoc duoc si duyet neu don co nguy co cao."
        };

    private static string NormalizeSeverity(string? severity)
    {
        var normalized = severity?.Trim().ToLowerInvariant();
        return normalized is "critical" or "warning" or "info" ? normalized : "warning";
    }
}

public sealed class ExternalDrugKnowledgeRequest
{
    public Guid PrescriptionId { get; set; }
    public string PrescriptionNumber { get; set; } = string.Empty;
    public ExternalDrugKnowledgePatient Patient { get; set; } = new();
    public ExternalDrugKnowledgeEncounter Encounter { get; set; } = new();
    public ExternalDrugKnowledgeItem[] Items { get; set; } = Array.Empty<ExternalDrugKnowledgeItem>();
    public ExternalDrugKnowledgeLabResult[] LabResults { get; set; } = Array.Empty<ExternalDrugKnowledgeLabResult>();
    public HospitalPrescriptionWarningDto[] InternalWarnings { get; set; } = Array.Empty<HospitalPrescriptionWarningDto>();
}

public sealed class ExternalDrugKnowledgePatient
{
    public Guid PatientId { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string[] Diagnoses { get; set; } = Array.Empty<string>();
}

public sealed class ExternalDrugKnowledgeEncounter
{
    public Guid EncounterId { get; set; }
    public string EncounterNumber { get; set; } = string.Empty;
    public string? PrimaryDiagnosisName { get; set; }
}

public sealed class ExternalDrugKnowledgeItem
{
    public Guid MedicineId { get; set; }
    public string DrugCode { get; set; } = string.Empty;
    public string MedicineName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string? DosageForm { get; set; }
    public string DoseInstruction { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Frequency { get; set; }
    public int? DurationDays { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class ExternalDrugKnowledgeLabResult
{
    public string? AnalyteCode { get; set; }
    public string AnalyteName { get; set; } = string.Empty;
    public string? ResultValue { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public string? AbnormalFlag { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
}

public sealed class ExternalDrugKnowledgeResponse
{
    public ExternalDrugKnowledgeWarning[] Warnings { get; set; } = Array.Empty<ExternalDrugKnowledgeWarning>();
}

public sealed class ExternalDrugKnowledgeWarning
{
    public string? Provider { get; set; }
    public string? Code { get; set; }
    public string? Severity { get; set; }
    public string? Category { get; set; }
    public string? Message { get; set; }
    public string? Recommendation { get; set; }
    public string[]? RelatedMedicines { get; set; }
}
