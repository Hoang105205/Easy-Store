using Api.Utils;
using Core.Data;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Api.GraphQL.Mutations;

public record ImportLogDetailInput(
    Guid ProductId,
    int QuantityAdded,
    long ActualImportPrice
);

public record CompleteImportInput(
    List<ImportLogDetailInput> Details,
    bool isDraft,
    bool isAutoSave
);

[ExtendObjectType(Name = "Mutation")]
public class ImportMutation
{
    public async Task<ImportLog> CompleteImportAsync(
        CompleteImportInput input,
        [Service] AppDbContext context
    )
    {
        // 1. Phân loại Trạng thái & Cờ Auto-save rõ ràng theo nghiệp vụ
        var isCompleted = !input.isDraft && !input.isAutoSave; // Chỉ Hoàn thành khi cả 2 cờ kia là false
        var finalStatus = isCompleted ? ImportStatus.Completed : ImportStatus.Draft;

        // 2. Dọn dẹp rác (Bản Auto-save cũ)
        // Bất kể hành động hiện tại là gì (lưu ngầm tiếp, lưu nháp hay chốt sổ), 
        // ta luôn tìm cái bản auto-save cũ duy nhất và xóa nó đi để nhường chỗ hoặc hủy bỏ.
        var existingAutoSave = await context.ImportLogs
            .Include(i => i.Details)
            .FirstOrDefaultAsync(i => i.IsAutoSaved == true);

        if (existingAutoSave != null)
        {
            context.ImportLogs.Remove(existingAutoSave);
        }

        // 3. Khởi tạo Phiếu Nhập
        var importLog = new ImportLog
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Status = finalStatus,
            IsAutoSaved = input.isAutoSave, // Nếu là Phiếu Tạm thực sự, cờ này sẽ = false
            TotalAmount = 0,
            Details = new List<ImportLogDetail>()
        };

        long totalAmount = 0;

        foreach (var item in input.Details)
        {
            var product = await context.Products.FindAsync(item.ProductId);

            if (product == null) continue;

            // 4. CHỐT SỔ: Chỉ duy nhất trạng thái Hoàn Thành mới được phép đụng vào DB Products
            if (isCompleted)
            {
                // Tính giá MAC
                long newMacPrice = ImportHelper.CalculateNewMacPrice(
                    currentMacPrice: product.ImportPrice ?? 0,
                    currentStock: product.StockQuantity,
                    quantityAdded: item.QuantityAdded,
                    actualImportPrice: item.ActualImportPrice
                );

                // Cộng kho & Cập nhật giá
                product.StockQuantity = product.StockQuantity + item.QuantityAdded;
                product.ImportPrice = newMacPrice;
                product.UpdatedAt = DateTime.UtcNow;
            }

            var detail = new ImportLogDetail
            {
                Id = Guid.NewGuid(),
                ImportLogId = importLog.Id,
                ProductId = product.Id,
                QuantityAdded = item.QuantityAdded,
                ActualImportPrice = item.ActualImportPrice
            };

            importLog.Details.Add(detail);
            totalAmount += item.QuantityAdded * item.ActualImportPrice;
        }

        importLog.TotalAmount = totalAmount;

        context.ImportLogs.Add(importLog);

        await context.SaveChangesAsync();

        return importLog;
    }
}
