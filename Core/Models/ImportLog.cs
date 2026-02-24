#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models;

[Table("ImportLogs")]
public class ImportLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public int QuantityAdded { get; set; }

    // Giá nhập thực tế đợt này
    public long ActualImportPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property (Liên kết ngược lại với bảng Product)
    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}