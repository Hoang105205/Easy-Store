using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.ViewModels;

namespace UI.Services.CategoryService
{
    public class CategoryModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class CategoryService
    {
        private readonly IEasyStoreClient _client;

        public CategoryService()
        {
            // Lấy GraphQL Client từ Service Provider
            _client = App.Current.Services.GetRequiredService<IEasyStoreClient>();
        }

        public async Task<List<CategoryModel>> GetCategoriesAsync()
        {
            try
            {
                var result = await _client.GetCategories.ExecuteAsync();

                if (result.Errors.Count > 0 || result.Data == null)
                {
                    return new List<CategoryModel>();
                }

                return result.Data.Categories.Select(c => new CategoryModel
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi Exception khi lấy Category: {ex.Message}");
                return new List<CategoryModel>();
            }
        }

        public async Task<bool> CreateCategoryAsync(string categoryName)
        {
            try
            {
                var result = await _client.CreateCategory.ExecuteAsync(categoryName);

                if (result.Errors.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi GraphQL: {result.Errors[0].Message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi Exception khi tạo Category: {ex.Message}");
                return false;
            }
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> DeleteCategoryAsync(Guid categoryId)
        {
            try
            {
                var result = await _client.DeleteCategory.ExecuteAsync(categoryId);

                // Kiểm tra xem server có ném lỗi (GraphQLException) về không
                if (result.Errors.Count > 0)
                {
                    // Lấy câu thông báo lỗi từ HotChocolate trả về (VD: "Không thể xóa danh mục này...")
                    return (false, result.Errors[0].Message);
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi mạng khi xóa Category: {ex.Message}");
                return (false, "Đã xảy ra lỗi kết nối đến máy chủ. Vui lòng thử lại sau.");
            }
        }
    }
}
