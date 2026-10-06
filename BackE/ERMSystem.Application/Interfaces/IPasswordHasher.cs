namespace ERMSystem.Application.Interfaces;

/// <summary>
/// Cổng trừu tượng (Port) cho việc băm và kiểm tra tính hợp lệ của mật khẩu người dùng.
/// Tuân thủ nguyên tắc Dependency Inversion: Application định nghĩa hợp đồng, Infrastructure triển khai.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Tạo chuỗi băm bảo mật từ mật khẩu thô.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Xác thực mật khẩu thô với chuỗi băm đã lưu trữ.
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}
