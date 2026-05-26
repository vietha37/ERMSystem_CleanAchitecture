using System.Threading.Tasks;
using ERMSystem.Application.DTOs;

namespace ERMSystem.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> RegisterPatientAsync(PatientRegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> VerifyMfaLoginAsync(VerifyMfaLoginDto request);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task LogoutAsync(RefreshTokenRequestDto request);
        Task LogoutAllAsync(Guid userId);
        Task ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task ResetPasswordAsync(ResetPasswordRequestDto request);
        Task<MfaStatusDto> GetMfaStatusAsync(Guid userId);
        Task<MfaSetupResponseDto> SetupMfaAsync(Guid userId);
        Task<MfaStatusDto> EnableMfaAsync(Guid userId, VerifyMfaCodeDto request);
        Task<MfaStatusDto> DisableMfaAsync(Guid userId, VerifyMfaCodeDto request);
    }
}
