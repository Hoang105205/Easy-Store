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
        var order = await context.Orders
        .Include(o => o.OrderItems)
        .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return false;

        // Nếu hủy một đơn nháp, hoàn trả lại AvailableStockQuantity
        if (order.IsDraft)
        {
            foreach (var item in order.OrderItems)
            {
                var product = await context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    if (order.IsDraft)
                    {
                        // Nếu xóa đơn nháp, chỉ hoàn trả khóa mềm (AvailableStockQuantity)
                        product.AvailableStockQuantity += item.Quantity;
                    }
                    //else
                    //{
                    //    // Nếu xóa đơn thật, phải hoàn trả cả kho thực (StockQuantity) và kho mềm (Available)
                    //    product.StockQuantity += item.Quantity;
                    //    product.AvailableStockQuantity += item.Quantity;
                    //}
                }
            }
        }

        context.Orders.Remove(order);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<Order> UpsertDraftOrderAsync(UpsertDraftOrderInput input, [Service] AppDbContext context)
    {
        Order order;
        long totalAmount = OrderHelper.CalculateTotalAmount(input.Items);

        if (input.OrderId.HasValue && input.OrderId.Value != Guid.Empty)
        {
            order = await context.Orders.FirstOrDefaultAsync(o => o.Id == input.OrderId.Value);
            if (order == null) throw new Exception("Không tìm thấy đơn nháp");

            order.Note = input.Note;
            order.TotalAmount = totalAmount;
            order.UpdatedAt = DateTime.UtcNow;

            // Lấy trực tiếp danh sách item cũ và trả lại AvailableStockQuantity trước khi xóa
            var oldItems = await context.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();
            foreach (var oldItem in oldItems)
            {
                var productToRestore = await context.Products.FindAsync(oldItem.ProductId);
                if (productToRestore != null)
                {
                    productToRestore.AvailableStockQuantity += oldItem.Quantity;
                }
            }
            context.OrderItems.RemoveRange(oldItems);
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
                TotalAmount = totalAmount,
                OrderDate = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            await context.Orders.AddAsync(order);
        }

        // Kiểm tra và trừ AvailableStockQuantity khi thêm items mới
        var newItems = new List<OrderItem>();
        foreach (var item in input.Items)
        {
            var product = await context.Products.FindAsync(item.ProductId);
            if (product == null) throw new Exception("Không tìm thấy sản phẩm");

            // Validate số lượng
            if (product.AvailableStockQuantity < item.Quantity)
            {
                throw new Exception($"Sản phẩm '{product.Name}' chỉ còn {product.AvailableStockQuantity} sản phẩm khả dụng.");
            }

            // Trừ soft lock
            product.AvailableStockQuantity -= item.Quantity;

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

        await context.OrderItems.AddRangeAsync(newItems);

        // Lưu thay đổi xuống DB (lệnh DELETE items cũ và INSERT items mới)
        await context.SaveChangesAsync();

        // Dọn sạch bộ nhớ đệm Tracking của DbContext
        context.ChangeTracker.Clear();

        // Kéo lại freshdate từ DB, kèm theo các bảng con (Product, Category)
        var savedOrder = await context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(o => o.Id == order.Id);

        return savedOrder ?? order;
    }

    public async Task<Order> FinalizeOrderAsync(Guid orderId, [Service] AppDbContext context)
    {
        var order = await context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) throw new Exception("Không tìm thấy hóa đơn");
        if (!order.IsDraft) throw new Exception("Hóa đơn này đã được tạo từ trước");
        
        foreach (var item in order.OrderItems)
        {
            // Cập nhật giá ImportPrice cho từng item
            item.UnitImportPrice = item.Product?.ImportPrice ?? 0;

            if (item.Product != null)
            {
                // chỉ trừ StockQuantity, không trừ AvailableStockQuantity nữa vì đã trừ lúc autosave rồi
                item.Product.StockQuantity -= item.Quantity;
            }
        }

        order.TotalProfit = OrderHelper.CalculateTotalProfit(order.OrderItems);
        order.IsDraft = false; // Chuyển hóa đơn sang trạng thái chính thức
        order.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return order;
    }
}