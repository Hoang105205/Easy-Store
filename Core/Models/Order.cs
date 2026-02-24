using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models;

[Table("Orders")]
public class Order
{
    [Key]
    public Guid Id { get; set; }

    // ReceiptNumber sẽ được database tự tăng (SERIAL)
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReceiptNumber { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Created";

    public long TotalAmount { get; set; } = 0;
    public long TotalProfit { get; set; } = 0;

    public bool IsDraft { get; set; } = true;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}