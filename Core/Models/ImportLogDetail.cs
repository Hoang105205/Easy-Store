using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Core.Models;

[Table("ImportLogDetails")]
public class ImportLogDetail
{
    [Key]
    public Guid Id { get; set; }

    // Liên kết về Phiếu nhập cha
    public Guid ImportLogId { get; set; }

    // Liên kết về Sản phẩm
    public Guid ProductId { get; set; }

    public int QuantityAdded { get; set; }

    // Giá nhập thực tế đợt này cho riêng sản phẩm này
    public long ActualImportPrice { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ImportLogId))]
    public ImportLog? ImportLog { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}
