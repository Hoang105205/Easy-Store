using Microsoft.Extensions.DependencyInjection;
using StrawberryShake;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UI.Services.ImportService;

public class ImportService
{
    private readonly IEasyStoreClient _client;

    public ImportService()
    {
        _client = App.Current.Services.GetRequiredService<IEasyStoreClient>();
    }

    // 1. Dành cho CreateImportViewModel (Lưu, Lấy nháp, Xóa)
    public async Task<IOperationResult<IGetActiveAutoSaveResult>> GetActiveAutoSaveAsync()
    {
        return await _client.GetActiveAutoSave.ExecuteAsync();
    }

    public async Task<IOperationResult<ICompleteImportLogResult>> CompleteImportLogAsync(CompleteImportInput input)
    {
        return await _client.CompleteImportLog.ExecuteAsync(input);
    }

    public async Task<IOperationResult<IDeleteImportResult>> DeleteImportAsync(Guid id)
    {
        return await _client.DeleteImport.ExecuteAsync(id);
    }

    // 2. Dành cho ImportViewModel (Danh sách, Summary)
    public async Task<IOperationResult<IGetImportHistoryResult>> GetImportHistoryAsync(
        int first,
        string? after,
        string? searchKeyword,
        DateTime? fromDate,
        DateTime? toDate,
        ImportStatus? status,
        string? sortColumn = "CreatedAt",
        bool isAscending = false
    )
    {
        var sortInput = new ImportLogSortInput();
        var sortDirection = isAscending ? SortEnumType.Asc : SortEnumType.Desc;

        switch (sortColumn)
        {
            case "Id":
                sortInput.Id = sortDirection;
                break;
            case "TotalAmount":
                sortInput.TotalAmount = sortDirection;
                break;
            case "Status":
                sortInput.Status = sortDirection;
                break;
            case "CreatedAt":
            default:
                sortInput.CreatedAt = sortDirection;
                break;
        }

        var orderList = new List<ImportLogSortInput> { sortInput };

        return await _client.GetImportHistory.ExecuteAsync(
            first,
            after,
            searchKeyword,
            fromDate,
            toDate,
            status,
            orderList
        );
    }

    public async Task<IOperationResult<IGetImportSummaryResult>> GetImportSummaryAsync()
    {
        return await _client.GetImportSummary.ExecuteAsync();
    }

    // 3. Dành cho ImportEditorViewModel (Chi tiết, Chốt phiếu)
    public async Task<IOperationResult<IGetImportByIdResult>> GetImportByIdAsync(Guid id)
    {
        return await _client.GetImportById.ExecuteAsync(id);
    }

    public async Task<IOperationResult<IMarkImportCompletedResult>> MarkImportCompletedAsync(Guid id)
    {
        return await _client.MarkImportCompleted.ExecuteAsync(id);
    }
}
