using Core.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using static Core.Models.Order;

namespace Api.GraphQL.Queries;

[ExtendObjectType("Query")]
public class DashboardQueries
{
    public async Task<StoreStatistics> GetDashboardStats(
        AppDbContext context,
        int? days = 7)
    {
        DateTime? startDate = null;
        DateTime? previousDate = null;

        if (days.HasValue)
        {
            startDate = DateTime.UtcNow.Date.AddDays(-days.Value);
            previousDate = DateTime.UtcNow.Date.AddDays(-2 * days.Value);
        }

        var allOrders = context.Orders.AsQueryable();
        var paidOrders = context.Orders.Where(o => o.Status == Statuses.Paid);

        if (startDate.HasValue)
        {
            allOrders = allOrders.Where(o => o.OrderDate >= startDate.Value);
            paidOrders = paidOrders.Where(o => o.OrderDate >= startDate.Value);
        }

        var actualGrossRevenue = await allOrders.SumAsync(o => o.TotalAmount);
        var actualRevenue = await paidOrders.SumAsync(o => o.TotalAmount);

        var actualGrossProfit = await allOrders.SumAsync(o => o.TotalProfit);
        var actualProfit = await paidOrders.SumAsync(o => o.TotalProfit);

        var totalNewOrders = await allOrders.CountAsync();
        var totalPaidOrders = await paidOrders.CountAsync();

        double totalPercentIncreaseRevenue = 0;
        double totalPercentIncreaseProfit = 0;

        if (startDate.HasValue && previousDate.HasValue)
        {
            var currentPeriodRevenue = await context.Orders
                .Where(o => o.OrderDate >= startDate.Value && o.Status == Statuses.Paid)
                .SumAsync(o => o.TotalAmount);

            var previousPeriodRevenue = await context.Orders
                .Where(o => o.OrderDate >= previousDate.Value && o.OrderDate < startDate.Value && o.Status == Statuses.Paid)
                .SumAsync(o => o.TotalAmount);

            if (previousPeriodRevenue == 0)
            {
                totalPercentIncreaseRevenue = currentPeriodRevenue > 0 ? 100 : 0;
            }
            else
            {
                totalPercentIncreaseRevenue = ((double)(currentPeriodRevenue - previousPeriodRevenue) / previousPeriodRevenue) * 100;
            }

            var currentPeriodProfit = await context.Orders
                .Where(o => o.OrderDate >= startDate.Value && o.Status == Statuses.Paid)
                .SumAsync(o => o.TotalProfit);

            var previousPeriodProfit = await context.Orders
                .Where(o => o.OrderDate >= previousDate.Value && o.OrderDate < startDate.Value && o.Status == Statuses.Paid)
                .SumAsync(o => o.TotalProfit);

            if (previousPeriodProfit == 0)
            {
                totalPercentIncreaseProfit = currentPeriodProfit > 0 ? 100 : 0;
            }
            else
            {
                totalPercentIncreaseProfit = ((double)(currentPeriodProfit - previousPeriodProfit) / previousPeriodProfit) * 100;
            }
        }

        var totalRevenueGraph = await paidOrders
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new DailyRevenue
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var bestSellingProductsQuery = context.OrderItems.Where(oi => oi.Order.Status == Statuses.Paid);

        if (startDate.HasValue)
        {
            bestSellingProductsQuery = bestSellingProductsQuery.Where(o => o.Order.OrderDate >= startDate.Value);
        }

        var bestSellingProducts = await bestSellingProductsQuery
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
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

        var nearlyOutOfStock = await context.Products
            .Where(p => p.StockQuantity <= p.MinimumStockQuantity)
            .Select(p => new ProductStat
            {
                Id = p.Id,
                Name = p.Name ?? "Unknown",
                Quantity = p.StockQuantity
            })
            .OrderBy(p => p.Quantity)
            .ToListAsync();

        return new StoreStatistics
        {
            ActualGrossRevenue = actualGrossRevenue,
            ActualRevenue = actualRevenue,
            TotalPercentIncreaseRevenue = totalPercentIncreaseRevenue,
            ActualGrossProfit = actualGrossProfit,
            ActualProfit = actualProfit,
            TotalPercentIncreaseProfit = totalPercentIncreaseProfit,
            TotalNewOrders = totalNewOrders,
            TotalPaidOrders = totalPaidOrders,
            TotalRevenueGraph = totalRevenueGraph,
            BestSellingProducts = bestSellingProducts,
            NearlyOutOfStock = nearlyOutOfStock
        };
    }
}
public class ProductStat
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime LastOrder { get; set; }
}

public class DailyRevenue
{
    public DateTime Date { get; set; }
    public long Revenue { get; set; }
}

public class StoreStatistics
{
    public long ActualGrossRevenue { get; set; }
    public long ActualRevenue { get; set; }
    public double TotalPercentIncreaseRevenue { get; set; }
    public long ActualGrossProfit { get; set; }
    public long ActualProfit { get; set; }
    public double TotalPercentIncreaseProfit { get; set; }
    public int TotalNewOrders { get; set; }
    public int TotalPaidOrders { get; set; }
    public List<DailyRevenue> TotalRevenueGraph { get; set; } = new();
    public List<ProductStat> BestSellingProducts { get; set; } = new();
    public List<ProductStat> NearlyOutOfStock { get; set; } = new();
}