using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Core.Models;

public class ProductImage
{
    [Key]
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; }

    [Required]
    [MaxLength(500)]
    public string ImagePath { get; set; } // Chứa đường dẫn cục bộ, vd: /images/coca.jpg

    public bool IsPrimary { get; set; }
}
