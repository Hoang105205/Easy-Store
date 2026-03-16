#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models;
public enum ImportStatus
{
    Draft = 0,      // Phiếu tạm: Chỉ lưu nháp, CHƯA cộng kho, CHƯA tính giá vốn.
    Completed = 1,  // Hoàn tất: ĐÃ cộng số lượng vào tồn kho, ĐÃ tính lại MAC.
}

[Table("ImportLogs")]
public class ImportLog
{
    [Key]
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public long TotalAmount { get; set; }

    // 1. NGHIỆP VỤ PHIẾU TẠM: Quản lý trạng thái áp dụng vào DB
    public ImportStatus Status { get; set; } = ImportStatus.Draft;

    // 2. NGHIỆP VỤ AUTO-SAVE: Cờ đánh dấu đây là bản lưu tự động ngầm
    public bool IsAutoSaved { get; set; } = false;

    public ICollection<ImportLogDetail> Details { get; set; } = new List<ImportLogDetail>();
}