using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenDonut.Data.Cursors;
using StrawberryShake;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace UI.ViewModels.Import;

public partial class ImportViewModel : ObservableObject
{
    // Dependency Injection: Client GraphQL do Strawberry Shake tự sinh ra
    private readonly IEasyStoreClient _client;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string totalCountText = "0";

    [ObservableProperty]
    private string totalAmountText = "0 VNĐ";

    [ObservableProperty]
    private string draftCountText = "0";

    [ObservableProperty]
    private bool hasNextPage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadPreviousPageCommand))]
    private bool hasPreviousPage;

    // Danh sách bind thẳng ra DataGrid
    // Lưu ý: IGetImportHistory_ImportHistory_Nodes là interface do Strawberry Shake tự gen ra
    public ObservableCollection<IGetImportHistory_ImportHistory_Nodes> ImportLogs { get; } = new();

    // Thuật toán tiến/lùi trang: Dùng Stack để lưu lại các điểm neo (Cursor)
    private readonly Stack<string?> _cursorHistory = new();
    private string? _currentCursor = null;
    private string? _nextCursor = null;

    public ImportViewModel(IEasyStoreClient client)
    {
        _client = client;
        _ = LoadDataAsync(null); 
    }

    public async Task LoadDataAsync(string? cursor)
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            await LoadImportLogs(cursor);

            await LoadImportSummary();
        }
        catch (Exception ex)
        {
            // TODO: Hiển thị dialog báo lỗi mạng
            System.Diagnostics.Debug.WriteLine($"Lỗi tải lịch sử nhập hàng: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadImportLogs(string? cursor)
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        int itemsPerPage = localSettings.Values["ItemsPerPage"] as int? ?? 10;

        // Gọi hàm GraphQL đã định nghĩa trong file Import.graphql
        var result = await _client.GetImportHistory.ExecuteAsync(first: itemsPerPage, after: cursor);

        // result.IsSuccessResult() là hàm có sẵn của Strawberry Shake
        if (result.IsSuccessResult() && result.Data?.ImportHistory != null)
        {
            var history = result.Data.ImportHistory;

            // 1. Clear bảng cũ và đổ dữ liệu mới vào
            ImportLogs.Clear();
            if (history.Nodes != null)
            {
                foreach (var node in history.Nodes)
                {
                    ImportLogs.Add(node);
                }
            }

            // 2. Cập nhật thanh trạng thái phân trang
            TotalCountText = $"{history.TotalCount}";
            HasNextPage = history.PageInfo.HasNextPage;
            _nextCursor = history.PageInfo.EndCursor;

            // Nếu lịch sử neo > 0 nghĩa là ta đang ở trang 2 trở đi -> Cho phép lùi
            HasPreviousPage = _cursorHistory.Count > 0;
        }
    }

    private async Task LoadImportSummary()
    {
        var summaryResult = await _client.GetImportSummary.ExecuteAsync();
        if (summaryResult.IsSuccessResult() && summaryResult.Data?.ImportSummary != null)
        {
            var summary = summaryResult.Data.ImportSummary;

            // Gán thẳng số vào thẻ
            TotalCountText = summary.TotalCount.ToString();

            // Format tiền
            TotalAmountText = summary.TotalAmount.ToString("N0") + " VNĐ";

            DraftCountText = summary.DraftCount.ToString();
        }
    }

    [RelayCommand]
    private async Task LoadNextPageAsync()
    {
        if (HasNextPage)
        {
            _cursorHistory.Push(_currentCursor); // Cất con trỏ trang hiện tại vào kho để lát có thể lùi về
            _currentCursor = _nextCursor;         // Trỏ tới trang tiếp theo
            await LoadDataAsync(_currentCursor);
        }
    }

    [RelayCommand]
    private async Task LoadPreviousPageAsync()
    {
        if (_cursorHistory.Count > 0)
        {
            _currentCursor = _cursorHistory.Pop(); // Lấy con trỏ trang trước đó ra
            await LoadDataAsync(_currentCursor);
        }
    }
}
