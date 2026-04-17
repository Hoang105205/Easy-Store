using Api.GraphQL.Queries;
using Core.Data;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests.GraphQL.Queries;

public class StatisticsQueryTest
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetStatistics_NenChiTinhDonPaidVaKhongPhaiDraft()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var query = new StatisticsQuery();

        var p1 = new Product { Id = Guid.NewGuid(), CategoryId = Guid.NewGuid(), SKU = "SKU-1", Name = "Product 1", StockQuantity = 5, MinimumStockQuantity = 10, Category = null! };
        var p2 = new Product { Id = Guid.NewGuid(), CategoryId = Guid.NewGuid(), SKU = "SKU-2", Name = "Product 2", StockQuantity = 20, MinimumStockQuantity = 10, Category = null! };

        var validOrder = new Order
        {
            Id = Guid.NewGuid(),
            IsDraft = false,
            Status = Order.Statuses.Paid,
            OrderDate = new DateTime(2026, 4, 10),
            TotalAmount = 100000,
            TotalProfit = 30000,
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = p1.Id,
                    Quantity = 2,
                    UnitSalePrice = 50000,
                    UnitImportPrice = 35000,
                    TotalPrice = 100000
                }
            }
        };

        var draftOrder = new Order
        {
            Id = Guid.NewGuid(),
            IsDraft = true,
            Status = Order.Statuses.Paid,
            OrderDate = new DateTime(2026, 4, 10),
            TotalAmount = 999999,
            TotalProfit = 999999
        };

        var unpaidOrder = new Order
        {
            Id = Guid.NewGuid(),
            IsDraft = false,
            Status = Order.Statuses.Created,
            OrderDate = new DateTime(2026, 4, 10),
            TotalAmount = 999999,
            TotalProfit = 999999
        };

        context.Products.AddRange(p1, p2);
        context.Orders.AddRange(validOrder, draftOrder, unpaidOrder);
        await context.SaveChangesAsync();

        // Act
        var result = await query.GetStatistics(
            context,
            new DateTime(2026, 4, 1),
            new DateTime(2026, 4, 30));

        // Assert
        Assert.Equal(100000, result.Summary.TotalRevenue);
        Assert.Equal(30000, result.Summary.TotalProfit);
        Assert.Equal(1, result.Summary.TotalOrders);
        Assert.Equal(1, result.Summary.LowStockProducts);

        Assert.Single(result.TopProducts);
        Assert.Equal("Product 1", result.TopProducts[0].ProductName);
        Assert.Equal("SKU-1", result.TopProducts[0].Sku);
        Assert.Equal(2, result.TopProducts[0].QuantitySold);
        Assert.Equal(100000, result.TopProducts[0].Revenue);
        Assert.Equal(30000, result.TopProducts[0].Profit);
    }

    [Fact]
    public async Task GetStatistics_NenFillDuNgayTrenChartData_KhiLocTheoNgay()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var query = new StatisticsQuery();

        var product = new Product { Id = Guid.NewGuid(), CategoryId = Guid.NewGuid(), SKU = "SKU-D", Name = "Daily", Category = null! };
        var order = new Order
        {
            Id = Guid.NewGuid(),
            IsDraft = false,
            Status = Order.Statuses.Paid,
            OrderDate = new DateTime(2026, 4, 2, 10, 0, 0),
            TotalAmount = 1000,
            TotalProfit = 300,
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Quantity = 1,
                    UnitSalePrice = 1000,
                    UnitImportPrice = 700,
                    TotalPrice = 1000
                }
            }
        };

        context.Products.Add(product);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act
        var result = await query.GetStatistics(
            context,
            new DateTime(2026, 4, 1),
            new DateTime(2026, 4, 3));

        // Assert
        Assert.Equal(3, result.ChartData.Count);
        Assert.Equal("01/04", result.ChartData[0].Label);
        Assert.Equal("02/04", result.ChartData[1].Label);
        Assert.Equal("03/04", result.ChartData[2].Label);

        Assert.Equal(0, result.ChartData[0].Revenue);
        Assert.Equal(1000, result.ChartData[1].Revenue);
        Assert.Equal(0, result.ChartData[2].Revenue);
    }

    [Fact]
    public async Task GetStatistics_NenNhomTheoThang_KhiRangeLonHon31Ngay()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var query = new StatisticsQuery();

        var product = new Product { Id = Guid.NewGuid(), CategoryId = Guid.NewGuid(), SKU = "SKU-M", Name = "Monthly", Category = null! };
        var order = new Order
        {
            Id = Guid.NewGuid(),
            IsDraft = false,
            Status = Order.Statuses.Paid,
            OrderDate = new DateTime(2026, 2, 15),
            TotalAmount = 2000,
            TotalProfit = 600,
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Quantity = 1,
                    UnitSalePrice = 2000,
                    UnitImportPrice = 1400,
                    TotalPrice = 2000
                }
            }
        };

        context.Products.Add(product);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Act
        var result = await query.GetStatistics(
            context,
            new DateTime(2026, 1, 1),
            new DateTime(2026, 3, 31));

        // Assert
        Assert.Equal(3, result.ChartData.Count);
        Assert.Equal("01/2026", result.ChartData[0].Label);
        Assert.Equal("02/2026", result.ChartData[1].Label);
        Assert.Equal("03/2026", result.ChartData[2].Label);

        Assert.Equal(0, result.ChartData[0].Revenue);
        Assert.Equal(2000, result.ChartData[1].Revenue);
        Assert.Equal(0, result.ChartData[2].Revenue);
    }

    [Fact]
    public async Task GetStatistics_NenNhomTheoNam_KhiRangeLonHon1Nam()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var query = new StatisticsQuery();

        var product = new Product { Id = Guid.NewGuid(), CategoryId = Guid.NewGuid(), SKU = "SKU-Y", Name = "Yearly", Category = null! };

        context.Products.Add(product);
        context.Orders.AddRange(
            new Order
            {
                Id = Guid.NewGuid(),
                IsDraft = false,
                Status = Order.Statuses.Paid,
                OrderDate = new DateTime(2025, 6, 1),
                TotalAmount = 3000,
                TotalProfit = 1000,
                OrderItems = new List<OrderItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        Quantity = 1,
                        UnitSalePrice = 3000,
                        UnitImportPrice = 2000,
                        TotalPrice = 3000
                    }
                }
            },
            new Order
            {
                Id = Guid.NewGuid(),
                IsDraft = false,
                Status = Order.Statuses.Paid,
                OrderDate = new DateTime(2027, 1, 1),
                TotalAmount = 4000,
                TotalProfit = 1200,
                OrderItems = new List<OrderItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        Quantity = 1,
                        UnitSalePrice = 4000,
                        UnitImportPrice = 2800,
                        TotalPrice = 4000
                    }
                }
            });
        await context.SaveChangesAsync();

        // Act
        var result = await query.GetStatistics(
            context,
            new DateTime(2025, 1, 1),
            new DateTime(2027, 12, 31));

        // Assert
        Assert.Equal(3, result.ChartData.Count);
        Assert.Equal("2025", result.ChartData[0].Label);
        Assert.Equal("2026", result.ChartData[1].Label);
        Assert.Equal("2027", result.ChartData[2].Label);

        Assert.Equal(3000, result.ChartData[0].Revenue);
        Assert.Equal(0, result.ChartData[1].Revenue);
        Assert.Equal(4000, result.ChartData[2].Revenue);
    }
}
