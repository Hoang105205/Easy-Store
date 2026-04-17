using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenDonut.Data.Cursors;
using Microsoft.UI.Xaml.Controls;
using StrawberryShake;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UI.Services.ExcelService;
using UI.Services.ImportService;
using UI.Views.Import;
using Windows.Storage;

namespace UI.ViewModels.Import;

public partial class ImportViewModel : ObservableObject
{
    private readonly ImportService _importService;

    private readonly ExcelService _excelService;

    public Action<StorageFile>? NavigateToCreateImportAction { get; set; }

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

    // === CÁC BIẾN CHO BỘ LỌC ===
    [ObservableProperty]
    private string searchKeyword = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? fromDate = null;

    [ObservableProperty]
    private DateTimeOffset? toDate = null;

    // Danh sách các trạng thái để hiển thị lên ComboBox
    public List<string> StatusOptions { get; } = new() { "Tất cả", "Hoàn thành", "Phiếu tạm" };

    [ObservableProperty]
    private string selectedStatus = "Tất cả";

    [ObservableProperty]
    private string filteredCountText = "0";

    // Danh sách bind thẳng ra DataGrid
    // Lưu ý: IGetImportHistory_ImportHistory_Nodes là interface do Strawberry Shake tự gen ra
    public ObservableCollection<IGetImportHistory_ImportHistory_Nodes> ImportLogs { get; } = new();

    // Thuật toán tiến/lùi trang: Dùng Stack để lưu lại các điểm neo (Cursor)
    private readonly Stack<string?> _cursorHistory = new();
    private string? _currentCursor = null;
    private string? _nextCursor = null;

    [ObservableProperty] private string activeSortColumn;

    [ObservableProperty]
    private bool isAscending = true;

    public ImportViewModel(ImportService importService, ExcelService excelService)
    {
        _importService = importService;
        _excelService = excelService;

        ActiveSortColumn = "CreatedAt";
        IsAscending = false;

        _ = LoadDataAsync(null);
    }

    // === LỆNH BẤM NÚT LỌC ===
    [RelayCommand]
    private async Task ApplyFilterAsync()
    {
        _cursorHistory.Clear();
        HasPreviousPage = false;
        _currentCursor = null;

        await LoadDataAsync(null);
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
            Debug.WriteLine($"Lỗi tải lịch sử nhập hàng: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadImportLogs(string? cursor = null)
    {
        var localSettings = ApplicationData.Current.LocalSettings;
        int itemsPerPage = localSettings.Values["ItemsPerPage"] as int? ?? 10;

        // === BƯỚC 1: CHUẨN BỊ DỮ LIỆU LỌC TỪ UI ===
        ImportStatus? apiStatus = SelectedStatus switch
        {
            "Hoàn thành" => ImportStatus.Completed,
            "Phiếu tạm" => ImportStatus.Draft,
            _ => null
        };

        DateTime? apiFromDate = FromDate?.DateTime;
        DateTime? apiToDate = ToDate?.DateTime;

        var result = await _importService.GetImportHistoryAsync(
            first: itemsPerPage,
            after: cursor,
            searchKeyword: SearchKeyword, 
            fromDate: apiFromDate,
            toDate: apiToDate,
            status: apiStatus,
            sortColumn: ActiveSortColumn,
            isAscending: IsAscending
        );

        if (result.IsSuccessResult() && result.Data?.ImportHistory != null)
        {
            var history = result.Data.ImportHistory;

            ImportLogs.Clear();
            if (history.Nodes != null)
            {
                foreach (var node in history.Nodes)
                {
                    ImportLogs.Add(node);
                }
            }

            FilteredCountText = $"{history.TotalCount}";
            HasNextPage = history.PageInfo.HasNextPage;
            _nextCursor = history.PageInfo.EndCursor;

            HasPreviousPage = _cursorHistory.Count > 0;
        }
    }

    private async Task LoadImportSummary()
    {
        var summaryResult = await _importService.GetImportSummaryAsync();
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

    [RelayCommand]
    private async Task DownloadTemplateAsync()
    {
        try
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads,
                SuggestedFileName = "MauNhapHang_EasyStore"
            };
            savePicker.FileTypeChoices.Add("Excel Workbook", new List<string>() { ".xlsx" });

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.AppMainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                using (var stream = await file.OpenStreamForWriteAsync())
                {
                    // Gọi Service dùng chung
                    _excelService.GenerateImportTemplate(stream);
                }
                Debug.WriteLine("Tải file mẫu thành công từ trang List!");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Lỗi: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task UploadExcelAsync()
    {
        try
        {
            // 1. Khởi tạo cửa sổ chọn file (Chỉ cho phép chọn file .xlsx)
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
            openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            openPicker.FileTypeFilter.Add(".xlsx");

            // 2. Cấp "Căn cước" (HWND) cho cửa sổ Picker hoạt động trong WinUI 3
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.AppMainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);

            // 3. Chờ người dùng chọn file
            StorageFile file = await openPicker.PickSingleFileAsync();

            if (file != null)
            {
                Debug.WriteLine($"Đã chọn file: {file.Name}");

                NavigateToCreateImportAction?.Invoke(file);
            }
            else
            {
                Debug.WriteLine("Người dùng đã hủy chọn file.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Lỗi chọn file Excel: {ex.Message}");
        }
    }


    [RelayCommand]
    private async Task SortAsync(string columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return;

        if (ActiveSortColumn == columnName)
        {
            IsAscending = !IsAscending;
        }
        else
        {
            ActiveSortColumn = columnName;
            IsAscending = true;
        }

        _cursorHistory.Clear();
        HasPreviousPage = false;
        _currentCursor = null;

        await LoadImportLogs();
    }
}
