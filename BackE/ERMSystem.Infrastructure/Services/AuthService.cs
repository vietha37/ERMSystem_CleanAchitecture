using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ERMSystem.Application.Authorization;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace ERMSystem.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IAuthSecurityMonitor _authSecurityMonitor;
        private readonly IHospitalIdentityBridgeService _hospitalIdentityBridgeService;
        private readonly HospitalDbContext _hospitalDbContext;
        private readonly IDistributedCache _distributedCache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IPatientRepository patientRepository,
            IAuthSecurityMonitor authSecurityMonitor,
            IHospitalIdentityBridgeService hospitalIdentityBridgeService,
            HospitalDbContext hospitalDbContext,
            IDistributedCache distributedCache,
            ILogger<AuthService> logger,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _patientRepository = patientRepository;
            _authSecurityMonitor = authSecurityMonitor;
            _hospitalIdentityBridgeService = hospitalIdentityBridgeService;
            _hospitalDbContext = hospitalDbContext;
            _distributedCache = distributedCache;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            var userExists = await _userRepository.UsernameExistsAsync(registerDto.Username);
            if (userExists)
                throw new InvalidOperationException($"Username '{registerDto.Username}' is already taken.");

            if (!Array.Exists(AppRole.Internal, r => r == registerDto.Role))
                throw new ArgumentException($"Invalid role '{registerDto.Role}'. Must be Admin, Doctor, or Receptionist.");

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Username = registerDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Role = registerDto.Role
            };

            await _userRepository.AddAsync(user);
            try
            {
                await _hospitalIdentityBridgeService.SyncInternalUserAsync(user);
            }
            catch
            {
                await _userRepository.DeleteAsync(user);
                throw;
            }

            return await BuildAuthResponseAsync(user);
        }

        public async Task<AuthResponseDto> RegisterPatientAsync(PatientRegisterDto registerDto)
        {
            var normalizedUsername = registerDto.Username.Trim();

            if (await _userRepository.UsernameExistsAsync(normalizedUsername))
                throw new InvalidOperationException($"Username '{normalizedUsername}' is already taken.");

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Username = normalizedUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Role = AppRole.Patient
            };

            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                AppUserId = user.Id,
                FullName = registerDto.FullName.Trim(),
                DateOfBirth = registerDto.DateOfBirth,
                Gender = registerDto.Gender.Trim(),
                Phone = registerDto.Phone.Trim(),
                Address = registerDto.Address.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            try
            {
                await _patientRepository.AddAsync(patient);
                await EnsureHospitalPatientProjectionAsync(user, patient);
            }
            catch
            {
                await TryDeleteLegacyPatientAsync(patient);
                await _userRepository.DeleteAsync(user);
                throw;
            }

            return await BuildAuthResponseAsync(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var normalizedUsername = loginDto.Username.Trim();

            if (await _authSecurityMonitor.IsLoginBlockedAsync(normalizedUsername))
            {
                await RecordSecurityEventAsync(null, normalizedUsername, "LoginBlocked", "Warning", "Dang nhap bi chan tam thoi do qua nhieu lan that bai.");
                throw new InvalidOperationException("Too many failed login attempts. Please retry later.");
            }

            var user = await _userRepository.GetByUsernameAsync(normalizedUsername);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                await _authSecurityMonitor.RecordFailedLoginAttemptAsync(normalizedUsername);
                await RecordSecurityEventAsync(user?.Id, normalizedUsername, "LoginFailed", "Warning", "Thong tin dang nhap khong hop le.");
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            if (user.Role == AppRole.Patient)
            {
                var patient = await _patientRepository.GetByAppUserIdAsync(user.Id);
                if (patient == null)
                    throw new UnauthorizedAccessException("Patient profile not found.");

                await EnsureHospitalPatientProjectionAsync(user, patient);
            }
            else if (Array.Exists(AppRole.Internal, role => role == user.Role))
            {
                await _hospitalIdentityBridgeService.SyncInternalUserAsync(user);
            }

            await _authSecurityMonitor.ClearFailedLoginAttemptsAsync(normalizedUsername);
            await RecordSecurityEventAsync(user.Id, user.Username, "LoginSucceeded", "Info", "Dang nhap thanh cong.");
            return await BuildAuthResponseAsync(user);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            if (!TryGetUserId(principal, out var userId))
                throw new UnauthorizedAccessException("Invalid token subject.");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            try
            {
                ValidateRefreshToken(user, request.RefreshToken);
            }
            catch (UnauthorizedAccessException) when (ShouldRevokeActiveRefreshToken(user, request.RefreshToken))
            {
                user.RefreshTokenRevokedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
                await _authSecurityMonitor.RecordRefreshTokenMisuseAsync(user.Id, user.Username);
                await RevokeHospitalRefreshTokensAsync(user.Id, "Refresh token khong hop le, session bi revoke.");
                await RecordSecurityEventAsync(user.Id, user.Username, "RefreshTokenMisuse", "Critical", "Refresh token khong hop le, session hien tai da bi revoke.");
                throw new UnauthorizedAccessException("Refresh token is invalid. Active session has been revoked.");
            }

            await RecordSecurityEventAsync(user.Id, user.Username, "RefreshTokenRotated", "Info", "Lam moi access token va refresh token thanh cong.");
            return await BuildAuthResponseAsync(user);
        }

        public async Task LogoutAsync(RefreshTokenRequestDto request)
        {
            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            if (!TryGetUserId(principal, out var userId))
                return;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.RefreshTokenHash))
                return;

            if (ComputeSha256(request.RefreshToken) == user.RefreshTokenHash)
            {
                user.RefreshTokenRevokedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
                await RevokeHospitalRefreshTokensAsync(user.Id, "Nguoi dung dang xuat.");
                await RecordSecurityEventAsync(user.Id, user.Username, "Logout", "Info", "Nguoi dung dang xuat thanh cong.");
            }
        }

        public async Task LogoutAllAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return;
            }

            user.RefreshTokenRevokedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await RevokeHospitalRefreshTokensAsync(user.Id, "Nguoi dung dang xuat tat ca thiet bi.");
            await RecordSecurityEventAsync(user.Id, user.Username, "LogoutAll", "Info", "Nguoi dung dang xuat tren tat ca thiet bi.");
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var normalizedUsername = request.Username.Trim();
            var user = await _userRepository.GetByUsernameAsync(normalizedUsername);
            if (user == null)
            {
                return;
            }

            var resetToken = GeneratePasswordResetToken();
            var resetHash = ComputeSha256(resetToken);
            var ttlMinutes = ReadIntSetting("Security:PasswordReset:ExpiryMinutes", 15);

            await _distributedCache.SetStringAsync(
                BuildPasswordResetKey(normalizedUsername),
                resetHash,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ttlMinutes)
                });

            _logger.LogInformation(
                "Password reset token cho username {Username}: {ResetToken}",
                normalizedUsername,
                resetToken);

            await RecordSecurityEventAsync(user.Id, user.Username, "PasswordResetRequested", "Info", "Nguoi dung yeu cau reset password.");
        }

        public async Task ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var normalizedUsername = request.Username.Trim();
            var user = await _userRepository.GetByUsernameAsync(normalizedUsername);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Reset token is invalid.");
            }

            var cacheKey = BuildPasswordResetKey(normalizedUsername);
            var storedHash = await _distributedCache.GetStringAsync(cacheKey);
            var presentedHash = ComputeSha256(request.ResetToken.Trim());

            if (string.IsNullOrWhiteSpace(storedHash)
                || !string.Equals(storedHash, presentedHash, StringComparison.Ordinal))
            {
                await RecordSecurityEventAsync(user.Id, user.Username, "PasswordResetFailed", "Warning", "Reset token khong hop le hoac da het han.");
                throw new UnauthorizedAccessException("Reset token is invalid.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            user.RefreshTokenRevokedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _distributedCache.RemoveAsync(cacheKey);
            await SyncHospitalPasswordAsync(user);
            await RevokeHospitalRefreshTokensAsync(user.Id, "Password da duoc reset.");
            await RecordSecurityEventAsync(user.Id, user.Username, "PasswordResetSucceeded", "Info", "Password da duoc reset thanh cong.");
        }

        private async Task<AuthResponseDto> BuildAuthResponseAsync(AppUser user)
        {
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
            var accessToken = GenerateJwtToken(user, expiresAt);
            var refreshToken = GenerateRefreshToken();
            var refreshTtlDays = int.Parse(_configuration["Jwt:RefreshExpiryDays"] ?? "14");

            // Rotate refresh token on every successful auth/refresh response.
            user.RefreshTokenHash = ComputeSha256(refreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTtlDays);
            user.RefreshTokenRevokedAt = null;
            await _userRepository.UpdateAsync(user);
            await SyncHospitalAuthStateAsync(user, refreshToken);

            return new AuthResponseDto
            {
                Token = accessToken,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Username = user.Username,
                Role = user.Role,
                ExpiresAt = expiresAt
            };
        }

        private void ValidateRefreshToken(AppUser user, string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)
                || string.IsNullOrWhiteSpace(user.RefreshTokenHash)
                || user.RefreshTokenExpiresAt == null)
            {
                throw new UnauthorizedAccessException("Refresh token is missing.");
            }

            if (user.RefreshTokenRevokedAt != null)
                throw new UnauthorizedAccessException("Refresh token has been revoked.");

            if (user.RefreshTokenExpiresAt <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token has expired.");

            var refreshHash = ComputeSha256(refreshToken);
            if (!string.Equals(refreshHash, user.RefreshTokenHash, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Refresh token is invalid.");
        }

        private string GenerateJwtToken(AppUser user, DateTime expiresAt)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            foreach (var permission in AppPermissions.GetPermissionsForRole(user.Role))
            {
                claims.Add(new Claim(AppPermissions.ClaimType, permission));
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken
                || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token.");
            }

            return principal;
        }

        private static string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        private static string ComputeSha256(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private static bool ShouldRevokeActiveRefreshToken(AppUser user, string presentedRefreshToken)
        {
            if (string.IsNullOrWhiteSpace(presentedRefreshToken)
                || string.IsNullOrWhiteSpace(user.RefreshTokenHash)
                || user.RefreshTokenRevokedAt != null
                || user.RefreshTokenExpiresAt == null
                || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                return false;
            }

            var presentedHash = ComputeSha256(presentedRefreshToken);
            return !string.Equals(presentedHash, user.RefreshTokenHash, StringComparison.Ordinal);
        }

        private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
        {
            var rawUserId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(rawUserId, out userId);
        }

        private async Task EnsureHospitalPatientProjectionAsync(AppUser user, Patient legacyPatient)
        {
            var nowUtc = DateTime.UtcNow;
            var normalizedPhone = string.IsNullOrWhiteSpace(legacyPatient.Phone) ? null : legacyPatient.Phone.Trim();

            var hospitalUser = await _hospitalDbContext.Users
                .FirstOrDefaultAsync(x => x.Id == user.Id);

            if (hospitalUser == null)
            {
                hospitalUser = new HospitalUserEntity
                {
                    Id = user.Id,
                    Username = user.Username.Trim(),
                    PasswordHash = user.PasswordHash,
                    PrimaryRoleCode = AppRole.Patient,
                    IsActive = true,
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc
                };

                _hospitalDbContext.Users.Add(hospitalUser);
            }
            else
            {
                hospitalUser.Username = user.Username.Trim();
                hospitalUser.PasswordHash = user.PasswordHash;
                hospitalUser.PrimaryRoleCode = AppRole.Patient;
                hospitalUser.IsActive = true;
                hospitalUser.UpdatedAtUtc = nowUtc;
            }

            var hasUserRole = await _hospitalDbContext.UserRoles.AnyAsync(
                x => x.UserId == user.Id && x.RoleCode == AppRole.Patient);

            if (!hasUserRole)
            {
                _hospitalDbContext.UserRoles.Add(new HospitalUserRoleEntity
                {
                    UserId = user.Id,
                    RoleCode = AppRole.Patient,
                    GrantedByUserId = null
                });
            }

            var patientAccount = await _hospitalDbContext.PatientAccounts
                .Include(x => x.Patient)
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            HospitalPatientEntity hospitalPatient;
            if (patientAccount != null)
            {
                hospitalPatient = patientAccount.Patient;
            }
            else
            {
                hospitalPatient = await _hospitalDbContext.Patients
                    .Where(x => x.DeletedAtUtc == null)
                    .Where(x => x.FullName == legacyPatient.FullName.Trim())
                    .Where(x => x.DateOfBirth == DateOnly.FromDateTime(legacyPatient.DateOfBirth))
                    .Where(x => normalizedPhone != null && x.Phone == normalizedPhone)
                    .Where(x => !_hospitalDbContext.PatientAccounts.Any(pa => pa.PatientId == x.Id))
                    .OrderByDescending(x => x.UpdatedAtUtc)
                    .FirstOrDefaultAsync()
                    ?? new HospitalPatientEntity
                    {
                        Id = Guid.NewGuid(),
                        MedicalRecordNumber = GenerateMedicalRecordNumber(nowUtc),
                        CreatedAtUtc = nowUtc
                    };

                if (_hospitalDbContext.Entry(hospitalPatient).State == EntityState.Detached)
                {
                    _hospitalDbContext.Patients.Add(hospitalPatient);
                }
            }

            hospitalPatient.FullName = legacyPatient.FullName.Trim();
            hospitalPatient.DateOfBirth = DateOnly.FromDateTime(legacyPatient.DateOfBirth);
            hospitalPatient.Gender = legacyPatient.Gender.Trim();
            hospitalPatient.Phone = normalizedPhone;
            hospitalPatient.AddressLine1 = string.IsNullOrWhiteSpace(legacyPatient.Address) ? null : legacyPatient.Address.Trim();
            hospitalPatient.Nationality ??= "Viet Nam";
            hospitalPatient.UpdatedAtUtc = nowUtc;

            if (patientAccount == null)
            {
                _hospitalDbContext.PatientAccounts.Add(new HospitalPatientAccountEntity
                {
                    PatientId = hospitalPatient.Id,
                    UserId = user.Id,
                    ActivatedAtUtc = nowUtc,
                    PortalStatus = "Active"
                });
            }
            else
            {
                patientAccount.PortalStatus = "Active";
            }

            await _hospitalDbContext.SaveChangesAsync();
        }

        private async Task SyncHospitalAuthStateAsync(AppUser user, string refreshToken)
        {
            var nowUtc = DateTime.UtcNow;
            var hospitalUser = await _hospitalDbContext.Users
                .FirstOrDefaultAsync(x => x.Id == user.Id);

            if (hospitalUser == null)
            {
                return;
            }

            hospitalUser.LastLoginAtUtc = nowUtc;
            hospitalUser.UpdatedAtUtc = nowUtc;

            var activeTokens = await _hospitalDbContext.RefreshTokens
                .Where(x => x.UserId == user.Id && x.RevokedAtUtc == null)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RotatedAtUtc = nowUtc;
                token.RevokedAtUtc = nowUtc;
            }

            _hospitalDbContext.RefreshTokens.Add(new HospitalRefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = ComputeSha256(refreshToken),
                ExpiresAtUtc = user.RefreshTokenExpiresAt ?? nowUtc.AddDays(14),
                CreatedAtUtc = nowUtc
            });

            await _hospitalDbContext.SaveChangesAsync();
        }

        private async Task SyncHospitalPasswordAsync(AppUser user)
        {
            var hospitalUser = await _hospitalDbContext.Users
                .FirstOrDefaultAsync(x => x.Id == user.Id);

            if (hospitalUser == null)
            {
                return;
            }

            hospitalUser.PasswordHash = user.PasswordHash;
            hospitalUser.UpdatedAtUtc = DateTime.UtcNow;
            await _hospitalDbContext.SaveChangesAsync();
        }

        private async Task RevokeHospitalRefreshTokensAsync(Guid userId, string reason)
        {
            var nowUtc = DateTime.UtcNow;
            var activeTokens = await _hospitalDbContext.RefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
                .ToListAsync();

            if (activeTokens.Count == 0)
            {
                return;
            }

            foreach (var token in activeTokens)
            {
                token.RevokedAtUtc = nowUtc;
                token.RotatedAtUtc ??= nowUtc;
            }

            await _hospitalDbContext.SaveChangesAsync();
        }

        private async Task RecordSecurityEventAsync(
            Guid? userId,
            string username,
            string eventType,
            string severity,
            string detail)
        {
            try
            {
                _hospitalDbContext.SecurityEvents.Add(new HospitalSecurityEventEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EventType = eventType,
                    Severity = severity,
                    Detail = SensitiveDataMasking.MaskAuditDetail($"Username={username}; {detail}"),
                    IpAddress = null,
                    UserAgent = null,
                    OccurredAtUtc = DateTime.UtcNow
                });

                await _hospitalDbContext.SaveChangesAsync();
            }
            catch
            {
                // Auth flow should not fail only because audit logging is unavailable.
            }
        }

        private async Task TryDeleteLegacyPatientAsync(Patient patient)
        {
            try
            {
                await _patientRepository.DeleteAsync(patient);
            }
            catch
            {
            }
        }

        private static string GenerateMedicalRecordNumber(DateTime nowUtc)
            => $"MRN-{nowUtc:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

        private static string GeneratePasswordResetToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToHexString(bytes);
        }

        private static string BuildPasswordResetKey(string username)
            => $"auth:password-reset:{username.Trim().ToLowerInvariant()}";

        private int ReadIntSetting(string key, int fallbackValue)
        {
            var raw = _configuration[key];
            return int.TryParse(raw, out var parsed) ? parsed : fallbackValue;
        }
    }
}
