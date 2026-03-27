using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StrawberryShake;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Services.ImportService;
using UI.Services.ProductService;
using UI.ViewModels.Product;
using Windows.Storage;

namespace UI.ViewModels.Import;

public partial class CreateImportViewModel : ObservableObject
{
    private record RawExcelRow(string Sku, string Quantity, string Price);

    private readonly ProductService _productService;

    private readonly ImportService _importService;

    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    private string searchKeyword = string.Empty;

    // === CÁC BIẾN CHO KHU VỰC AUTO-SAVE ===
    [ObservableProperty]
    private string autoSaveText = "Sẵn sàng";

    [ObservableProperty]
    private string autoSaveIcon = "\uE73E"; // Mã icon Dấu Check

    [ObservableProperty]
    private string autoSaveColor = "Gray";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProcessingVisibility))]
    private bool isProcessingExcel;

    public Visibility ProcessingVisibility =>
        IsProcessingExcel ? Visibility.Visible : Visibility.Collapsed;

    // === DANH SÁCH BINDING RA UI ===
    public ObservableCollection<ProductModel> SearchResults { get; } = new();
    public ObservableCollection<ImportItemModel> SelectedItems { get; } = new();

    private Guid? _currentAutoSaveId = null;

    public Action? GoBackAction { get; set; }

    public CreateImportViewModel(ProductService productService, ImportService importService)
    {
        _productService = productService;
        _importService = importService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    // TÍNH NĂNG TÌM KIẾM: Tự động chạy mỗi khi biến SearchKeyword thay đổi
    partial void OnSearchKeywordChanged(string value)
    {
        SearchProductsAsync(value);
    }

    private async void SearchProductsAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            _dispatcherQueue.TryEnqueue(() => SearchResults.Clear()); 
            return;
        }

        try
        {
            var result = await _productService.GetProductsAsync(keyword, null);

            _dispatcherQueue.TryEnqueue(() =>
            {
                SearchResults.Clear();
                foreach (var item in result)
                {
                    SearchResults.Add(item);
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Lỗi tìm kiếm: {ex.Message}");
        }
    }

    // === CÁC LỆNH (COMMAND) CHO NÚT BẤM ===

    [RelayCommand]
    private void AddProduct(ProductModel product)
    {
        if (product == null) return;

        // Kiểm tra xem sản phẩm đã có trong phiếu chưa
        var existingItem = SelectedItems.FirstOrDefault(x => x.ProductId == product.Id);
        if (existingItem != null)
        {
            existingItem.Quantity++; // Có rồi thì tăng số lượng
        }
        else
        {
            // Chưa có thì thêm dòng mới
            SelectedItems.Add(new ImportItemModel
            {
                ProductId = product.Id,
                Name = product.Name ?? "Sản phẩm không tên",
                Sku = product.Sku ?? "N/A",
                ImportPrice = 0
            });
        }

        // Cứ thêm hàng là kích hoạt Auto-save ngầm
        _ = TriggerAutoSaveAsync();
    }

    [RelayCommand]
    private void RemoveProduct(ImportItemModel item)
    {
        if (item != null)
        {
            Debug.WriteLine("Xóa sản phẩm: " + item.Name);
            SelectedItems.Remove(item);
            _ = TriggerAutoSaveAsync();
        }
    }

    // === LOGIC LƯU NGẦM (AUTO-SAVE) ===
    // LUỒNG 1: LƯU NGẦM (isDraft = false, isAutoSave = true -> Backend ra Draft & AutoSaved = true)
    public async Task TriggerAutoSaveAsync()
    {
        var validItems = GetValidItems();
        if (validItems.Count == 0)
        {
            _dispatcherQueue.TryEnqueue(() => {
                AutoSaveText = "Không có sản phẩm hợp lệ để lưu";
                AutoSaveIcon = "\uE783"; // Icon cảnh báo
                AutoSaveColor = "DarkOrange";
            });
            return;
        }

        _dispatcherQueue.TryEnqueue(() => {
            AutoSaveText = "Đang lưu tạm..."; 
            AutoSaveIcon = "\uE895"; 
            AutoSaveColor = "DarkOrange";
        });

        var (success, errorMessage) = await ExecuteSaveAsync(isDraft: false, isAutoSave: true);

        _dispatcherQueue.TryEnqueue(() => {
            if (success)
            {
                AutoSaveText = $"Đã lưu ({DateTime.Now.ToString("HH:mm:ss")})";
                AutoSaveIcon = "\uE73E";
                AutoSaveColor = "SeaGreen";
            }
            else
            {
                AutoSaveText = errorMessage;
                AutoSaveIcon = "\uEA39";
                AutoSaveColor = "Red";
            }
        });
    }

    // LUỒNG 2: CHỐT PHIẾU TẠM (isDraft = true, isAutoSave = false -> Backend ra Draft & AutoSaved = false)
    [RelayCommand]
    private async Task SaveDraftAsync()
    {
        var (success, errorMessage) = await ExecuteSaveAsync(isDraft: true, isAutoSave: false);

        if (success)
        {
            _dispatcherQueue.TryEnqueue(() => {
                GoBackAction?.Invoke();
                Debug.WriteLine("Đã lưu Phiếu Tạm thành công, về trang chủ thôi!");
            });
        }
        else
        {
            // HIỆN DIALOG BÁO LỖI CHO NGƯỜI DÙNG
            _dispatcherQueue.TryEnqueue(async () => {
                var errorDialog = new ContentDialog
                {
                    Title = "Không thể tạo phiếu tạm",
                    Content = errorMessage,
                    CloseButtonText = "Đã hiểu",
                    XamlRoot = App.Current!.AppMainWindow!.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
            });
        }
    }

    [RelayCommand]
    private async Task CompleteImportAsync()
    {
        var (success, errorMessage) = await ExecuteSaveAsync(isDraft: false, isAutoSave: false);

        if (success)
        {
            _dispatcherQueue.TryEnqueue(() => {
                GoBackAction?.Invoke();
                Debug.WriteLine("Đã chốt sổ và cộng kho, về trang chủ thôi!");
            });
        }
        else
        {
            // HIỆN DIALOG BÁO LỖI CHO NGƯỜI DÙNG
            _dispatcherQueue.TryEnqueue(async () => {
                var errorDialog = new ContentDialog
                {
                    Title = "Không thể chốt phiếu",
                    Content = errorMessage,
                    CloseButtonText = "Đã hiểu",
                    XamlRoot = App.Current!.AppMainWindow!.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
            });
        }
    }

    public async Task LoadExistingAutoSaveAsync()
    {
        try
        {
            var result = await _importService.GetActiveAutoSaveAsync();

            if (result.IsSuccessResult() && result.Data?.ActiveAutoSave != null)
            {
                var autoSavedDoc = result.Data.ActiveAutoSave;
                _currentAutoSaveId = autoSavedDoc.Id;

                _dispatcherQueue.TryEnqueue(() =>
                {
                    SelectedItems.Clear();
                    foreach (var detail in autoSavedDoc.Details)
                    {
                        SelectedItems.Add(new ImportItemModel
                        {
                            ProductId = detail.ProductId,
                            Name = detail!.Product!.Name ?? "Không xác định",
                            Quantity = detail.QuantityAdded,
                            Sku = detail.Product.Sku ?? "N/A",
                            ImportPrice = detail.ActualImportPrice
                        });
                    }

                    AutoSaveText = "Đã khôi phục phiên gõ dở";
                    AutoSaveColor = "SeaGreen";
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Lỗi tải Auto-save: {ex.Message}");
        }
    }

    private async Task<(bool IsSuccess, string ErrorMessage)> ExecuteSaveAsync(bool isDraft, bool isAutoSave)
    {
        // 1. Dùng module lọc hàng sạch
        var validItems = GetValidItems();

        if (validItems.Count == 0)
        {
            return (false, "Không có sản phẩm nào hợp lệ để lưu.");
        }

        try
        {
            // 2. Dùng module ánh xạ dữ liệu
            var detailsInput = MapToApiInput(validItems);

            var input = new CompleteImportInput
            {
                Details = detailsInput,
                IsDraft = isDraft,
                IsAutoSave = isAutoSave
            };

            // 3. Gửi xuống Backend
            var result = await _importService.CompleteImportLogAsync(input);

            if (result.IsErrorResult())
            {
                var firstError = result.Errors?.FirstOrDefault();
                string backendError = firstError?.Message ?? "Lỗi không xác định từ máy chủ.";

                Debug.WriteLine($"GraphQL Error: {backendError}");
                return (false, backendError); 
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Lỗi gọi API lưu: {ex.Message}");
            return (false, "Lỗi kết nối mạng. Vui lòng thử lại!");
        }
    }

    // Module 1: Chuyên lọc ra các dòng "Sạch" (Không có lỗi và có ProductId thật)
    private List<ImportItemModel> GetValidItems()
    {
        return SelectedItems
            .Where(x => !x.HasError && x.ProductId != Guid.Empty)
            .ToList();
    }

    // Module 2: Chuyên chuyển đổi (Map) từ Model của UI sang Model của GraphQL API
    private List<ImportLogDetailInput> MapToApiInput(List<ImportItemModel> validItems)
    {
        return validItems.Select(x => new ImportLogDetailInput
        {
            ProductId = x.ProductId,
            QuantityAdded = (int)x.Quantity,
            ActualImportPrice = (long)x.ImportPrice
        }).ToList();
    }

    public async Task ProcessImportedExcelAsync(StorageFile file)
    {
        IsProcessingExcel = true;

        try
        {
            // BƯỚC 0: DỌN DẸP BẢN NHÁP CŨ (CLEAN UP)
            await LoadExistingAutoSaveAsync();

            if (_currentAutoSaveId.HasValue && _currentAutoSaveId.Value != Guid.Empty)
            {
                await _importService.DeleteImportAsync(_currentAutoSaveId.Value);

                _currentAutoSaveId = null;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                SelectedItems.Clear();
            });
            // ==========================================


            // Bước 1: Trích xuất dữ liệu thô
            var rawRows = await ExtractDataFromExcelAsync(file);

            var tempList = new List<ImportItemModel>();

            // Bước 2: Validate từng dòng và lấy kết quả
            foreach (var rawRow in rawRows)
            {
                var validatedItem = await ValidateAndMapRowAsync(rawRow);

                if (validatedItem.HasError || validatedItem.ProductId == Guid.Empty)
                {
                    tempList.Add(validatedItem);
                    continue;
                }

                var existingItem = tempList.FirstOrDefault(x => x.ProductId == validatedItem.ProductId);

                if (existingItem != null)
                {
                    // 1. Tính tổng tiền hiện tại của cả 2 dòng (trước khi cộng dồn số lượng)
                    double totalValue = (existingItem.Quantity * existingItem.ImportPrice)
                                      + (validatedItem.Quantity * validatedItem.ImportPrice);

                    // 2. Cộng dồn số lượng
                    existingItem.Quantity += validatedItem.Quantity;

                    // 3. Tính giá nhập trung bình CÓ TRỌNG SỐ (Weighted Average)
                    existingItem.ImportPrice = totalValue / existingItem.Quantity;
                }
                else
                {
                    tempList.Add(validatedItem);
                }
            }

            // Bước 3: Đưa dữ liệu lên UI an toàn
            _dispatcherQueue.TryEnqueue(() =>
            {
                SelectedItems.Clear();
                foreach (var item in tempList)
                {
                    SelectedItems.Add(item);
                }

                _ = TriggerAutoSaveAsync();
            });

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Lỗi xử lý file Excel: {ex.Message}");
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsProcessingExcel = false;
            });
        }
    }

    private async Task<List<RawExcelRow>> ExtractDataFromExcelAsync(StorageFile file)
    {
        var rawData = new List<RawExcelRow>();
        using var stream = await file.OpenStreamForReadAsync();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);

        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            var skuInput = row.Cell(1).GetString().Trim();
            var qtyInput = row.Cell(2).GetString().Trim();
            var priceInput = row.Cell(3).GetString().Trim();

            // Bỏ qua dòng trống
            if (string.IsNullOrEmpty(skuInput) && string.IsNullOrEmpty(qtyInput) && string.IsNullOrEmpty(priceInput))
                continue;

            rawData.Add(new RawExcelRow(skuInput, qtyInput, priceInput));
        }

        return rawData;
    }

    private async Task<ImportItemModel> ValidateAndMapRowAsync(RawExcelRow rawRow)
    {
        var item = new ImportItemModel { Sku = rawRow.Sku };
        var errorList = new List<string>();

        // 1. KIỂM TRA SỐ LƯỢNG
        if (!int.TryParse(rawRow.Quantity, out int qty) || qty <= 0)
        {
            errorList.Add("Số lượng không hợp lệ");
            item.Quantity = 0;
        }
        else 
        { 
            item.Quantity = qty; 
        }

        // 2. KIỂM TRA GIÁ NHẬP
        if (!double.TryParse(rawRow.Price, out double price) || price < 0)
        {
            errorList.Add("Giá nhập không hợp lệ");
            item.ImportPrice = 0;
        }
        else 
        { 
            item.ImportPrice = price; 
        }

        // 3. KIỂM TRA SKU TỒN TẠI VÀ GỌI API
        if (string.IsNullOrEmpty(rawRow.Sku))
        {
            errorList.Add("Thiếu mã SKU");
        }
        else
        {
            var productResult = await _productService.GetProductBySkuAsync(rawRow.Sku);

            // Giả định productResult.IsErrorResult() là cách bạn check lỗi từ thư viện GraphQL
            if (productResult.Data?.ProductBySku == null)
            {
                errorList.Add("SKU không tồn tại");
                item.Name = "Sản phẩm không xác định";
            }
            else
            {
                item.Sku = productResult.Data.ProductBySku.Sku;
                item.ProductId = productResult.Data.ProductBySku.Id;
                item.Name = productResult.Data.ProductBySku.Name;
            }
        }

        // 4. CHỐT LỖI
        if (errorList.Any())
        {
            item.HasError = true;
            item.ErrorMessage = string.Join(" | ", errorList);
        }

        return item;
    }
}