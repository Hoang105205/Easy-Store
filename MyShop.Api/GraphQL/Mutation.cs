using Core.Data;
using Core.Models;
using HotChocolate;

namespace MyShop.Api.GraphQL;

public class Mutation
{
    public string Login(string username, string password, [Service] AppDbContext context)
    {
        // LƯU Ý: Đây chỉ là code test luồng. Thực tế mật khẩu phải được mã hóa (Hash) chứ không so sánh chuỗi thô thế này nhé!
        var user = context.Users.FirstOrDefault(u => u.Username == username && u.PasswordHash == password);

        if (user == null)
        {
            return "Login failed! Wrong username or password.";
        }

        return $"Login successfully! Welcome back, {user.Username}.";
    }
}