#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models;

[Table("OrderItems")]
public class OrderItem
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
    public long UnitSalePrice { get; set; }

    // Lưu lại giá MAC tại thời điểm bán để tính lãi chuẩn xác
    public long? UnitImportPrice { get; set; }

    public long TotalPrice { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}