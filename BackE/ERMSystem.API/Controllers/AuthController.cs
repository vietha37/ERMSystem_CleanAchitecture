using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace ERMSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("auth-fixed-window")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var response = await _authService.RegisterAsync(registerDto);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/auth/patient-register
        [HttpPost("patient-register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterPatient([FromBody] PatientRegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var response = await _authService.RegisterPatientAsync(registerDto);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        // POST: api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var response = await _authService.LoginAsync(loginDto);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (InvalidOperationException ex) when (string.Equals(ex.Message, "Too many failed login attempts. Please retry later.", StringComparison.Ordinal))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    error = "suspicious_login_detected",
                    message = ex.Message
                });
            }
        }

        // POST: api/auth/verify-mfa-login
        [HttpPost("verify-mfa-login")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyMfaLogin([FromBody] VerifyMfaLoginDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var response = await _authService.VerifyMfaLoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        // POST: api/auth/refresh
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var response = await _authService.RefreshTokenAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _authService.LogoutAsync(request);
            return NoContent();
        }

        // POST: api/auth/forgot-password
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _authService.ForgotPasswordAsync(request);
            return Ok(new
            {
                message = "If the account exists, a password reset instruction has been generated."
            });
        }

        // POST: api/auth/reset-password
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _authService.ResetPasswordAsync(request);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        // POST: api/auth/logout-all
        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAll()
        {
            var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? User.FindFirstValue(ClaimTypes.Name)
                            ?? User.FindFirstValue("sub");

            if (!Guid.TryParse(rawUserId, out var userId))
            {
                return Unauthorized("Invalid token subject.");
            }

            await _authService.LogoutAllAsync(userId);
            return NoContent();
        }

        // GET: api/auth/mfa/status
        [HttpGet("mfa/status")]
        [Authorize]
        public async Task<IActionResult> GetMfaStatus()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized("Invalid token subject.");
            }

            try
            {
                return Ok(await _authService.GetMfaStatusAsync(userId));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        // POST: api/auth/mfa/setup
        [HttpPost("mfa/setup")]
        [Authorize(Roles = "Admin,Doctor,Cashier")]
        public async Task<IActionResult> SetupMfa()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized("Invalid token subject.");
            }

            try
            {
                return Ok(await _authService.SetupMfaAsync(userId));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        // POST: api/auth/mfa/enable
        [HttpPost("mfa/enable")]
        [Authorize(Roles = "Admin,Doctor,Cashier")]
        public async Task<IActionResult> EnableMfa([FromBody] VerifyMfaCodeDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized("Invalid token subject.");
            }

            try
            {
                return Ok(await _authService.EnableMfaAsync(userId, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        // POST: api/auth/mfa/disable
        [HttpPost("mfa/disable")]
        [Authorize(Roles = "Admin,Doctor,Cashier")]
        public async Task<IActionResult> DisableMfa([FromBody] VerifyMfaCodeDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized("Invalid token subject.");
            }

            try
            {
                return Ok(await _authService.DisableMfaAsync(userId, request));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("sub");

            return Guid.TryParse(rawUserId, out userId);
        }
    }
}
