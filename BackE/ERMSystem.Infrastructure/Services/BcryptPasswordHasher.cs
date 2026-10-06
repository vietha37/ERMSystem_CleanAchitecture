using ERMSystem.Application.Interfaces;

namespace ERMSystem.Infrastructure.Services;

/// <summary>
/// Triển khai dịch vụ băm mật khẩu bảo mật sử dụng thuật toán BCrypt.
/// Nằm tại Infrastructure Layer theo đúng nguyên tắc Clean Architecture.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
