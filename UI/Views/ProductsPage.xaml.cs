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
        // Các biến dùng để lưu trữ trạng thái phân trang của GraphQL
        private string? _currentEndCursor = null;
        private string? _currentStartCursor = null;
        private int _currentPageNumber = 1;

        // Stack lưu lại lịch sử Cursor để quay về "Trang trước"
        // Vì GraphQL Cursor chỉ có EndCursor đi tới, muốn lùi lại phải nhớ con đường đã đi
        private System.Collections.Generic.Stack<string> _previousCursors = new();

        public ProductsPage()
        {
            InitializeComponent();

            // Gọi hàm load dữ liệu ban đầu
            LoadProductsAsync(null);
        }

        private async void LoadProductsAsync(string? afterCursor)
        {
            try
            {
                // 1. Lấy Client đã được đăng ký trong App.xaml.cs
                var client = App.Current.Services.GetRequiredService<IEasyStoreClient>();

                // 2. Gọi API kéo 20 sản phẩm
                var result = await client.GetProducts.ExecuteAsync(first: 10, after: afterCursor);

                if (result.Errors.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[GRAPHQL ERROR] {result.Errors[0].Message}");
                    return;
                }

                // 3. Đẩy dữ liệu lên UI (Bắt buộc dùng TryEnqueue để an toàn với Thread)
                DispatcherQueue.TryEnqueue(() =>
                {
                    // 1. ÉP DỮ LIỆU TỪ DẠNG "ẨN" CỦA GRAPHQL SANG DẠNG "HIỆN" CỦA UI (MAPPING)
                    var mappedData = result.Data?.Products?.Nodes?.Select(x => new ProductModel
                    {
                        Name = x.Name,
                        CategoryName = x.Category?.Name ?? "Chưa có danh mục",
                        ImagePath = x.Images?.FirstOrDefault(i => i.IsPrimary)?.ImagePath ?? "ms-appx:///Assets/StoreLogo.png",
                        StockQuantity = x.StockQuantity,
                        SalePrice = x.SalePrice
                    }).ToList();

                    // 2. Gán danh sách đã bọc vào bảng
                    ProductsListView.ItemsSource = mappedData;

                    // Cập nhật Cursor và trạng thái nút "Trang sau"
                    var pageInfo = result.Data?.Products?.PageInfo;
                    if (pageInfo != null)
                    {
                        _currentEndCursor = pageInfo.EndCursor;
                        BtnNextPage.IsEnabled = pageInfo.HasNextPage;
                    }

                    // Cập nhật trạng thái nút "Trang trước" và số trang
                    BtnPreviousPage.IsEnabled = _currentPageNumber > 1;
                    TxtCurrentPage.Text = _currentPageNumber.ToString();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NETWORK ERROR] {ex.Message}");
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            // Khi đi tới trang sau, phải lưu lại cái Cursor hiện tại vào Stack để lát còn biết đường lùi về
            if (_currentEndCursor != null)
            {
                _previousCursors.Push(_currentEndCursor);
            }

            _currentPageNumber++;
            LoadProductsAsync(_currentEndCursor); // Truyền EndCursor vào hàm load để lấy 20 món tiếp theo
        }

        private void BtnPreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPageNumber > 1 && _previousCursors.Count > 0)
            {
                _currentPageNumber--;

                // Lấy cursor của trang trước đó ra khỏi Stack
                _previousCursors.Pop();

                // Nếu Pop xong mà Stack trống nghĩa là ta đã lùi về tận Trang 1 (afterCursor = null)
                string? cursorToLoad = _previousCursors.Count > 0 ? _previousCursors.Peek() : null;

                LoadProductsAsync(cursorToLoad);
            }
        }
    }

    public class ProductModel
    {
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public string ImagePath { get; set; }
        public int StockQuantity { get; set; }
        public long SalePrice { get; set; }
    }
}
