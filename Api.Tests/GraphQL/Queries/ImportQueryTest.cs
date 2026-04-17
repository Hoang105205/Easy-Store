using Api.GraphQL.Queries;
using Core.Data;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests.GraphQL.Queries;

public class ImportQueryTest
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetActiveAutoSaveAsync_NenTraVePhieuAutoSave()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var queries = new ImportQueries();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            SKU = "SKU-AUTO",
            Name = "Auto Product",
            Category = null!
        };

        var autoSaveLog = new ImportLog
        {
            Id = Guid.NewGuid(),
            IsAutoSaved = true,
            Status = ImportStatus.Draft,
            Details = new List<ImportLogDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    QuantityAdded = 2,
                    ActualImportPrice = 1000
                }
            }
        };

        context.Products.Add(product);
        context.ImportLogs.Add(autoSaveLog);
        await context.SaveChangesAsync();

        // Act
        var result = await queries.GetActiveAutoSaveAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.IsAutoSaved);
        Assert.Single(result.Details);
    }

    [Fact]
    public async Task GetImportHistory_NenLocDungTheoKeywordNgayVaStatus_VaBoQuaAutoSave()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var queries = new ImportQueries();

        var productA = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            SKU = "PEPSI-001",
            Name = "Pepsi",
            Category = null!
        };

        var productB = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            SKU = "STING-001",
            Name = "Sting",
            Category = null!
        };

        var import1 = new ImportLog
        {
            Id = Guid.NewGuid(),
            CreatedAt = new DateTime(2026, 4, 10, 10, 0, 0),
            Status = ImportStatus.Completed,
            IsAutoSaved = false,
            Details = new List<ImportLogDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = productA.Id,
                    QuantityAdded = 5,
                    ActualImportPrice = 1000
                }
            }
        };

        var import2 = new ImportLog
        {
            Id = Guid.NewGuid(),
            CreatedAt = new DateTime(2026, 4, 15, 15, 30, 0),
            Status = ImportStatus.Draft,
            IsAutoSaved = false,
            Details = new List<ImportLogDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = productB.Id,
                    QuantityAdded = 3,
                    ActualImportPrice = 2000
                }
            }
        };

        var autoSave = new ImportLog
        {
            Id = Guid.NewGuid(),
            CreatedAt = new DateTime(2026, 4, 16, 8, 0, 0),
            Status = ImportStatus.Draft,
            IsAutoSaved = true,
            Details = new List<ImportLogDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = productA.Id,
                    QuantityAdded = 1,
                    ActualImportPrice = 1500
                }
            }
        };

        context.Products.AddRange(productA, productB);
        context.ImportLogs.AddRange(import1, import2, autoSave);
        await context.SaveChangesAsync();

        // Act
        var allResult = queries.GetImportHistory(context, null, null, null, null).ToList();
        var keywordResult = queries.GetImportHistory(context, "pepsi", null, null, null).ToList();
        var filterResult = queries.GetImportHistory(
            context,
            null,
            new DateTime(2026, 4, 9),
            new DateTime(2026, 4, 12),
            ImportStatus.Completed).ToList();

        // Assert
        Assert.Equal(2, allResult.Count);
        Assert.All(allResult, x => Assert.False(x.IsAutoSaved));
        Assert.True(allResult[0].CreatedAt > allResult[1].CreatedAt);

        Assert.Single(keywordResult);
        Assert.Equal(import1.Id, keywordResult[0].Id);

        Assert.Single(filterResult);
        Assert.Equal(import1.Id, filterResult[0].Id);
    }

    [Fact]
    public async Task GetImportByIdAsync_NenTraVeDungPhieuNhap()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var queries = new ImportQueries();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            SKU = "SKU-BY-ID",
            Name = "ById Product",
            Category = null!
        };

        var importLog = new ImportLog
        {
            Id = Guid.NewGuid(),
            Status = ImportStatus.Draft,
            IsAutoSaved = false,
            Details = new List<ImportLogDetail>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    QuantityAdded = 7,
                    ActualImportPrice = 3000
                }
            }
        };

        context.Products.Add(product);
        context.ImportLogs.Add(importLog);
        await context.SaveChangesAsync();

        // Act
        var result = await queries.GetImportByIdAsync(importLog.Id, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(importLog.Id, result!.Id);
        Assert.Single(result.Details);
    }

    [Fact]
    public async Task GetImportSummaryAsync_NenTinhDungTongSoPhieuTongTienVaSoPhieuNhap()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var queries = new ImportQueries();

        context.ImportLogs.AddRange(
            new ImportLog
            {
                Id = Guid.NewGuid(),
                IsAutoSaved = false,
                Status = ImportStatus.Completed,
                TotalAmount = 1000
            },
            new ImportLog
            {
                Id = Guid.NewGuid(),
                IsAutoSaved = false,
                Status = ImportStatus.Completed,
                TotalAmount = 2000
            },
            new ImportLog
            {
                Id = Guid.NewGuid(),
                IsAutoSaved = false,
                Status = ImportStatus.Draft,
                TotalAmount = 9999
            },
            new ImportLog
            {
                Id = Guid.NewGuid(),
                IsAutoSaved = true,
                Status = ImportStatus.Completed,
                TotalAmount = 5000
            }
        );
        await context.SaveChangesAsync();

        // Act
        var summary = await queries.GetImportSummaryAsync(context);

        // Assert
        Assert.Equal(3, summary.TotalCount);
        Assert.Equal(3000, summary.TotalAmount);
        Assert.Equal(1, summary.DraftCount);
    }

}
