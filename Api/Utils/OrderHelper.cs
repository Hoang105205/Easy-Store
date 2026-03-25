using System;
using System.Collections.Generic;
using System.Text;

using Api.GraphQL.Mutations; // Để nhận DraftOrderItemInput

namespace Api.Utils;

public class OrderHelper
{
    // Hàm tính tổng tiền cho đơn nháp
    public static long CalculateTotalAmount(List<DraftOrderItemInput> items)
    {
        long totalAmount = 0;
        foreach (var item in items)
        {
            totalAmount += item.Quantity * item.UnitSalePrice;
        }
        return totalAmount;
    }

    // Hàm tính tổng lợi nhuận khi chốt đơn (Finalize), nhận vào danh sách OrderItems đã được Include Product từ DB
    public static long CalculateTotalProfit(IEnumerable<Core.Models.OrderItem> orderItems)
    {
        long totalProfit = 0;
        foreach (var item in orderItems)
        {
            long importPrice = item.Product?.ImportPrice ?? 0;
            totalProfit += (item.UnitSalePrice - importPrice) * item.Quantity;
        }
        return totalProfit;
    }
}
