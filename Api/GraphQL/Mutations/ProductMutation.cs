using Core.Data;
using Core.Models;
using HotChocolate;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.GraphQL.Mutations;

// DTO nhận dữ liệu từ Client
public record CreateProductInput(
    string Sku,
    string Name,
    Guid CategoryId,
    long ImportPrice,
    long SalePrice,
    int StockQuantity,
    List<string> ImagePaths
);

public record UpdateProductInput(
    Guid Id,
    string Sku,
    string Name,
    Guid CategoryId,
    long SalePrice,
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
            SKU = input.Sku,
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

    public async Task<Product> UpdateProductAsync(
        UpdateProductInput input,
        [Service] AppDbContext dbContext)
    {
        var product = await dbContext.Products.FindAsync(input.Id);
        if (product == null) throw new Exception("Không tìm thấy sản phẩm.");

        // Cập nhật thông tin cơ bản
        product.SKU = input.Sku;
        product.Name = input.Name;
        product.CategoryId = input.CategoryId;
        product.SalePrice = input.SalePrice;
        product.UpdatedAt = DateTime.UtcNow;

        // Cập nhật ảnh (Xóa ảnh cũ, thêm ảnh mới)
        var oldImages = dbContext.ProductImages.Where(i => i.ProductId == product.Id);
        dbContext.ProductImages.RemoveRange(oldImages);

        if (input.ImagePaths != null)
        {
            foreach (var path in input.ImagePaths)
            {
                dbContext.ProductImages.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ImagePath = path,
                    IsPrimary = input.ImagePaths.IndexOf(path) == 0
                });
            }
        }

        await dbContext.SaveChangesAsync();
        return product;
    }

    public async Task<bool> DeleteProductAsync(
        Guid id,
        [Service] AppDbContext dbContext)
    {
        var product = await dbContext.Products.FindAsync(id);
        if (product == null) throw new Exception("Không tìm thấy sản phẩm.");

        // Hiện tại ta cho phép xóa thẳng (Hard Delete).
        // TO-DO: Sau này có bảng OrderDetail thì kiểm tra ForeignKey ở đây.

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync();

        return true;
    }
}