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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UI.ViewModels;
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
        public ProductViewModel ViewModel { get; } = new ProductViewModel();

        public ProductsPage()
        {
            InitializeComponent();
            Loaded += ProductsPage_Loaded;
        }

        private async void ProductsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Tự động load dữ liệu khi trang được mở lên
            await ViewModel.LoadProductsAsync();
        }

        private async void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.NextPageAsync();
        }

        private async void BtnPreviousPage_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.PreviousPageAsync();
        }
        
        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(CreateProductPage));
        }
    }
}
