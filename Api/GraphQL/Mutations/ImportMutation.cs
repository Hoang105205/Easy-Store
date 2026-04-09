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

        // BỨC TƯỜNG LỬA: Gom nhóm các sản phẩm trùng ID lại thành 1 dòng duy nhất
        var groupedDetails = input.Details
            .GroupBy(d => d.ProductId)
            .Select(g => new ImportLogDetailInput(
                ProductId: g.Key,
                QuantityAdded: g.Sum(x => x.QuantityAdded),    // Cộng dồn toàn bộ số lượng
                ActualImportPrice: g.First().ActualImportPrice // Lấy giá nhập của dòng đầu tiên
            ))
            .ToList();

        foreach (var item in groupedDetails)
        {
            var product = await context.Products.FindAsync(item.ProductId);

            if (product == null)
            {
                throw new GraphQLException($"Sản phẩm có ID {item.ProductId} không tồn tại hoặc đã bị xóa khỏi hệ thống. Vui lòng tải lại trang!");
            }

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
                product.AvailableStockQuantity = product.AvailableStockQuantity + item.QuantityAdded;
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

    public async Task<bool> DeleteImportAsync(
        Guid importId,
        [Service] AppDbContext context)
    {
        // 1. Tìm phiếu trong DB
        var importLog = await context.ImportLogs.FindAsync(importId);

        if (importLog == null)
        {
            throw new GraphQLException("Không tìm thấy phiếu nhập này!");
        }

        // 2. Chặn đứng nếu người dùng cố tình xóa phiếu đã Hoàn thành
        if (importLog.Status == ImportStatus.Completed)
        {
            throw new GraphQLException("Không thể xóa phiếu đã hoàn thành vì đã cộng vào tồn kho!");
        }

        // 3. Xóa an toàn
        context.ImportLogs.Remove(importLog);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<ImportLog> MarkImportCompletedAsync(
        Guid importId,
        [Service] AppDbContext context)
    {
        // 1. Tìm phiếu kèm theo danh sách sản phẩm bên trong
        var importLog = await context.ImportLogs
            .Include(i => i.Details)
            .FirstOrDefaultAsync(i => i.Id == importId);

        if (importLog == null)
        {
            throw new GraphQLException("Không tìm thấy phiếu nhập này!");
        }

        if (importLog.Status == ImportStatus.Completed)
        {
            throw new GraphQLException("Phiếu này đã được chốt hoàn thành từ trước rồi!");
        }

        // 2. CHỐT SỔ: Duyệt qua từng món hàng để cộng kho và tính MAC
        foreach (var item in importLog.Details)
        {
            var product = await context.Products.FindAsync(item.ProductId);

            if (product == null)
            {
                throw new GraphQLException($"Sản phẩm có ID {item.ProductId} trong phiếu nhập không còn tồn tại trong hệ thống!");
            }

            // Tính giá MAC (Tái sử dụng lại Helper cực xịn của bạn)
            long newMacPrice = ImportHelper.CalculateNewMacPrice(
                currentMacPrice: product.ImportPrice ?? 0,
                currentStock: product.StockQuantity,
                quantityAdded: item.QuantityAdded,
                actualImportPrice: item.ActualImportPrice
            );

            // Cộng kho & Cập nhật giá
            product.StockQuantity = product.StockQuantity + item.QuantityAdded;
            product.AvailableStockQuantity = product.AvailableStockQuantity + item.QuantityAdded;
            product.ImportPrice = newMacPrice;
            product.UpdatedAt = DateTime.UtcNow;
        }

        // 3. Đổi trạng thái phiếu thành Hoàn tất
        importLog.Status = ImportStatus.Completed;

        // Bỏ cờ AutoSave đi (cho chắc cú, đề phòng data cũ bị dính)
        importLog.IsAutoSaved = false;

        await context.SaveChangesAsync();

        return importLog;
    }
}
