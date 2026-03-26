using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UI.Messages;
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

    public partial class CategoryViewModel : ObservableObject
    {
        private readonly CategoryService _categoryService;

        // Khai báo DispatcherQueue
        private readonly DispatcherQueue _dispatcherQueue;

        public Func<string, Task<bool>>? ConfirmDeleteAction { get; set; }
        public Func<Task<string?>>? ShowCreateCategoryDialogAction { get; set; }

        public Action<string>? ShowErrorAction { get; set; }

        public ObservableCollection<CategoryDropdownItem> Categories { get; } = new();
        [ObservableProperty] private CategoryDropdownItem? selectedCategory = null;
        [ObservableProperty] private bool deletable = false;

        public static readonly Guid CREATE_NEW_CATEGORY_ID = Guid.Empty;

        public CategoryViewModel()
        {
            _categoryService = App.Current.Services.GetRequiredService<CategoryService>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        partial void OnSelectedCategoryChanged(CategoryDropdownItem value)
        {
            DeleteSelectedCategoryCommand.NotifyCanExecuteChanged();
            Deletable = value != null && value.Id != null && value.Id != CREATE_NEW_CATEGORY_ID;

            if (value == null) return;

            if (value.Id == CREATE_NEW_CATEGORY_ID)
            {
                HandleCreateNewCategoryAsync();
            }
            else
            {
                Guid? idToSend = value.Id;
                WeakReferenceMessenger.Default.Send(new CategorySelectedMessage(idToSend));
            }
        }

        public async Task LoadCategoriesAsync(bool includeCreateNew = true)
        {
            var apiCategories = await _categoryService.GetCategoriesAsync();

            var tcs = new TaskCompletionSource();

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

                if (includeCreateNew)
                {
                    Categories.Add(new CategoryDropdownItem
                    {
                        Id = CREATE_NEW_CATEGORY_ID,
                        Name = "+ Tạo danh mục mới..."
                    });
                }

                tcs.SetResult();
            });

            await tcs.Task;
        }

        public async Task<bool> CreateCategoryAsync(string categoryName)
        {
            Debug.WriteLine(categoryName);
            if (string.IsNullOrWhiteSpace(categoryName)) return false;

            bool isSuccess = await _categoryService.CreateCategoryAsync(categoryName.Trim());

            if (isSuccess)
            {
                await LoadCategoriesAsync();
                return true;
            }

            return false;
        }

        private async void HandleCreateNewCategoryAsync()
        {
            if (ShowCreateCategoryDialogAction != null)
            {
                string? newCategoryName = await ShowCreateCategoryDialogAction.Invoke();

                if (!string.IsNullOrWhiteSpace(newCategoryName))
                {
                    bool success = await CreateCategoryAsync(newCategoryName);
                    if (success)
                    {
                        var newlyCreatedCategory = GetCategoryByName(newCategoryName);
                        if (newlyCreatedCategory != null)
                        {
                            SelectedCategory = newlyCreatedCategory;
                            return;
                        }
                    }
                }

                SelectedCategory = null;
            }
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> DeleteCategoryAsync()
        {
            if (SelectedCategory == null || SelectedCategory.Id == null || SelectedCategory.Id == CREATE_NEW_CATEGORY_ID)
            {
                return (false, "Vui lòng chọn một danh mục hợp lệ để xóa.");
            }

            var categoryId = SelectedCategory.Id.Value;
            var result = await _categoryService.DeleteCategoryAsync(categoryId);

            if (result.IsSuccess)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    var categoryToRemove = Categories.FirstOrDefault(c => c.Id == categoryId);
                    if (categoryToRemove != null)
                    {
                        Categories.Remove(categoryToRemove);
                        WeakReferenceMessenger.Default.Send(new CategoryDeletedMessage(categoryId));
                    }
                });
            }

            return result;
        }

        //[RelayCommand(CanExecute = nameof(CanDeleteSelectedCategory))]
        [RelayCommand]
        public async Task DeleteSelectedCategory()
        {
            if (ConfirmDeleteAction != null)
            {
                bool isConfirmed = await ConfirmDeleteAction.Invoke(SelectedCategory.Name);
                if (!isConfirmed) return;
            }

            var result = await DeleteCategoryAsync();

            if (!result.IsSuccess)
            {
                ShowErrorAction?.Invoke(result.ErrorMessage);
            }
        }

        public CategoryDropdownItem? GetCategoryByName(string name)
        {
            return Categories.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsCategoryNameDuplicate(string inputName)
        {
            if (string.IsNullOrWhiteSpace(inputName)) return false;

            return Categories.Any(c =>
                string.Equals(c.Name, inputName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                c.Id != CREATE_NEW_CATEGORY_ID);
        }
    }
}