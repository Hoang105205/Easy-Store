using Api.GraphQL.Mutations;
using Api.Utils;
using Core.Models;
using Xunit;

namespace Api.Tests.Utils;

public class OrderHelperTests
{
    [Fact]
    public void CalculateTotalAmount_NenTraVeTongTienChinhXac()
    {
        // 1. Arrange (Chuẩn bị dữ liệu)
        var items = new List<DraftOrderItemInput>
        {
            new DraftOrderItemInput { ProductId = Guid.NewGuid(), Quantity = 2, UnitSalePrice = 15000 },
            new DraftOrderItemInput { ProductId = Guid.NewGuid(), Quantity = 3, UnitSalePrice = 20000 }
        };
        // Kỳ vọng: (2 * 15000) + (3 * 20000) = 90000

        // 2. Act (Hành động gọi hàm cần test)
        long result = OrderHelper.CalculateTotalAmount(items);

        // 3. Assert (Kiểm chứng)
        Assert.Equal(90000, result);
    }

    [Fact]
    public void CalculateTotalProfit_NenTinhDungLoiNhuan()
    {
        // Arrange
        var product1 = new Product { Id = Guid.NewGuid(), ImportPrice = 10000 }; // Lời 5000/sp
        var product2 = new Product { Id = Guid.NewGuid(), ImportPrice = 15000 }; // Lời 5000/sp

        var orderItems = new List<OrderItem>
        {
            new OrderItem { Product = product1, Quantity = 2, UnitSalePrice = 15000 },
            new OrderItem { Product = product2, Quantity = 1, UnitSalePrice = 20000 }
        };
        // Tổng lời: (5000 * 2) + (5000 * 1) = 15000

        // Act
        long result = OrderHelper.CalculateTotalProfit(orderItems);

        // Assert
        Assert.Equal(15000, result);
    }
}