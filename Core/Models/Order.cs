using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models;

[Table("Orders")]
public class Order
{
    // Định nghĩa các trạng thái của đơn hàng
    public static class Statuses
    {
        public const string Created = "Created";
        public const string Paid = "Paid";
    }

    [Key]
    public Guid Id { get; set; }

    // ReceiptNumber sẽ được database tự tăng (SERIAL)
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReceiptNumber { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = Statuses.Created; 

    [MaxLength(100)]
    public string? Note { get; set; } = null;

    public long TotalAmount { get; set; } = 0;
    public long TotalProfit { get; set; } = 0;

    public bool IsDraft { get; set; } = true;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}