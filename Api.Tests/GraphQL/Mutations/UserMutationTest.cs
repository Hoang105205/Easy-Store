using Api.GraphQL.Mutations;
using Core.Data;
using Core.Models;
using HotChocolate;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests.GraphQL.Mutations;

public class UserMutationTest
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task ChangePassword_NenBaoLoi_KhiChuaCoAdmin()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new UserMutation();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<GraphQLException>(() => mutation.ChangePassword("old", "new", context));
        Assert.Equal("Hệ thống chưa khởi tạo người dùng Admin.", exception.Message);
    }

    [Fact]
    public async Task ChangePassword_NenBaoLoi_KhiAdminChuaCoMatKhauKhoiTao()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new UserMutation();

        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            PasswordHash = string.Empty
        });
        await context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<GraphQLException>(() => mutation.ChangePassword("old", "new", context));
        Assert.Equal("Tài khoản chưa có mật khẩu khởi tạo.", exception.Message);
    }

    [Fact]
    public async Task ChangePassword_NenBaoLoi_KhiMatKhauHienTaiKhongChinhXac()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new UserMutation();

        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456")
        });
        await context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<GraphQLException>(() => mutation.ChangePassword("wrong-current", "new-123", context));
        Assert.Equal("Mật khẩu hiện tại không chính xác.", exception.Message);
    }

    [Fact]
    public async Task ChangePassword_NenThanhCong_KhiNhapDungMatKhauHienTai()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new UserMutation();

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("old-password")
        };
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        // Act
        var result = await mutation.ChangePassword("old-password", "new-password", context);

        // Assert
        Assert.True(result);

        var adminInDb = await context.Users.FirstAsync();
        Assert.True(BCrypt.Net.BCrypt.Verify("new-password", adminInDb.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("old-password", adminInDb.PasswordHash));
    }

}
