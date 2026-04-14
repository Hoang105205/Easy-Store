using Api.GraphQL.Mutations;
using Core.Data;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.GraphQL.Mutations;

public class OrderMutationTests
{
    // Hàm tiện ích tạo DB giả lập rỗng cho mỗi test case
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Mỗi test một DB riêng biệt
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task UpsertDraftOrderAsync_NenBaoLoi_KhiKhongDuSoLuongTonKho()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new OrderMutation();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Coca Cola",
            AvailableStockQuantity = 5 // Chỉ còn 5 lon
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var input = new UpsertDraftOrderInput
        {
            Items = new List<DraftOrderItemInput>
            {
                new DraftOrderItemInput { ProductId = product.Id, Quantity = 10, UnitSalePrice = 10000 } // Đòi mua 10 lon
            }
        };

        // Act & Assert
        // Kỳ vọng hàm này sẽ quăng ra một Exception với nội dung tương ứng
        var exception = await Assert.ThrowsAsync<Exception>(() => mutation.UpsertDraftOrderAsync(input, context));
        Assert.Contains("chỉ còn 5 sản phẩm khả dụng", exception.Message);
    }

    [Fact]
    public async Task UpsertDraftOrderAsync_NenTruAvailableStock_KhiTaoDonMoiThanhCong()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new OrderMutation();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Chocopie",
            AvailableStockQuantity = 20
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var input = new UpsertDraftOrderInput
        {
            Items = new List<DraftOrderItemInput>
            {
                new DraftOrderItemInput { ProductId = product.Id, Quantity = 5, UnitSalePrice = 50000 }
            }
        };

        // Act
        var resultOrder = await mutation.UpsertDraftOrderAsync(input, context);

        // Assert
        Assert.NotNull(resultOrder);
        Assert.True(resultOrder.IsDraft);

        // Kiểm tra db xem kho mềm (AvailableStockQuantity) đã bị trừ chưa
        var productInDb = await context.Products.FindAsync(product.Id);
        Assert.Equal(15, productInDb.AvailableStockQuantity); // 20 - 5 = 15
    }

    [Fact]
    public async Task FinalizeOrderAsync_NenTruStockQuantityThucTe()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new OrderMutation();

        var product = new Product { Id = Guid.NewGuid(), Name = "Heineken", StockQuantity = 50, ImportPrice = 15000 };
        var order = new Order { Id = Guid.NewGuid(), IsDraft = true };
        var orderItem = new OrderItem { Id = Guid.NewGuid(), OrderId = order.Id, ProductId = product.Id, Quantity = 10, UnitSalePrice = 20000 };

        context.Products.Add(product);
        context.Orders.Add(order);
        context.OrderItems.Add(orderItem);
        await context.SaveChangesAsync();

        // Act
        var result = await mutation.FinalizeOrderAsync(order.Id, context);

        // Assert
        Assert.False(result.IsDraft); // Đã chốt hóa đơn
        Assert.Equal(50000, result.TotalProfit); // Lời: (20000 - 15000) * 10 = 50000

        var productInDb = await context.Products.FindAsync(product.Id);
        Assert.Equal(40, productInDb.StockQuantity); // 50 - 10 = 40 (kho thực tế bị trừ)
    }
}