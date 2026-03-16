using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UI.Services.ProductService;
using UI.Services.CategoryService;

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

    // dữ liệu cơ bản
    [ObservableProperty] private Guid productId;
    [ObservableProperty] private string sku = string.Empty;
    [ObservableProperty] private string productName = string.Empty;
    [ObservableProperty] private CategoryModel? selectedCategory;
    [ObservableProperty] private long importPrice = 0;
    [ObservableProperty] private int stockQuantity = 0;
    [ObservableProperty] private long salePrice = 0;
    [ObservableProperty] private string createdAtText = string.Empty;

    // ảnh
    [ObservableProperty] private string? mainImage = "ms-appx:///Assets/StoreLogo.png"; // Vì ảnh load lâu nên tạm thời set ảnh mặc định, sau khi load xong sẽ update lại
    public ObservableCollection<string> DisplayImages { get; } = new(); 
    public ObservableCollection<string> EditImages { get; } = new();    
    public ObservableCollection<CategoryModel> Categories { get; } = new();

    // Lưu lại bản nháp để Restore nếu bấm Hủy
    private IGetProductById_ProductById? _originalData;
    private readonly DispatcherQueue _dispatcherQueue;

    public ProductDetailViewModel()
    {
        _productService = new ProductService();
        _categoryService = new CategoryService();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public async Task LoadCategoriesAsync()
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
        ImportPrice = data.ImportPrice;
        SalePrice = data.SalePrice;
        StockQuantity = data.StockQuantity;
        CreatedAtText = data.CreatedAt.ToString("dd/MM/yyyy HH:mm");
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == data.CategoryId);

        // Map Ảnh
        DisplayImages.Clear();
        EditImages.Clear();
        if (data.Images != null)
        {
            foreach (var img in data.Images)
            {
                DisplayImages.Add(img.ImagePath);
                EditImages.Add(img.ImagePath);
            }
        }

        MainImage = DisplayImages.FirstOrDefault() ?? "ms-appx:///Assets/StoreLogo.png";
    }

    public void EnableEditMode()
    {
        PageTitle = "Chỉnh sửa sản phẩm";
        ViewVisibility = Visibility.Collapsed;
        EditVisibility = Visibility.Visible;
        IsReadOnly = false;
        IsEditable = true;
    }

    public void CancelEditMode()
    {
        PageTitle = "Chi tiết sản phẩm";
        ViewVisibility = Visibility.Visible;
        EditVisibility = Visibility.Collapsed;
        IsReadOnly = true;
        IsEditable = false;
        if (_originalData != null)
        {
            _dispatcherQueue.TryEnqueue(() => FillData(_originalData)); // Restore lại data cũ
        } 
    }

    public async Task<bool> SaveChangesAsync()
    {
        if (string.IsNullOrWhiteSpace(ProductName) || SelectedCategory == null)
            throw new Exception("Tên và Danh mục không được để trống!");

        var success = await _productService.UpdateProductAsync(ProductId, Sku, ProductName, SelectedCategory.Id, SalePrice, new List<string>(EditImages));
        if (success) await LoadDataAsync(ProductId); // Load lại data mới nhất từ DB
        return success;
    }

    public async Task<bool> DeleteAsync() => await _productService.DeleteProductAsync(ProductId);
}