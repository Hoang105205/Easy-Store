using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UI.Services.CategoryService;
using UI.Services.ProductService;

namespace UI.ViewModels
{
    public class CategoryDropdownItem
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }

        public bool IsCreateNewAction => Id == Guid.Empty;
    }

    public class CategoryViewModel
    {
        private readonly CategoryService _categoryService;

        // Khai báo DispatcherQueue
        private readonly DispatcherQueue _dispatcherQueue;

        public ObservableCollection<CategoryDropdownItem> Categories { get; } = new();

        public static readonly Guid CREATE_NEW_CATEGORY_ID = Guid.Empty;

        public CategoryViewModel()
        {
            _categoryService = App.Current.Services.GetRequiredService<CategoryService>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        public async Task LoadCategoriesAsync()
        {
            var apiCategories = await _categoryService.GetCategoriesAsync();

            _dispatcherQueue.TryEnqueue(() =>
            {
                Categories.Clear();

                Categories.Add(new CategoryDropdownItem
                {
                    Id = null,
                    Name = "Danh mục"
                });

                foreach (var cat in apiCategories)
                {
                    Categories.Add(new CategoryDropdownItem
                    {
                        Id = cat.Id,
                        Name = cat.Name
                    });
                }

                Categories.Add(new CategoryDropdownItem
                { 
                    Id = CREATE_NEW_CATEGORY_ID,
                    Name = "+ Tạo danh mục mới..."
                });
            });
        }

        public async Task<bool> CreateCategoryAsync(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return false;

            bool isSuccess = await _categoryService.CreateCategoryAsync(categoryName.Trim());

            if (isSuccess)
            {
                await LoadCategoriesAsync();
                return true;
            }

            return false;
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> DeleteCategoryAsync(Guid categoryId)
        {
            var result = await _categoryService.DeleteCategoryAsync(categoryId);

            if (result.IsSuccess)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    var categoryToRemove = Categories.FirstOrDefault(c => c.Id == categoryId);
                    if (categoryToRemove != null)
                    {
                        Categories.Remove(categoryToRemove);
                    }
                });
            }

            return result;
        }

        public CategoryDropdownItem GetCategoryByName(string name)
        {
            return Categories.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}