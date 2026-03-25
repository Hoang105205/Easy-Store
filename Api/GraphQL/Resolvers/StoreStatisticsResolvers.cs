using Core.Data;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Api.GraphQL.Resolvers;

[ExtendObjectType(typeof(StoreStatistics))]
public class StoreStatisticsResolvers
{
    // Field 1: totalIncome
    public async Task<long> GetTotalIncome(
        [Parent] StoreStatistics parent,
        [Service] AppDbContext context)
    {
        var query = context.Orders.AsQueryable();

        if (parent.StartDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= parent.StartDate.Value);
        }

        return await query.SumAsync(o => o.TotalAmount);
    }

    // Field 2: totalNewOrders
    public async Task<int> GetTotalNewOrders(
        [Parent] StoreStatistics parent,
        [Service] AppDbContext context)
    {
        var query = context.Orders.AsQueryable();

        if (parent.StartDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= parent.StartDate.Value);
        }

        return await query.CountAsync();
    }

    // Field 3: getTotalRevenue
    public async Task<List<DailyRevenue>> GetTotalRevenue(
        [Parent] StoreStatistics parent,
        [Service] AppDbContext context)
    {
        var query = context.Orders.AsQueryable();

        if (parent.StartDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= parent.StartDate.Value);
        }

        return await query.GroupBy(o => o.OrderDate.Date)
                          .Select(g => new DailyRevenue
                          {
                              Date = g.Key,
                              Revenue = g.Sum(o => o.TotalAmount)
                          })
                          .OrderBy(x => x.Date)
                          .ToListAsync();
    }

    // Field 4: bestSellingProducts
    public async Task<List<ProductStat>> GetBestSellingProducts(
        [Parent] StoreStatistics parent,
        [Service] AppDbContext context)
    {
        var query = context.OrderItems.AsQueryable();

        if (parent.StartDate.HasValue)
        {
            query = query.Where(o => o.Order.OrderDate >= parent.StartDate.Value);
        }

        return await  query.GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                           .Select(g => new ProductStat
                           {
                               Id = g.Key.ProductId,
                               Name = g.Key.Name ?? "Unknown",
                               Quantity = g.Sum(oi => oi.Quantity),
                               LastOrder = g.Max(oi => oi.Order.OrderDate)
                           })
                           .OrderByDescending(x => x.Quantity)
                           .Take(5)
                           .ToListAsync();
    }

    // Field 5: nearlyOutOfStock
    public async Task<List<ProductStat>> GetNearlyOutOfStock(
        [Parent] StoreStatistics parent,
        [Service] AppDbContext context)
    {
        return await context.Products
            .Where(p => p.StockQuantity > 0 && p.StockQuantity <= p.MinimumStockQuantity)
            .Select(p => new ProductStat
            {
                Id = p.Id,
                Name = p.Name ?? "Unknown",
                Quantity = p.StockQuantity
            })
            .OrderBy(p => p.Quantity)
            .Take(5)
            .ToListAsync();
    }
}
