using Api.GraphQL.Mutations;
using Core.Data;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests.GraphQL.Mutations;

public class AuthMutationTest
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Login_NenThanhCong_KhiThongTinDangNhapHopLe()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new AuthMutation();
        var plainPassword = "123456";

        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword)
        });
        await context.SaveChangesAsync();

        // Act
        var result = await mutation.Login("admin", plainPassword, context);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Đăng nhập thành công!", result.Message);
    }

    [Fact]
    public async Task Login_NenThatBai_KhiSaiMatKhau()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new AuthMutation();

        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
        });
        await context.SaveChangesAsync();

        // Act
        var result = await mutation.Login("admin", "wrong-password", context);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Đăng nhập thất bại", result.Message);
    }

    [Fact]
    public async Task Login_NenThatBai_KhiKhongTonTaiNguoiDung()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new AuthMutation();

        // Act
        var result = await mutation.Login("not-found-user", "123456", context);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Đăng nhập thất bại", result.Message);
    }

}
