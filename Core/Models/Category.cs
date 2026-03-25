using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models;

[Table("Categories")]
public class Category
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // Navigation property (Một danh mục có nhiều sản phẩm)
    public ICollection<Product> Products { get; set; } = new List<Product>();
}