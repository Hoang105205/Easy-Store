using Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UI.ViewModels;
using UI.ViewModels.Product;
using UI.Views.Products;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProductsPage : Page
    {
        // Khai báo ViewModel để file XAML có thể x:Bind tới
        public ProductViewModel ProductVM { get; }
        public CategoryViewModel CategoryVM { get; }

        private bool _isPageReady = false;
        private int _waitingInterval = 500;

        private Guid? currentCategoryId = null;
        private string? currentSearchText = null;
        private long? minPrice = null, maxPrice = null;

        private DispatcherTimer _debounceTimer;

        public ProductsPage()
        {
            InitializeComponent();
            // Đổi từ Loaded thành sự kiện này:

            this.Loaded += (s, e) => _isPageReady = true;

            ProductVM = (App.Current as App)!.Services.GetService<ProductViewModel>();
            CategoryVM = (App.Current as App)!.Services.GetService<CategoryViewModel>();


            _debounceTimer = new DispatcherTimer();
            _debounceTimer.Interval = TimeSpan.FromMilliseconds(_waitingInterval);
            _debounceTimer.Tick += DebounceTimer_Tick;
        }

        private long? ParsePrice(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            string cleanString = Regex.Replace(input, @"[^\d]", "");

            if (long.TryParse(cleanString, out long result))
            {
                return result;
            }

            return null;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await ProductVM.LoadProductsAsync();
            await CategoryVM.LoadCategoriesAsync();
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            _debounceTimer.Stop();
            ExecuteSearchAndFilter();
        }

        private void DebounceTimer_Tick(object sender, object e)
        {
            _debounceTimer.Stop();
            ExecuteSearchAndFilter();
        }

        private async void ExecuteSearchAndFilter()
        {
            if (!_isPageReady) return;

            currentSearchText = SearchBox.Text.Trim();
            if (string.IsNullOrEmpty(currentSearchText))
            {
                currentSearchText = null;
            }

            currentCategoryId = null;
            if (CategoryComboBox.SelectedItem is CategoryDropdownItem selectedCategory &&
                selectedCategory.Id != CategoryViewModel.CREATE_NEW_CATEGORY_ID &&
                selectedCategory != null)
            {
                currentCategoryId = selectedCategory.Id;
            }

            string rawMin = TxtMinPrice.Text;
            string rawMax = TxtMaxPrice.Text;
            minPrice = ParsePrice(rawMin);
            maxPrice = ParsePrice(rawMax);

            await ProductVM.LoadProductsAsync(
                searchText: currentSearchText, 
                categoryId: currentCategoryId,
                minPrice: minPrice,
                maxPrice: maxPrice
            );
        }

        private async void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isPageReady) return;

            if (CategoryComboBox.SelectedItem is not CategoryDropdownItem selectedCategory)
            {
                BtnDeleteCategory.IsEnabled = false;
                return;
            }

            if (e.AddedItems.Count == 0) return;

            BtnDeleteCategory.IsEnabled = (selectedCategory.Id != CategoryViewModel.CREATE_NEW_CATEGORY_ID && selectedCategory.Id != null);

            if (selectedCategory.Id == CategoryViewModel.CREATE_NEW_CATEGORY_ID)
            {
                CategoryComboBox.SelectedIndex = -1;
                TxtNewCategoryName.Text = string.Empty;
                TxtCategoryError.Visibility = Visibility.Collapsed;
                NewCategoryDialog.XamlRoot = this.XamlRoot;

                var result = await NewCategoryDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    string newCategoryName = TxtNewCategoryName.Text;

                    // Giao việc tạo mới cho ViewModel
                    bool success = await CategoryVM.CreateCategoryAsync(newCategoryName);

                    if (success)
                    {
                        this.DispatcherQueue.TryEnqueue(() =>
                        {
                            var newlyCreatedCategory = CategoryVM.GetCategoryByName(newCategoryName);
                            if (newlyCreatedCategory != null)
                            {
                                CategoryComboBox.SelectedItem = newlyCreatedCategory;
                            }
                        });
                    }
                    else
                    {
                        // TODO: Hiển thị thông báo lỗi (nếu cần)
                    }
                }
            }
            else
            {
                _debounceTimer.Stop();
                ExecuteSearchAndFilter();
            }
        }

        private void PriceInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private async void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            await ProductVM.NextPageAsync(
                searchText: currentSearchText,
                categoryId: currentCategoryId,
                minPrice: minPrice,
                maxPrice: maxPrice
            );
        }

        private async void BtnPreviousPage_Click(object sender, RoutedEventArgs e)
        {
            await ProductVM.PreviousPageAsync(
                searchText: currentSearchText,
                categoryId: currentCategoryId,
                minPrice: minPrice,
                maxPrice: maxPrice
            );
        }
        
        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(CreateProductPage));
        }

        private void ProductsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ProductModel selectedProduct)
            {
                this.Frame.Navigate(typeof(Products.ProductDetailPage), selectedProduct.Id);
            }
        }
        private void NewCategoryDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            string inputName = TxtNewCategoryName.Text.Trim();

            if (string.IsNullOrEmpty(inputName))
            {
                TxtCategoryError.Text = "Tên danh mục không được để trống.";
                TxtCategoryError.Visibility = Visibility.Visible;

                args.Cancel = true;
                return;
            }

            bool isDuplicate = CategoryVM.Categories.Any(c =>
                c.Name.Equals(inputName, StringComparison.OrdinalIgnoreCase) &&
                c.Id != CategoryViewModel.CREATE_NEW_CATEGORY_ID);

            if (isDuplicate)
            {
                TxtCategoryError.Text = $"Danh mục '{inputName}' đã tồn tại. Vui lòng chọn tên khác.";
                TxtCategoryError.Visibility = Visibility.Visible;

                args.Cancel = true;
                return;
            }

            TxtCategoryError.Visibility = Visibility.Collapsed;
        }

        private void TxtNewCategoryName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtCategoryError.Visibility == Visibility.Visible)
            {
                TxtCategoryError.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem is not CategoryDropdownItem categoryToDelete)
                return;

            // 1. Hộp thoại xác nhận thao tác nguy hiểm
            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "Xác nhận xóa",
                Content = $"Bạn có chắc chắn muốn xóa danh mục '{categoryToDelete.Name}' không?\nHành động này không thể hoàn tác.",
                PrimaryButtonText = "Xóa",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var confirmResult = await deleteDialog.ShowAsync();

            if (confirmResult == ContentDialogResult.Primary)
            {
                // 2. Gọi ViewModel để xóa (nhận về Tuple)

                if (categoryToDelete.Id == null)
                {
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "Lỗi không xác định",
                        Content = "Danh mục không hợp lệ. Vui lòng thử lại.",
                        CloseButtonText = "Đóng",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                    return;
                }

                var categoryId = (Guid) categoryToDelete.Id;
                var (isSuccess, errorMessage) = await CategoryVM.DeleteCategoryAsync(categoryId);

                if (isSuccess)
                {
                    // Reset ComboBox về trạng thái trống
                    CategoryComboBox.SelectedIndex = -1;

                    // 3. Xóa thành công thì tải lại danh sách sản phẩm (ProductVM)
                    // Vì danh mục đã mất, các sản phẩm đang hiển thị (nếu lọc theo danh mục đó) cũng không còn ý nghĩa
                    await ProductVM.LoadProductsAsync();
                }
                else
                {
                    // 4. Nếu thất bại (có sản phẩm bên trong), hiển thị Dialog báo lỗi
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "Không thể xóa danh mục",
                        Content = errorMessage, // Hiển thị đúng câu lỗi gửi từ Backend
                        CloseButtonText = "Đóng",
                        XamlRoot = this.XamlRoot
                    };

                    await errorDialog.ShowAsync();
                }
            }
        }
    }
}
