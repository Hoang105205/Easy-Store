using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UI.Services.ProductService;
using CommunityToolkit.Mvvm.ComponentModel;
using UI.Services.CategoryService;
using Windows.UI.Notifications;
using System.Diagnostics;

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

    [ObservableProperty] private CategoryModel? selectedCategory;  

    public CreateProductViewModel()
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

    // Trả về Tuple (IsSuccess, ErrorMessage)
    public (bool, string) ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(Sku)) return (false, "Vui lòng nhập mã SKU.");
        if (string.IsNullOrWhiteSpace(ProductName)) return (false, "Vui lòng nhập tên sản phẩm.");
        if (SelectedCategory == null) return (false, "Vui lòng chọn danh mục.");
        if (SelectedImages.Count < 1) return (false, "Vui lòng chọn ít nhất 1 ảnh.");

        return (true, string.Empty);
    }

    public async Task<bool> SaveProductAsync()
    {
        var validation = ValidateForm();
        if (!validation.Item1) throw new Exception(validation.Item2);

        return await _productService.CreateProductAsync(
            Sku,
            ProductName,
            SelectedCategory!.Id,
            new List<string>(SelectedImages)
        );
    }

    public void ResetForm()
    {
        ProductName = string.Empty;
        Sku = string.Empty;
        SelectedCategory = null;
        SelectedImages.Clear();
    }
}