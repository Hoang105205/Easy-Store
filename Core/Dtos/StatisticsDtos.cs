using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Dtos;

public record DashboardSummaryDto(
    long TotalRevenue,      // Tổng doanh thu
    long TotalProfit,       // Tổng lợi nhuận
    int TotalOrders,        // Tổng số đơn hàng
    int LowStockProducts    // Số SP sắp hết hàng (Stock < Minimum)
);

// 2. Dữ liệu cho biểu đồ (Dùng chung cho cả đường và cột)
public record ChartDataDto(
    string Label,           // Ví dụ: "25/03", "Tháng 3"
    long Revenue,
    long Profit
);

// 3. Top sản phẩm mang lại lợi nhuận cao nhất
public record TopProductStatsDto(
    string ProductName,
    string Sku,
    int QuantitySold,
    long Revenue,
    long Profit             // Lợi nhuận = (Giá bán - Giá vốn) * SL
);

// 4. Object tổng hợp trả về cho 1 lần gọi API
public record StatisticsResultDto(
    DashboardSummaryDto Summary,
    List<ChartDataDto> ChartData,
    List<TopProductStatsDto> TopProducts
);
