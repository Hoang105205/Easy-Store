using Core.Data;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using static Core.Models.Order;

namespace Api.GraphQL.Resolvers;

[ExtendObjectType(typeof(StoreStatistics))]
public class StoreStatisticsResolvers
{
    // Field: actualGrossRevenue
    public async Task<long> GetActualGrossRevenue(
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

    // Field: actualRevenue
    public async Task<long> GetActualRevenue(
        [Parent] StoreStatistics parent,
        [Service] AppDbContext context)
    {
        var query = context.Orders.Where(o => o.Status == Statuses.Paid);

        if (parent.StartDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= parent.StartDate.Value);
        }

        return await query.SumAsync(o => o.TotalAmount);
    }

    // Field: totalPercentIncreaseRevenue
    public async Task<double> GetTotalPercentIncreaseRevenue(
        [Parent] StoreStatistics parent,
        [Service] AppDbContext context)
    {
        if (parent.StartDate == null || parent.PreviousDate == null)
        {
            return 0;
        }

        var currentPeriodRevenue = await context.Orders
            .Where(o => o.OrderDate >= parent.StartDate.Value && o.Status == Statuses.Paid)
            .SumAsync(o => o.TotalAmount);

        var previousPeriodRevenue = await context.Orders
            .Where(o => o.OrderDate >= parent.PreviousDate.Value && o.OrderDate < parent.StartDate.Value && o.Status == Statuses.Paid)
            .SumAsync(o => o.TotalAmount);

        if (previousPeriodRevenue == 0)
        {
            return currentPeriodRevenue > 0 ? 100 : 0;
        }

        return ((double)(currentPeriodRevenue - previousPeriodRevenue) / previousPeriodRevenue) * 100;
    }

    // Field: actualGrossProfit
    public async Task<long> GetActualGrossProfit(
        [Parent] StoreStatistics parent,
        [Service] AppDbContext context)
    {
        var query = context.Orders.AsQueryable();

        if (parent.StartDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= parent.StartDate.Value);
        }

        return await query.SumAsync(o => o.TotalProfit);
    }

    // Field: actualProfit
    public async Task<long> GetActualProfit(
        [Parent] StoreStatistics parent,
        [Service] AppDbContext context)
    {
        var query = context.Orders.Where(o => o.Status == Statuses.Paid);

        if (parent.StartDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= parent.StartDate.Value);
        }

        return await query.SumAsync(o => o.TotalProfit);
    }

    // Field: totalPercentIncreaseProfit
    public async Task<double> GetTotalPercentIncreaseProfit(
        [Parent] StoreStatistics parent,
        [Service] AppDbContext context)
    {
        if (parent.StartDate == null || parent.PreviousDate == null)
        {
            return 0;
        }

        var currentPeriodProfit = await context.Orders
            .Where(o => o.OrderDate >= parent.StartDate.Value && o.Status == Statuses.Paid)
            .SumAsync(o => o.TotalProfit);

        var previousPeriodProfit = await context.Orders
            .Where(o => o.OrderDate >= parent.PreviousDate.Value && o.OrderDate < parent.StartDate.Value && o.Status == Statuses.Paid)
            .SumAsync(o => o.TotalProfit);

        if (previousPeriodProfit == 0)
        {
            return currentPeriodProfit > 0 ? 100 : 0;
        }

        return ((double)(currentPeriodProfit - previousPeriodProfit) / previousPeriodProfit) * 100;
    }

    // Field: totalNewOrders
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

    // Field: totalPaidOrders
    public async Task<int> GetTotalPaidOrders(
        [Parent] StoreStatistics parent,
        [Service] AppDbContext context)
    {
        var query = context.Orders.Where(o => o.Status == Statuses.Paid);

        if (parent.StartDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= parent.StartDate.Value);
        }

        return await query.CountAsync();
    }

    // Field: totalRevenueGraph
    public async Task<List<DailyRevenue>> GetTotalRevenueGraph(
        [Parent] StoreStatistics parent,
        [Service] AppDbContext context)
    {
        var query = context.Orders.Where(o => o.Status == Statuses.Paid);

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

    // Field: bestSellingProducts
    public async Task<List<ProductStat>> GetBestSellingProducts(
        [Parent] StoreStatistics parent,
        [Service] AppDbContext context)
    {
        var query = context.OrderItems.Where(oi => oi.Order.Status == Statuses.Paid);

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
                           .Where(x => x.Quantity > 0)
                           .OrderByDescending(x => x.Quantity)
                           .Take(10)
                           .ToListAsync();
    }

    // Field: nearlyOutOfStock
    public async Task<List<ProductStat>> GetNearlyOutOfStock(
        [Parent] StoreStatistics parent,
        [Service] AppDbContext context)
    {
        return await context.Products
            .Where(p => p.StockQuantity <= p.MinimumStockQuantity)
            .Select(p => new ProductStat
            {
                Id = p.Id,
                Name = p.Name ?? "Unknown",
                Quantity = p.StockQuantity
            })
            .OrderBy(p => p.Quantity)
            .ToListAsync();
    }
}
