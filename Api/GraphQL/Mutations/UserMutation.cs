using Core.Data;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Api.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class UserMutation
{
    public async Task<bool> ChangePassword(
        string currentPassword,
        string newPassword,
        [Service] AppDbContext context)
    {
        var admin = await context.Users.FirstOrDefaultAsync();

        if (admin == null)
            throw new GraphQLException("Hệ thống chưa khởi tạo người dùng Admin.");

        if (string.IsNullOrEmpty(admin.PasswordHash))
            throw new GraphQLException("Tài khoản chưa có mật khẩu khởi tạo.");

        // 2. Kiểm tra mật khẩu hiện tại (Dùng BCrypt)
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, admin.PasswordHash);

        if (!isPasswordValid)
        {
            throw new GraphQLException("Mật khẩu hiện tại không chính xác.");
        }

        // 3. Băm mật khẩu mới và lưu lại
        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        await context.SaveChangesAsync();

        return true;
    }
}

