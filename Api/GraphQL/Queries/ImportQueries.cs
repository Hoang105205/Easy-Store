using Core.Data;
using Core.Models;
using HotChocolate.Execution.Processing;
using Microsoft.EntityFrameworkCore;

namespace Api.GraphQL.Queries;

public class ImportSummary
{
    public int TotalCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int DraftCount { get; set; }
}

[ExtendObjectType(Name = "Query")]
public class ImportQueries
{
    public async Task<ImportLog?> GetActiveAutoSaveAsync(
        [Service] AppDbContext context)
    {
        var autoSaveLog = await context.ImportLogs
            .Include(i => i.Details)
            .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(i => i.IsAutoSaved == true);

        return autoSaveLog;
    }

    [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20, MaxPageSize = 20)]
    public IQueryable<ImportLog> GetImportHistory(
        [Service] AppDbContext context,
        string? searchKeyword,
        DateTime? fromDate,
        DateTime? toDate,
        ImportStatus? status)
    {
        // 1. Phải Include Details và Product để có data tìm kiếm theo SKU
        var query = context.ImportLogs
            .Include(i => i.Details)
                .ThenInclude(d => d.Product)
            .Where(i => i.IsAutoSaved == false)
            .AsQueryable();

        // 2. Lọc theo Keyword (Mã phiếu hoặc Mã SKU)
        if (!string.IsNullOrEmpty(searchKeyword))
        {
            var keyword = searchKeyword.Trim().ToLower();

            query = query.Where(i =>
                i.Id.ToString().ToLower().Contains(keyword) ||
                i.Details.Any(d => d.Product!.SKU.ToLower().Contains(keyword))
            );
        }

        // 3. Lọc theo Ngày (Lấy từ đầu ngày FromDate)
        if (fromDate.HasValue)
        {
            var startOfDay = fromDate.Value.Date;
            query = query.Where(i => i.CreatedAt >= startOfDay);
        }

        // 4. Lọc theo Ngày (Lấy đến cuối ngày ToDate)
        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(i => i.CreatedAt <= endOfDay);
        }

        // 5. Lọc theo Trạng thái
        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        return query.OrderByDescending(i => i.CreatedAt);
    }

    public async Task<ImportLog?> GetImportByIdAsync(
        Guid id,
        [Service] AppDbContext context)
    {
        return await context.ImportLogs
            .Include(i => i.Details)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<ImportSummary> GetImportSummaryAsync([Service] AppDbContext context)
    {
        // Lọc bỏ những phiếu AutoSave ngầm định
        var query = context.ImportLogs.Where(x => x.IsAutoSaved == false);

        var totalCount = await query.CountAsync();
        var totalAmount = await query.Where(x => x.Status == ImportStatus.Completed).SumAsync(x => x.TotalAmount);
        var draftCount = await query.Where(x => x.Status == ImportStatus.Draft).CountAsync();

        return new ImportSummary
        {
            TotalCount = totalCount,
            TotalAmount = totalAmount,
            DraftCount = draftCount
        };
    }
}
