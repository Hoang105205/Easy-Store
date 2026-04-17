using Api.GraphQL.Mutations;
using Core.Data;
using Core.Models;
using HotChocolate;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests.GraphQL.Mutations;

public class ImportMutationTest
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task CompleteImportAsync_NenCongKho_VaCapNhatMAC_KhiChotPhieu()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new ImportMutation();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            SKU = "SKU-1",
            Name = "Pepsi",
            StockQuantity = 10,
            AvailableStockQuantity = 10,
            ImportPrice = 1000,
            Category = null!
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var input = new CompleteImportInput(
            Details: new List<ImportLogDetailInput>
            {
                new(product.Id, 5, 2000)
            },
            isDraft: false,
            isAutoSave: false
        );

        // Act
        var result = await mutation.CompleteImportAsync(input, context);

        // Assert
        Assert.Equal(ImportStatus.Completed, result.Status);
        Assert.False(result.IsAutoSaved);
        Assert.Equal(10000, result.TotalAmount);

        var productInDb = await context.Products.FindAsync(product.Id);
        Assert.NotNull(productInDb);
        Assert.Equal(15, productInDb.StockQuantity);
        Assert.Equal(15, productInDb.AvailableStockQuantity);
        Assert.Equal(1333, productInDb.ImportPrice);
    }

    [Fact]
    public async Task CompleteImportAsync_NenXoaAutoSaveCu_VaKhongCongKho_KhiDangAutoSave()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new ImportMutation();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            SKU = "SKU-2",
            Name = "Mirinda",
            StockQuantity = 20,
            AvailableStockQuantity = 20,
            ImportPrice = 1200,
            Category = null!
        };

        var oldAutoSave = new ImportLog
        {
            Id = Guid.NewGuid(),
            IsAutoSaved = true,
            Status = ImportStatus.Draft,
            Details = new List<ImportLogDetail>()
        };

        context.Products.Add(product);
        context.ImportLogs.Add(oldAutoSave);
        await context.SaveChangesAsync();

        var input = new CompleteImportInput(
            Details: new List<ImportLogDetailInput>
            {
                new(product.Id, 3, 1500)
            },
            isDraft: false,
            isAutoSave: true
        );

        // Act
        var result = await mutation.CompleteImportAsync(input, context);

        // Assert
        Assert.Equal(ImportStatus.Draft, result.Status);
        Assert.True(result.IsAutoSaved);

        var productInDb = await context.Products.FindAsync(product.Id);
        Assert.NotNull(productInDb);
        Assert.Equal(20, productInDb.StockQuantity);
        Assert.Equal(20, productInDb.AvailableStockQuantity);
        Assert.Equal(1200, productInDb.ImportPrice);

        var allImportLogs = await context.ImportLogs.ToListAsync();
        Assert.Single(allImportLogs);
        Assert.Equal(result.Id, allImportLogs[0].Id);
    }

    [Fact]
    public async Task CompleteImportAsync_NenGomNhomChiTietTrungProductId()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new ImportMutation();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            SKU = "SKU-3",
            Name = "7Up",
            StockQuantity = 0,
            AvailableStockQuantity = 0,
            ImportPrice = 0,
            Category = null!
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var input = new CompleteImportInput(
            Details: new List<ImportLogDetailInput>
            {
                new(product.Id, 2, 500),
                new(product.Id, 3, 500)
            },
            isDraft: false,
            isAutoSave: false
        );

        // Act
        var result = await mutation.CompleteImportAsync(input, context);

        // Assert
        Assert.Single(result.Details);
        Assert.Equal(5, result.Details.First().QuantityAdded);
        Assert.Equal(2500, result.TotalAmount);

        var productInDb = await context.Products.FindAsync(product.Id);
        Assert.NotNull(productInDb);
        Assert.Equal(5, productInDb.StockQuantity);
    }

    [Fact]
    public async Task DeleteImportAsync_NenBaoLoi_KhiXoaPhieuDaHoanThanh()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new ImportMutation();
        var importLog = new ImportLog
        {
            Id = Guid.NewGuid(),
            Status = ImportStatus.Completed
        };

        context.ImportLogs.Add(importLog);
        await context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<GraphQLException>(() => mutation.DeleteImportAsync(importLog.Id, context));
        Assert.Equal("Không thể xóa phiếu đã hoàn thành vì đã cộng vào tồn kho!", exception.Message);
    }

    [Fact]
    public async Task MarkImportCompletedAsync_NenChotPhieu_VaCongKhoSanPham()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var mutation = new ImportMutation();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            SKU = "SKU-4",
            Name = "Sting",
            StockQuantity = 10,
            AvailableStockQuantity = 10,
            ImportPrice = 1000,
            Category = null!
        };

        var importLog = new ImportLog
        {
            Id = Guid.NewGuid(),
            Status = ImportStatus.Draft,
            IsAutoSaved = true,
            Details = new List<ImportLogDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    QuantityAdded = 10,
                    ActualImportPrice = 2000
                }
            }
        };

        context.Products.Add(product);
        context.ImportLogs.Add(importLog);
        await context.SaveChangesAsync();

        // Act
        var result = await mutation.MarkImportCompletedAsync(importLog.Id, context);

        // Assert
        Assert.Equal(ImportStatus.Completed, result.Status);
        Assert.False(result.IsAutoSaved);

        var productInDb = await context.Products.FindAsync(product.Id);
        Assert.NotNull(productInDb);
        Assert.Equal(20, productInDb.StockQuantity);
        Assert.Equal(20, productInDb.AvailableStockQuantity);
        Assert.Equal(1500, productInDb.ImportPrice);
    }

}
