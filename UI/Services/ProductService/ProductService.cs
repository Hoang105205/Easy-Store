using Microsoft.Extensions.DependencyInjection;
using StrawberryShake;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.ViewModels.Product; // Để dùng chung ProductModel

namespace UI.Services.ProductService
{
    public class ProductService
    {
        private readonly IEasyStoreClient _client;

        public ProductService()
        {
            // Lấy GraphQL Client từ Service Provider
            _client = App.Current.Services.GetRequiredService<IEasyStoreClient>();
        }

        // Trả về một Tuple chứa Danh sách sản phẩm, EndCursor và trạng thái HasNextPage
        public async Task<List<ProductModel>> GetProductsAsync(
            string? searchText = null, 
            Guid? categoryId = null
        )
        {
            var result = await _client.GetProducts.ExecuteAsync(
                searchTerm: searchText,
                categoryId: categoryId
            );

            if (result.Errors?.Count > 0)
            {
                // Quăng lỗi để ViewModel bắt và xử lý
                throw new Exception(result.Errors[0].Message);
            }

            // Map dữ liệu GraphQL sang Model của ứng dụng
            var mappedData = result.Data?.Products?.Select(x => new ProductModel
            {
                Id = x.Id,
                Name = x.Name,
                Sku = x.Sku,
                CategoryName = x.Category?.Name ?? "Chưa có danh mục",
                ImagePath = x.Images?.FirstOrDefault(i => i.IsPrimary)?.ImagePath ?? "ms-appx:///Assets/StoreLogo.png",
                StockQuantity = x.StockQuantity,
                SalePrice = x.SalePrice,
                AvailableStockQuantity = x.AvailableStockQuantity
            }).ToList() ?? new List<ProductModel>();

            return mappedData;
        }

        public async Task<(List<ProductModel> Products, string? EndCursor, bool HasNextPage)> GetProductsPaginationAsync(
            int itemsPerPage,
            string? afterCursor,
            string? searchText = null,
            Guid? categoryId = null,
            long? minPrice = null,
            long? maxPrice = null
        )
        {
            var result = await _client.GetProductsPagination.ExecuteAsync(
                first: itemsPerPage,
                after: afterCursor,
                searchTerm: searchText,
                categoryId: categoryId,
                minPrice: minPrice,
                maxPrice: maxPrice
            );

            if (result.Errors?.Count > 0)
            {
                // Quăng lỗi để ViewModel bắt và xử lý
                throw new Exception(result.Errors[0].Message);
            }

            // Map dữ liệu GraphQL sang Model của ứng dụng
            var mappedData = result.Data?.ProductsPagination?.Nodes?.Select(x => new ProductModel
            {
                Id = x.Id,
                Name = x.Name,
                Sku = x.Sku,
                CategoryName = x.Category?.Name ?? "Chưa có danh mục",
                ImagePath = x.Images?.FirstOrDefault(i => i.IsPrimary)?.ImagePath ?? "ms-appx:///Assets/StoreLogo.png",
                StockQuantity = x.StockQuantity,
                SalePrice = x.SalePrice,
                AvailableStockQuantity = x.AvailableStockQuantity
            }).ToList() ?? new List<ProductModel>();

            var pageInfo = result.Data?.ProductsPagination?.PageInfo;

            return (mappedData, pageInfo?.EndCursor, pageInfo?.HasNextPage ?? false);
        }

        public async Task<bool> CreateProductAsync(string sku, string name, Guid categoryId, int minimumStockQuantity, List<string> images)
        {
            var input = new CreateProductInput
            {
                Sku = sku,
                Name = name,
                CategoryId = categoryId,
                MinimumStockQuantity = minimumStockQuantity,
                ImagePaths = images
            };

            var result = await _client.CreateProduct.ExecuteAsync(input);

            if (result.Errors.Count > 0)
            {
                throw new Exception(result.Errors[0].Message);
            }

            return result.Data?.CreateProduct != null;
        }

        public async Task<IGetProductById_ProductById?> GetProductByIdAsync(Guid id)
        {
            var result = await _client.GetProductById.ExecuteAsync(id);
            if (result.Errors.Count > 0) throw new Exception(result.Errors[0].Message);
            return result.Data?.ProductById;
        }

        public async Task<bool> UpdateProductAsync(Guid id, string sku, string name, Guid categoryId, long salePrice, int minimumStockQuantity, List<string> images)
        {
            var input = new UpdateProductInput
            {
                Id = id,
                Sku = sku,
                Name = name,
                CategoryId = categoryId,
                SalePrice = salePrice,
                MinimumStockQuantity = minimumStockQuantity,
                ImagePaths = images
            };
            var result = await _client.UpdateProduct.ExecuteAsync(input);
            if (result.Errors.Count > 0) throw new Exception(result.Errors[0].Message);
            return result.Data?.UpdateProduct != null;
        }
        public async Task<bool> DeleteProductAsync(Guid id)
        {
            var result = await _client.DeleteProduct.ExecuteAsync(id);
            if (result.Errors.Count > 0) throw new Exception(result.Errors[0].Message);
            return result.Data?.DeleteProduct ?? false;
        }

        public async Task<IOperationResult<IGetProductBySkuResult>> GetProductBySkuAsync(string sku)
        {
            return await _client.GetProductBySku.ExecuteAsync(sku);
        }
    }
}