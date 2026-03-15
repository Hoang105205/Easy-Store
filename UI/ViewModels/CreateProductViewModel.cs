using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UI.Services.ProductService;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UI.ViewModels;

public class CategoryMock
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

public partial class CreateProductViewModel : ObservableObject
{
    private readonly ProductService _productService;

    public ObservableCollection<CategoryMock> Categories { get; } = new();
    public ObservableCollection<string> SelectedImages { get; } = new();

    [ObservableProperty] private string sku = string.Empty; 

    [ObservableProperty] private string productName = string.Empty;

    [ObservableProperty] private CategoryMock? selectedCategory; 

    [ObservableProperty] private long importPrice = 0; 

    [ObservableProperty] private int importQuantity = 0; 

    [ObservableProperty] private long salePrice = 0; 

    public CreateProductViewModel()
    {
        _productService = new ProductService();
        LoadMockCategories();
    }

    private void LoadMockCategories()
    {
        Categories.Add(new CategoryMock { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Nước ngọt" });
        Categories.Add(new CategoryMock { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Bia & Đồ có cồn" });
        Categories.Add(new CategoryMock { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Bánh kẹo" });
        Categories.Add(new CategoryMock { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Mì ăn liền" });
        Categories.Add(new CategoryMock { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Gia vị" });
    }

    // Trả về Tuple (IsSuccess, ErrorMessage)
    public (bool, string) ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(Sku)) return (false, "Vui lòng nhập mã SKU.");
        if (string.IsNullOrWhiteSpace(ProductName)) return (false, "Vui lòng nhập tên sản phẩm.");
        if (SelectedCategory == null) return (false, "Vui lòng chọn danh mục.");
        if (SelectedImages.Count < 1) return (false, "Vui lòng chọn ít nhất 1 ảnh.");

        // Cặp điều kiện Nhập hàng
        if ((ImportPrice > 0 && ImportQuantity <= 0) || (ImportQuantity > 0 && ImportPrice <= 0))
        {
            return (false, "Giá nhập và Số lượng nhập phải được nhập cùng lúc.");
        }

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
            ImportPrice,
            SalePrice,
            ImportQuantity,
            new List<string>(SelectedImages)
        );
    }

    public void ResetForm()
    {
        ProductName = string.Empty;
        Sku = string.Empty;
        SelectedCategory = null;
        ImportPrice = 0;
        ImportQuantity = 0;
        SalePrice = 0;
        SelectedImages.Clear();
    }
}