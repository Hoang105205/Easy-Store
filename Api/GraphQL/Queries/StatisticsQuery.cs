using Core.Data;
using Core.Dtos;
using Core.Models;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;

namespace Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
public class StatisticsQuery
{
    public async Task<StatisticsResultDto> GetStatistics(
        [Service] AppDbContext context,
        DateTime fromDate,
        DateTime toDate)
    {
        var startOfFromDate = fromDate.Date;
        var endOfToDate = toDate.Date.AddDays(1).AddTicks(-1);

        var orders = await context.Orders
            .Where(o => !o.IsDraft && o.Status == Order.Statuses.Paid
                    && o.OrderDate >= startOfFromDate && o.OrderDate <= endOfToDate)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .ToListAsync();

        // --- 1. LOGIC AUTO-BINNING ---
        var totalDays = (endOfToDate - startOfFromDate).TotalDays;
        string dateFormat;
        Func<DateTime, DateTime> groupKeySelector;

        if (totalDays <= 31) // Dưới 1 tháng -> Nhóm theo Ngày
        {
            dateFormat = "dd/MM";
            groupKeySelector = d => d.Date;
        }
        else if (totalDays <= 365) // Dưới 1 năm -> Nhóm theo Tháng
        {
            dateFormat = "MM/yyyy";
            groupKeySelector = d => new DateTime(d.Year, d.Month, 1);
        }
        else // Trên 1 năm -> Nhóm theo Năm
        {
            dateFormat = "yyyy";
            groupKeySelector = d => new DateTime(d.Year, 1, 1);
        }

        // --- 2. TẠO TRỤC THỜI GIAN ĐẦY ĐỦ (Fix lỗi sắp xếp & lấp khoảng trống) ---
        var chartDataDict = new Dictionary<string, ChartDataDto>();
        var current = startOfFromDate;

        // Khởi tạo tất cả các điểm trên biểu đồ với giá trị 0
        while (current <= endOfToDate)
        {
            var label = current.ToString(dateFormat);
            if (!chartDataDict.ContainsKey(label))
            {
                chartDataDict.Add(label, new ChartDataDto(label, 0, 0));
            }

            // Tăng tiến trình dựa trên đơn vị nhóm
            if (totalDays <= 31) current = current.AddDays(1);
            else if (totalDays <= 365) current = current.AddMonths(1);
            else current = current.AddYears(1);
        }

        // --- 3. ĐỔ DỮ LIỆU THỰC TẾ VÀO ---
        var groupedOrders = orders
            .GroupBy(o => groupKeySelector(o.OrderDate))
            .ToList();

        foreach (var group in groupedOrders)
        {
            var label = group.Key.ToString(dateFormat);
            if (chartDataDict.ContainsKey(label))
            {
                chartDataDict[label] = new ChartDataDto(
                    Label: label,
                    Revenue: group.Sum(o => o.TotalAmount),
                    Profit: group.Sum(o => o.TotalProfit)
                );
            }
        }

        var finalChartData = chartDataDict.Values.ToList();

        // 5. Tính toán Summary
        var summary = new DashboardSummaryDto(
            TotalRevenue: orders.Sum(o => o.TotalAmount),
            TotalProfit: orders.Sum(o => o.TotalProfit),
            TotalOrders: orders.Count,
            LowStockProducts: await context.Products.CountAsync(p => p.StockQuantity < p.MinimumStockQuantity)
        );

        // 6. Tính toán Top Products (Top 5 lợi nhuận cao nhất)
        var topProducts = orders
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => new { oi.ProductId, oi.Product?.Name, oi.Product?.SKU })
            .Select(g => new TopProductStatsDto(
                ProductName: g.Key.Name ?? "N/A",
                Sku: g.Key.SKU ?? "N/A",
                QuantitySold: g.Sum(x => x.Quantity),
                Revenue: g.Sum(x => x.TotalPrice),
                Profit: g.Sum(x => (x.UnitSalePrice - (x.UnitImportPrice ?? 0)) * x.Quantity)
            ))
            .OrderByDescending(x => x.Profit)
            .Take(5)
            .ToList();

        return new StatisticsResultDto(summary, finalChartData, topProducts);
    }
}