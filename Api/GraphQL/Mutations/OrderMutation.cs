using Core.Data;
using Core.Models;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Api.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class OrderMutation
{
    public async Task<Order> PayOrderAsync(Guid id, [Service] AppDbContext context)
    {
        var order = await context.Orders.FindAsync(id);
        if (order == null) throw new Exception("Không tìm thấy đơn hàng");

        if (order.Status == "Paid") throw new Exception("Đơn hàng đã được thanh toán");

        order.Status = "Paid";
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
}