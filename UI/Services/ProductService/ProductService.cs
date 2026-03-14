using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.ViewModels; // Để dùng chung ProductModel

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
        public async Task<(List<ProductModel> Products, string? EndCursor, bool HasNextPage)> GetProductsAsync(int itemsPerPage, string? afterCursor)
        {
            var result = await _client.GetProducts.ExecuteAsync(first: itemsPerPage, after: afterCursor);

            if (result.Errors.Count > 0)
            {
                // Quăng lỗi để ViewModel bắt và xử lý
                throw new Exception(result.Errors[0].Message);
            }

            // Map dữ liệu GraphQL sang Model của ứng dụng
            var mappedData = result.Data?.Products?.Nodes?.Select(x => new ProductModel
            {
                Name = x.Name,
                CategoryName = x.Category?.Name ?? "Chưa có danh mục",
                ImagePath = x.Images?.FirstOrDefault(i => i.IsPrimary)?.ImagePath ?? "ms-appx:///Assets/StoreLogo.png",
                StockQuantity = x.StockQuantity,
                SalePrice = x.SalePrice
            }).ToList() ?? new List<ProductModel>();

            var pageInfo = result.Data?.Products?.PageInfo;

            return (mappedData, pageInfo?.EndCursor, pageInfo?.HasNextPage ?? false);
        }
    }
}