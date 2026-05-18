using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERMSystem.Application.Authorization;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;

namespace ERMSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientsController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        // GET: api/patients
        [HttpGet]
        [Authorize(Policy = AppPermissions.Patients.Read)]
        public async Task<IActionResult> GetAllPatients([FromQuery] PaginationRequest request, CancellationToken ct)
        {
            var result = await _patientService.GetAllPatientsAsync(request, ct);
            return Ok(result);
        }

        // GET: api/patients/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = AppPermissions.Patients.Read)]
        public async Task<IActionResult> GetPatientById(Guid id, CancellationToken ct)
        {
            var patient = await _patientService.GetPatientByIdAsync(id, ct);
            if (patient == null)
                return NotFound($"Patient with ID {id} not found.");
            return Ok(patient);
        }

        // GET: api/patients/{id}/potential-duplicates
        [HttpGet("{id}/potential-duplicates")]
        [Authorize(Policy = AppPermissions.Patients.Read)]
        public async Task<IActionResult> GetPotentialDuplicates(Guid id, CancellationToken ct)
        {
            try
            {
                var result = await _patientService.GetPotentialDuplicatesAsync(id, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // GET: api/patients/me
        [HttpGet("me")]
        [Authorize(Policy = AppPermissions.Patients.SelfRead)]
        public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        {
            var userIdRaw = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdRaw, out var userId))
            {
                return Unauthorized("Invalid user context.");
            }

            var patient = await _patientService.GetPatientByAppUserIdAsync(userId, ct);
            if (patient == null)
            {
                return NotFound("Patient profile not found.");
            }

            return Ok(patient);
        }

        // POST: api/patients
        [HttpPost]
        [Authorize(Policy = AppPermissions.Patients.Create)]
        public async Task<IActionResult> CreatePatient([FromBody] CreatePatientDto createPatientDto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdPatient = await _patientService.CreatePatientAsync(createPatientDto, ct);
            return CreatedAtAction(nameof(GetPatientById), new { id = createdPatient.Id }, createdPatient);
        }

        // PUT: api/patients/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = AppPermissions.Patients.Update)]
        public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] UpdatePatientDto updatePatientDto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _patientService.UpdatePatientAsync(id, updatePatientDto, ct);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }

            return NoContent();
        }

        // POST: api/patients/merge
        [HttpPost("merge")]
        [Authorize(Policy = AppPermissions.Patients.Merge)]
        public async Task<IActionResult> MergePatients([FromBody] MergePatientsRequestDto request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _patientService.MergePatientsAsync(
                    request,
                    ResolveActorUserId(),
                    ResolveActorUsername(),
                    ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // DELETE: api/patients/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = AppPermissions.Patients.Delete)]
        public async Task<IActionResult> DeletePatient(Guid id, CancellationToken ct)
        {
            try
            {
                await _patientService.DeletePatientAsync(id, ct);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }

            return NoContent();
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
}
