using Core.Data;
using Core.Models;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.RegularExpressions;


namespace Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
internal class OrderQueries
{
    [UsePaging(DefaultPageSize = 20)]
    [UseSorting]
    public IQueryable<Order> GetOrders([Service] AppDbContext context)
    {
        var query = context.Orders.Where(o => !o.IsDraft).AsQueryable();
        query = query.OrderByDescending(p => p.OrderDate).ThenBy(p => p.Id);

        return query;
    }

    [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
    [UseProjection]
    [UseSorting]
    public IQueryable<Order> GetOrdersPagination(
        [Service] AppDbContext context,
        string? receiptNumber = null,
        DateTime? startDate = null,
        DateTime? endDate = null
    )
    {
        var query = context.Orders.Where(o => !o.IsDraft).AsQueryable();

        // Lọc theo mã hóa đơn (Tìm kiếm gần đúng)
        if (!string.IsNullOrWhiteSpace(receiptNumber) && int.TryParse(receiptNumber, out int receiptInt))
        {
            query = query.Where(o => o.ReceiptNumber == receiptInt);
        }

        // Lọc từ ngày
        if (startDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= startDate.Value);
        }

        // Lọc đến ngày
        if (endDate.HasValue)
        {
            // Cộng thêm 1 ngày và trừ đi 1 tick để lấy đến 23:59:59 của ngày được chọn
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.OrderDate <= endOfDay);
        }

        query = query.OrderByDescending(p => p.OrderDate).ThenBy(p => p.Id);

        return query;
    }

    [UseSingleOrDefault]
    [UseProjection]
    public IQueryable<Order> GetOrderById(Guid id, [Service] AppDbContext dbContext)
    { 
        return dbContext.Orders.Where(p => p.Id == id);
    }

    [UseProjection]
    public IQueryable<Order> GetDraftOrders([Service] AppDbContext context)
    {
        return context.Orders.Where(o => o.IsDraft).OrderBy(o => o.OrderDate);
    }
}
