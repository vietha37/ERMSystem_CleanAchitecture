using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;

namespace ERMSystem.Application.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly IDoctorRepository _doctorRepository;

        public DoctorService(IDoctorRepository doctorRepository)
        {
            _doctorRepository = doctorRepository;
        }

        public async Task<PaginatedResult<DoctorDto>> GetAllDoctorsAsync(PaginationRequest request, CancellationToken ct = default)
        {
            var (items, totalCount) = await _doctorRepository.GetPagedAsync(request.PageNumber, request.PageSize, ct);
            return new PaginatedResult<DoctorDto>(items.Select(MapToDto), totalCount, request.PageNumber, request.PageSize);
        }

        public async Task<DoctorDto?> GetDoctorByIdAsync(Guid id, CancellationToken ct = default)
        {
            var doctor = await _doctorRepository.GetByIdAsync(id, ct);
            return doctor == null ? null : MapToDto(doctor);
        }

        public async Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto dto, CancellationToken ct = default)
        {
            var doctor = Doctor.Create(dto.FullName, dto.Specialty);

            await _doctorRepository.AddAsync(doctor, ct);
            return MapToDto(doctor);
        }

        public async Task UpdateDoctorAsync(Guid id, UpdateDoctorDto dto, CancellationToken ct = default)
        {
            var doctor = await _doctorRepository.GetByIdAsync(id, ct);
            if (doctor == null)
                throw new KeyNotFoundException($"Doctor with ID {id} not found.");

            doctor.Update(dto.FullName, dto.Specialty);

            await _doctorRepository.UpdateAsync(doctor, ct);
        }

        public async Task DeleteDoctorAsync(Guid id, CancellationToken ct = default)
        {
            var doctor = await _doctorRepository.GetByIdAsync(id, ct);
            if (doctor == null)
                throw new KeyNotFoundException($"Doctor with ID {id} not found.");

            await _doctorRepository.DeleteAsync(doctor, ct);
        }

        private static DoctorDto MapToDto(Doctor d) => new DoctorDto
        {
            Id = d.Id,
            FullName = d.FullName,
            Specialty = d.Specialty
        };
    }
}
