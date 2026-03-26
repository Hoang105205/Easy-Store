#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models;

[Table("Products")]
public class Product
{
    [Key]
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    [Required]
    [MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public long ?ImportPrice { get; set; } // Giá MAC
    public long ?SalePrice { get; set; }
    public int StockQuantity { get; set; } = 0;
    public int AvailableStockQuantity { get; set; } = 0;
    public int MinimumStockQuantity { get; set; } = 0;

    // Cờ Auto-save
    public bool IsDraft { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties (Liên kết khóa ngoại)
    [ForeignKey(nameof(CategoryId))]
    public Category Category { get; set; }

    public ICollection<ImportLogDetail> ImportLogs { get; set; } = new List<ImportLogDetail>();

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}