using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UI.Services.CategoryService;
using UI.Services.ImageCacheService;
using UI.Services.ProductService;

namespace UI.ViewModels;

public partial class ProductDetailViewModel : ObservableObject
{
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;

    // trạng thái giao diện (View Mode hay Edit Mode)
    [ObservableProperty] private string pageTitle = "Chi tiết sản phẩm";
    [ObservableProperty] private Visibility viewVisibility = Visibility.Visible;
    [ObservableProperty] private Visibility editVisibility = Visibility.Collapsed;
    [ObservableProperty] private bool isReadOnly = true;  // Dùng cho TextBox
    [ObservableProperty] private bool isEditable = false; // Dùng cho ComboBox
    [ObservableProperty] private bool isBusy;

    // dữ liệu cơ bản
    [ObservableProperty] private Guid productId;
    [ObservableProperty] private string sku = string.Empty;
    [ObservableProperty] private string productName = string.Empty;
    [ObservableProperty] private CategoryModel? selectedCategory;
    [ObservableProperty] private long importPrice = 0;
    [ObservableProperty] private int stockQuantity = 0;
    [ObservableProperty] private long? salePrice = 0;
    [ObservableProperty] private int? minimumStockQuantity = 0;
    [ObservableProperty] private string createdAtText = string.Empty;

    // ảnh
    [ObservableProperty] private string? mainImage = "ms-appx:///Assets/StoreLogo.png"; // Vì ảnh load lâu nên tạm thời set ảnh mặc định, sau khi load xong sẽ update lại
    public ObservableCollection<string> DisplayImages { get; } = new(); 
    public ObservableCollection<string> EditImages { get; } = new();
    public ObservableCollection<string> OriginalImagesCollection { get; } = new();

    public ObservableCollection<CategoryModel> Categories { get; } = new();

    public Action? GoBackAction { get; set; }
    public Func<string, string, string, string, Task<bool>>? ShowConfirmAction { get; set; }
    public Func<string, string, Task>? ShowAlertAction { get; set; }

    // Lưu lại bản nháp để Restore nếu bấm Hủy
    private IGetProductById_ProductById? _originalData;
    private readonly DispatcherQueue _dispatcherQueue;

