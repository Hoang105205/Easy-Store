using Core.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Api.GraphQL.Mutations;
public record LoginResultDTO(bool Success, string Message);

[ExtendObjectType(typeof(Mutation))]
public class AuthMutation
{
    public async Task<LoginResultDTO> Login(
        string username,
        string password,
        [Service] AppDbContext context)
    {

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user != null)
        {
            // Log ra để kiểm tra trong cửa sổ Output của Visual Studio
            Console.WriteLine($"[DEBUG] DB Hash: {user.PasswordHash}");
            Console.WriteLine($"[DEBUG] Input Pass: {password}");

            bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            Console.WriteLine($"[DEBUG] Is Valid: {isValid}");

            if (isValid) return new LoginResultDTO(true, "Đăng nhập thành công!");
        }

        return new LoginResultDTO(false, "Đăng nhập thất bại");
    }
}

