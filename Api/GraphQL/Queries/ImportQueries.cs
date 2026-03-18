using Core.Data;
using Core.Models;
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

    [UsePaging(IncludeTotalCount = true, DefaultPageSize = 20)]
    public IQueryable<ImportLog> GetImportHistory([Service] AppDbContext context)
    {
        return context.ImportLogs
                .Include(i => i.Details)
                    .ThenInclude(d => d.Product)
            .Where(i => i.IsAutoSaved == false)
            .OrderByDescending(i => i.CreatedAt);
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
