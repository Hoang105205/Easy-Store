using Core.Data;
using Core.Models;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Api.Utils;

namespace Api.GraphQL.Mutations;

public class DraftOrderItemInput
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public long UnitSalePrice { get; set; }
}

public class UpsertDraftOrderInput
{
    public Guid? OrderId { get; set; } // Null nếu là đơn mới hoàn toàn
    public string? Note { get; set; }
    public List<DraftOrderItemInput> Items { get; set; } = new();
}

[ExtendObjectType(typeof(Mutation))]
public class OrderMutation
{
    public async Task<Order> PayOrderAsync(Guid id, [Service] AppDbContext context)
    {
        var order = await context.Orders.FindAsync(id);
        if (order == null) throw new Exception("Không tìm thấy đơn hàng");

        if (order.Status == Order.Statuses.Paid) throw new Exception("Đơn hàng đã được thanh toán");

        order.Status = Order.Statuses.Paid;
        order.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return order;
    }

    public async Task<bool> DeleteOrderAsync(Guid id, [Service] AppDbContext context)
    {
        var order = await context.Orders.FindAsync(id);
        if (order == null) return false;

        context.Orders.Remove(order);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<Order> UpsertDraftOrderAsync(UpsertDraftOrderInput input, [Service] AppDbContext context)
    {
        Order order;

        if (input.OrderId.HasValue && input.OrderId.Value != Guid.Empty)
        {
            order = await context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == input.OrderId.Value);
            if (order == null) throw new Exception("Không tìm thấy đơn nháp");
            order.Note = input.Note;
            order.UpdatedAt = DateTime.UtcNow;
            // Dọn sạch item cũ để chèn lại cho nhanh
            context.OrderItems.RemoveRange(order.OrderItems);
        }
        else
        {
            // Nếu chưa có ID thì tạo Order mới với IsDraft = true
            order = new Order
            {
                Id = Guid.NewGuid(),
                Status = Order.Statuses.Created,
                Note = input.Note,
                IsDraft = true,
                OrderDate = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrderItems = new List<OrderItem>()
            };
            await context.Orders.AddAsync(order);
        }

        long totalAmount = OrderHelper.CalculateTotalAmount(input.Items);
        var newItems = new List<OrderItem>();

        foreach (var item in input.Items)
        {
            newItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitImportPrice = null,
                UnitSalePrice = item.UnitSalePrice,
                TotalPrice = item.Quantity * item.UnitSalePrice,
            });
        }

        order.OrderItems = newItems;
        order.TotalAmount = totalAmount;

        await context.SaveChangesAsync();
        return order;
    }

    public async Task<Order> FinalizeOrderAsync(Guid orderId, [Service] AppDbContext context)
    {
        var order = await context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) throw new Exception("Không tìm thấy hóa đơn");
        if (!order.IsDraft) throw new Exception("Hóa đơn này đã được tạo từ trước");

        // Cập nhật giá ImportPrice cho từng item
        foreach (var item in order.OrderItems)
        {
            item.UnitImportPrice = item.Product?.ImportPrice ?? 0;
        }

        order.TotalProfit = OrderHelper.CalculateTotalProfit(order.OrderItems);
        order.IsDraft = false; // Chuyển hóa đơn sang trạng thái chính thức
        order.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return order;
    }
}