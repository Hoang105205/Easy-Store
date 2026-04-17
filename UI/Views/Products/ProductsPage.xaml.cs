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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UI.Dialog;
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
        public ProductViewModel ProductVM { get; }
        public CategoryViewModel CategoryVM { get; }

        private bool _isPageReady = false;

        public ProductsPage()
        {
            ProductVM = (App.Current as App)!.Services.GetRequiredService<ProductViewModel>();
            CategoryVM = (App.Current as App)!.Services.GetRequiredService<CategoryViewModel>();

            InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            ProductVM.NavigateToAddProductAction = () => this.Frame.Navigate(typeof(Products.CreateProductPage));
            ProductVM.NavigateToProductDetailAction = (productId) => this.Frame.Navigate(typeof(Products.ProductDetailPage), productId);

            this.Unloaded += ProductsPage_Unloaded;

            CategoryVM.ConfirmDeleteAction = async (categoryName) =>
            {
                ContentDialog deleteDialog = new ContentDialog
                {
                    Title = "Xác nhận xóa",
                    Content = $"Bạn có chắc chắn muốn xóa danh mục '{categoryName}' không?\nHành động này không thể hoàn tác.",
                    PrimaryButtonText = "Xóa",
                    CloseButtonText = "Hủy",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };
                var result = await deleteDialog.ShowAsync();
                return result == ContentDialogResult.Primary;
            };

            CategoryVM.ShowErrorAction = async (errorMessage) =>
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Không thể xóa danh mục",
                    Content = errorMessage,
                    CloseButtonText = "Đóng",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            };

            CategoryVM.ShowCreateCategoryDialogAction = async () =>
            {
                var dialog = new NewCategoryDialog(CategoryVM)
                {
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    return dialog.CreatedCategoryName;
                }
                return null;
            };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await ProductVM.LoadProductsAsync();
            await CategoryVM.LoadCategoriesAsync();
        }

        private void ProductsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ProductModel selectedProduct)
            {
                ProductVM.GoToProductDetailCommand.Execute(selectedProduct);
            }
        }

        private void ProductsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ProductVM?.UnregisterMessages();
        }

        private void NumberTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                string rawNumber = new string(textBox.Text.Where(char.IsDigit).ToArray());
                textBox.Text = rawNumber;
                textBox.Select(textBox.Text.Length, 0);
            }
        }

        private void NumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                string rawNumber = new string(textBox.Text.Where(char.IsDigit).ToArray());

                if (long.TryParse(rawNumber, out long value))
                {
                    string formatString = textBox.Tag?.ToString() ?? "{0:N0}";
                    textBox.Text = string.Format(new System.Globalization.CultureInfo("vi-VN"), formatString, value);
                }
                else
                {
                    textBox.Text = string.Empty;
                }
            }
        }
    }
}
