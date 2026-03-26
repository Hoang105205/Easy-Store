using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UI.Services.CategoryService;
using UI.Services.ProductService;
using Windows.UI.Notifications;

namespace UI.ViewModels;

public partial class CreateProductViewModel : ObservableObject
{
    private readonly ProductService _productService;

    private readonly CategoryService _categoryService;

    private readonly DispatcherQueue _dispatcherQueue;

    public ObservableCollection<CategoryModel> Categories { get; } = new();
    public ObservableCollection<string> SelectedImages { get; } = new();

    [ObservableProperty] private string sku = string.Empty; 

    [ObservableProperty] private string productName = string.Empty;

    [ObservableProperty] private int? minimumStock = null;

    [ObservableProperty] private CategoryModel? selectedCategory;

    public Action? GoBackAction { get; set; }
    public Func<string, string, Task>? ShowAlertAction { get; set; }
    public Func<string, string, Task<bool>>? ShowConfirmAction { get; set; }

    public CreateProductViewModel()
    {
        _productService = App.Current.Services.GetRequiredService<ProductService>();
        _categoryService = App.Current.Services.GetRequiredService<CategoryService>();
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

    public (bool, string) ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(Sku)) return (false, "Vui lòng nhập mã SKU.");
        if (string.IsNullOrWhiteSpace(ProductName)) return (false, "Vui lòng nhập tên sản phẩm.");
        if (MinimumStock < 0)
        {
            return (false, "Vui lòng nhập số lượng tồn tối thiểu hợp lệ (số nguyên dương).");
        }
        if (SelectedCategory == null) return (false, "Vui lòng chọn danh mục.");
        if (SelectedImages.Count < 1) return (false, "Vui lòng chọn ít nhất 1 ảnh.");

        return (true, string.Empty);
    }

    [RelayCommand]
    public async Task SaveProduct()
    {
        try
        {
            var validation = ValidateForm();
            if (!validation.Item1)
            {
                if (ShowAlertAction != null) await ShowAlertAction("Lỗi", validation.Item2);
                return;
            }

            int minimumStockQuantity = MinimumStock ?? 0;

            var success = await _productService.CreateProductAsync(
                Sku,
                ProductName,
                SelectedCategory!.Id,
                minimumStockQuantity,
                new List<string>(SelectedImages)
            );

            if (success)
            {
                if (ShowAlertAction != null) await ShowAlertAction("Thành công", "Sản phẩm được tạo mới thành công");
                GoBackAction?.Invoke();
            }
        }
        catch (Exception ex)
        {
            if (ShowAlertAction != null) await ShowAlertAction("Lỗi", ex.Message);
        }
    }

    [RelayCommand]
    public async Task Cancel()
    {
        if (ShowConfirmAction != null)
        {
            var result = await ShowConfirmAction("Xác nhận", "Bạn có chắc muốn hủy? Các thông tin đã nhập trước đó sẽ bị xóa.");
            if (result) GoBackAction?.Invoke();
        }
    }

    [RelayCommand]
    public async Task Reset()
    {
        if (ShowConfirmAction != null)
        {
            var result = await ShowConfirmAction("Xác nhận", "Bạn có muốn nhập lại? Các thông tin đã nhập trước đó sẽ bị xóa.");
            if (result) ResetForm();
        }
    }

    public void ResetForm()
    {
        ProductName = string.Empty;
        Sku = string.Empty;
        SelectedCategory = null;
        MinimumStock = null;
        SelectedImages.Clear();
    }

    [RelayCommand]
    public void RemoveImage(string imagePath)
    {
        if (SelectedImages.Contains(imagePath))
        {
            SelectedImages.Remove(imagePath);
        }
    }
}