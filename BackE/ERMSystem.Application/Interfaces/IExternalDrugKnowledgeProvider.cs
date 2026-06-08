using ERMSystem.Application.DTOs;

namespace ERMSystem.Application.Interfaces;

public interface IExternalDrugKnowledgeProvider
{
    Task<IReadOnlyCollection<HospitalPrescriptionWarningDto>> EvaluatePrescriptionAsync(
        HospitalPrescriptionAggregateSnapshot prescription,
        IReadOnlyCollection<HospitalPrescriptionWarningDto> internalWarnings,
        CancellationToken ct = default);
}
