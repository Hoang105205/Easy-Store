using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UI.Services.ProfileService;

public class UserService
{
    private readonly IEasyStoreClient _client;

    public UserService(IEasyStoreClient client)
    {
        _client = client;
    }

    public async Task<(bool IsSuccess, string Message)> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        try
        {
            // Gọi Mutation đã sinh ra từ file .graphql
            var result = await _client.ChangePassword.ExecuteAsync(currentPassword, newPassword);

            // Kiểm tra lỗi từ hệ thống (Lỗi mạng, Server chết...)
            if (result.Errors.Count > 0)
            {
                return (false, result.Errors[0].Message);
            }

            // Kiểm tra kết quả nghiệp vụ từ Backend trả về
            if (result.Data != null && result.Data.ChangePassword)
            {
                return (true, "Mật khẩu đã được cập nhật thành công!");
            }

            return (false, "Không thể cập nhật mật khẩu. Vui lòng thử lại.");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi hệ thống: {ex.Message}");
        }
    }
}