    public ProductDetailViewModel()
    {
        _productService = App.Current.Services.GetRequiredService<ProductService>();
        _categoryService = App.Current.Services.GetRequiredService<CategoryService>();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public async Task LoadCategoriesAsync()
    {
        try
        {
            var apiCategories = await _categoryService.GetCategoriesAsync();

            _dispatcherQueue.TryEnqueue(() =>
            {
                Categories.Clear();
                foreach (var cat in apiCategories)
                {
                    Categories.Add(cat);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi load categories: {ex.Message}");
        }
    }

    public async Task LoadDataAsync(Guid id)
    {
        try
        {
            _originalData = await _productService.GetProductByIdAsync(id);
            if (_originalData != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    FillData(_originalData);
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Lỗi load data: {ex.Message}"); }
    }

    private void FillData(IGetProductById_ProductById data)
    {
        ProductId = data.Id;
        Sku = data.Sku ?? string.Empty;
        ProductName = data.Name ?? string.Empty;
        ImportPrice = data.ImportPrice ?? 0;
        SalePrice = data.SalePrice ?? 0;
        MinimumStockQuantity = data.MinimumStockQuantity;
        StockQuantity = data.StockQuantity;
        CreatedAtText = data.CreatedAt.ToString("dd/MM/yyyy HH:mm");
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == data.CategoryId);

        DisplayImages.Clear();
        EditImages.Clear();
        OriginalImagesCollection.Clear();
        if (data.Images != null)
        {
            foreach (var img in data.Images)
            {
                DisplayImages.Add(img.ImagePath);
                EditImages.Add(img.ImagePath);
                OriginalImagesCollection.Add(img.ImagePath);
            }
        }

        MainImage = DisplayImages.FirstOrDefault() ?? "ms-appx:///Assets/StoreLogo.png";
    }

    [RelayCommand]
    public void EnableEditMode()
    {
        PageTitle = "Chỉnh sửa sản phẩm";
        ViewVisibility = Visibility.Collapsed;
        EditVisibility = Visibility.Visible;
        IsReadOnly = false;
        IsEditable = true;
    }

    [RelayCommand]
    public async Task CancelEditMode()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var newImagesToDiscard = EditImages
            .Except(OriginalImagesCollection, StringComparer.OrdinalIgnoreCase)
            .ToList();

            if (newImagesToDiscard.Any())
            {
                var deleteTasks = newImagesToDiscard.Select(async imgPath =>
                {
                    try
                    {
                        await ImageCacheService.DeleteImageAsync(imgPath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi xóa ảnh nháp {imgPath}: {ex.Message}");
                    }
                });

                await Task.WhenAll(deleteTasks);
            }

            ExitEditModeUiOnly();

            if (_originalData != null)
            {
                _dispatcherQueue.TryEnqueue(() => FillData(_originalData));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ExitEditModeUiOnly()
    {
        PageTitle = "Chi tiết sản phẩm";
        ViewVisibility = Visibility.Visible;
        EditVisibility = Visibility.Collapsed;
        IsReadOnly = true;
        IsEditable = false;
    }

    [RelayCommand]
    public async Task SaveChanges()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            if (ShowConfirmAction != null)
            {
                bool isConfirmed = await ShowConfirmAction("Xác nhận", "Bạn có chắc chắn muốn lưu các thay đổi này?", "Có", "Không");
                if (!isConfirmed) return;
            }

            if (string.IsNullOrWhiteSpace(ProductName) || SelectedCategory == null)
                throw new Exception("Tên và Danh mục không được để trống!");

            long salePriceValue = SalePrice ?? 0;
            int minimumStockQuantityValue = MinimumStockQuantity ?? 0;
            var success = await _productService.UpdateProductAsync(ProductId, Sku, ProductName, SelectedCategory.Id, salePriceValue, minimumStockQuantityValue, new List<string>(EditImages));

            if (success)
            {
                var imagesToDelete = OriginalImagesCollection.Except(EditImages, StringComparer.OrdinalIgnoreCase)
                                                             .ToList();

                var deleteTasks = imagesToDelete.Select(async imgPath =>
                {
                    try
                    {
                        await ImageCacheService.DeleteImageAsync(imgPath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi dọn dẹp ảnh {imgPath}: {ex.Message}");
                    }
                });

                await Task.WhenAll(deleteTasks);

                await LoadDataAsync(ProductId);
                ExitEditModeUiOnly();
                if (ShowAlertAction != null) await ShowAlertAction("Thành công", "Đã cập nhật sản phẩm thành công!");
            }
        }
        catch (Exception ex)
        {
            if (ShowAlertAction != null) await ShowAlertAction("Lỗi", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task Delete()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            if (ShowConfirmAction != null)
            {
                bool isConfirmed = await ShowConfirmAction("Cảnh báo nguy hiểm", $"Bạn có chắc chắn muốn xóa sản phẩm '{ProductName}' không? Hành động này không thể hoàn tác.", "Xóa", "Hủy");
                if (!isConfirmed) return;
            }

            await _productService.DeleteProductAsync(ProductId);

            var allImages = OriginalImagesCollection.Concat(EditImages)
                                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                                    .ToList();

            var deleteTasks = allImages.Select(async imgPath =>
            {
                try
                {
                    await ImageCacheService.DeleteImageAsync(imgPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi dọn dẹp ảnh {imgPath}: {ex.Message}");
                }
            });

            await Task.WhenAll(deleteTasks);

            if (ShowAlertAction != null)
            {
                await ShowAlertAction("Thành công", "Sản phẩm đã bị xóa.");
            }

            GoBackAction?.Invoke();
        }
        catch (Exception ex)
        {
            if (ShowAlertAction != null) await ShowAlertAction("Không thể xóa", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void RemoveImage(string imagePath)
    {
        if (EditImages.Contains(imagePath))
        {
            EditImages.Remove(imagePath);
        }
    }
}