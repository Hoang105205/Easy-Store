using Core.Data;
using Core.Models;
using HotChocolate;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.GraphQL.Mutations;

// DTO nhận dữ liệu từ Client (Đã thêm Sku)
public record CreateProductInput(
    string Sku,
    string Name,
    Guid CategoryId,
    long ImportPrice,
    long SalePrice,
    int StockQuantity,
    List<string> ImagePaths
);

[ExtendObjectType(typeof(Mutation))]
public class ProductMutation
{
    public async Task<Product> CreateProductAsync(
        CreateProductInput input,
        [Service] AppDbContext dbContext)
    {
        // Tạo sản phẩm mới
        var newProduct = new Product
        {
            Id = Guid.NewGuid(),
            SKU = input.Sku, // Lấy SKU từ người dùng nhập
            Name = input.Name,
            CategoryId = input.CategoryId,
            ImportPrice = input.ImportPrice,
            SalePrice = input.SalePrice,
            StockQuantity = input.StockQuantity,
            IsDraft = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Thêm ảnh
        foreach (var path in input.ImagePaths)
        {
            newProduct.Images.Add(new ProductImage
            {
                Id = Guid.NewGuid(),
                ImagePath = path,
                IsPrimary = input.ImagePaths.IndexOf(path) == 0
            });
        }

        dbContext.Products.Add(newProduct);
        await dbContext.SaveChangesAsync();

        return newProduct;
    }
}