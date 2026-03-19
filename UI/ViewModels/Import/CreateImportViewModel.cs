using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using StrawberryShake;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Services.ProductService;

namespace UI.ViewModels.Import;

public partial class CreateImportViewModel : ObservableObject
{
    private readonly ProductService _productService;

    private readonly IEasyStoreClient _client;

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

    // === DANH SÁCH BINDING RA UI ===
    public ObservableCollection<ProductModel> SearchResults { get; } = new();
    public ObservableCollection<ImportItemModel> SelectedItems { get; } = new();

    private Guid? _currentAutoSaveId = null;

    public Action? GoBackAction { get; set; }

    public CreateImportViewModel(ProductService productService, IEasyStoreClient client)
    {
        _productService = productService;
        _client = client;
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
            var result = await _productService.GetProductsAsync(10, null, keyword, null);

            _dispatcherQueue.TryEnqueue(() =>
            {
                SearchResults.Clear();
                foreach (var item in result.Products)
                {
                    SearchResults.Add(item);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi tìm kiếm: {ex.Message}");
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
        if (SelectedItems.Count == 0) return;

        _dispatcherQueue.TryEnqueue(() => {
            AutoSaveText = "Đang lưu tạm..."; 
            AutoSaveIcon = "\uE895"; 
            AutoSaveColor = "DarkOrange";
        });

        var success = await ExecuteSaveAsync(isDraft: false, isAutoSave: true);

        _dispatcherQueue.TryEnqueue(() => {
            if (success)
            {
                AutoSaveText = $"Đã lưu ({DateTime.Now.ToString("HH:mm:ss")})";
                AutoSaveIcon = "\uE73E"; 
                AutoSaveColor = "SeaGreen";
            }
            else
            {
                AutoSaveText = "Lỗi khi lưu!"; 
                AutoSaveIcon = "\uEA39"; 
                AutoSaveColor = "Red";
            }
        });
    }

    // LUỒNG 2: CHỐT PHIẾU TẠM (isDraft = true, isAutoSave = false -> Backend ra Draft & AutoSaved = false)
    [RelayCommand]
    private async Task SaveDraftAsync()
    {
        var success = await ExecuteSaveAsync(isDraft: true, isAutoSave: false);
        if (success)
        {
            _dispatcherQueue.TryEnqueue(() => {
                GoBackAction?.Invoke();


                Debug.WriteLine("Đã lưu Phiếu Tạm thành công, về trang chủ thôi!");
            });
        }
    }

    [RelayCommand]
    private async Task CompleteImportAsync()
    {
        var success = await ExecuteSaveAsync(isDraft: false, isAutoSave: false);
        if (success)
        {
            _dispatcherQueue.TryEnqueue(() => {
                GoBackAction?.Invoke();

                Debug.WriteLine("Đã chốt sổ và cộng kho, về trang chủ thôi!");
            });
        }
    }

    public async Task LoadExistingAutoSaveAsync()
    {
        try
        {
            var result = await _client.GetActiveAutoSave.ExecuteAsync();

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

    private async Task<bool> ExecuteSaveAsync(bool isDraft, bool isAutoSave)
    {
        if (SelectedItems.Count == 0) return false;

        try
        {
            var detailsInput = SelectedItems.Select(x => new ImportLogDetailInput
            {
                ProductId = x.ProductId,
                QuantityAdded = (int) x.Quantity,
                ActualImportPrice = (long) x.ImportPrice
            }).ToList();

            var input = new CompleteImportInput
            {
                Details = detailsInput,
                IsDraft = isDraft,
                IsAutoSave = isAutoSave
            };

            // 2. Gọi 1 Mutation duy nhất cho tất cả các trường hợp!
            var result = await _client.CompleteImportLog.ExecuteAsync(input);

            return result.IsSuccessResult();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi gọi API lưu: {ex.Message}");
            return false;
        }
    }
}