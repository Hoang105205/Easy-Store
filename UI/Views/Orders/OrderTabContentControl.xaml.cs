using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;
using UI.ViewModels;
using UI.ViewModels.Orders;
using UI.ViewModels.Product;

namespace UI.Views.Orders
{
    public sealed partial class OrderTabContentControl : UserControl
    {
        // Sự kiện này sẽ được NewOrderPage lắng nghe để biết khi nào cần đóng Tab (khi hủy đơn hoặc tạo đơn thành công)
        public event EventHandler CloseTabRequested;

        // Khởi tạo độc lập để mỗi Tab là một state riêng biệt, không bị lẫn bộ lọc
        public ProductViewModel ProductVM { get; } = new ProductViewModel();
        public CategoryViewModel CategoryVM { get; } = new CategoryViewModel();
        public NewOrderPageViewModel CartVM { get; } = new NewOrderPageViewModel();

        private bool _isDataLoaded = false; // chặn load nhiều lần

        public OrderTabContentControl()
        {
            this.InitializeComponent();
            this.Loaded += OrderTabContentControl_Loaded;
            this.Unloaded += OrderTabContentControl_Unloaded;
        }

        private async void OrderTabContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isDataLoaded) return;
            _isDataLoaded = true;
            // Tải dữ liệu danh mục và sản phẩm ngay khi Tab được mở
            await CategoryVM.LoadCategoriesAsync(includeCreateNew: false);
            await ProductVM.LoadAllProductsAsync();
        }

        private void OrderTabContentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Ép lưu dữ liệu ngầm ngay khi UI bị tháo ra (chuyển tab/chuyển trang)
            CartVM.ForceSaveIfNeeded();
        }

        private async void SearchProductBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await ApplyFiltersAsync();
        }

        private async void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Đảm bảo không gọi filter khi UI chưa render xong
            if (!this.IsLoaded) return;
            await ApplyFiltersAsync();
        }

        // Hàm tổng hợp các tham số và gọi lại Service
        private async Task ApplyFiltersAsync()
        {
            string? searchText = string.IsNullOrWhiteSpace(SearchProductBox.Text) ? null : SearchProductBox.Text;

            Guid? categoryId = null;
            if (CategoryFilter.SelectedItem is CategoryDropdownItem selectedCategory
                && selectedCategory.Id != null
                && selectedCategory.Id != CategoryViewModel.CREATE_NEW_CATEGORY_ID)
            {
                categoryId = selectedCategory.Id;
            }

            // Truyền 2 tham số
            await ProductVM.LoadAllProductsAsync(
                searchText: searchText,
                categoryId: categoryId
            );
        }

        // Bắt sự kiện Double Click từ ProductsGrid bên trái
        private void ProductsGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is ProductModel selectedProduct)
            {
                // Gọi VM để thêm sản phẩm
                CartVM.AddProductToCart(selectedProduct.Id, selectedProduct.Name, selectedProduct.SalePrice ?? 0);
            }
        }

        // Bắt sự kiện DataGrid render ra dòng (Dùng để chèn Menu chuột phải)
        private void CartGrid_LoadingRow(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridRowEventArgs e)
        {
            // Tạo MenuFlyout (Menu chuột phải)
            var menuFlyout = new MenuFlyout();
            var deleteItem = new MenuFlyoutItem
            {
                Text = "Xóa sản phẩm",
                Icon = new SymbolIcon(Symbol.Delete)
            };

            // Xử lý khi bấm nút "Xóa"
            deleteItem.Click += (s, args) =>
            {
                if (e.Row.DataContext is CartItemModel itemToRemove)
                {
                    CartVM.RemoveItem(itemToRemove);
                }
            };

            menuFlyout.Items.Add(deleteItem);

            // Gắn menu vào dòng hiện tại
            e.Row.ContextFlyout = menuFlyout;
        }

        // Bắt sự kiện NumberBox thay đổi giá trị để cập nhật Tổng tiền
        private void CartQuantity_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (sender.DataContext is CartItemModel item)
            {
                // 1. Xử lý lỗi khi người dùng xóa trắng ô nhập (trả về NaN)
                if (double.IsNaN(args.NewValue) || args.NewValue < 1)
                {
                    // Nếu xóa trắng hoặc nhập số < 1, tự động gán lại bằng 1
                    sender.Value = 1;
                    item.Quantity = 1;
                }
                else
                {
                    // 2. Cập nhật trực tiếp số mới vào Model để tránh độ trễ của Binding
                    item.Quantity = (int)args.NewValue;
                }

                // 3. Yêu cầu ViewModel tính lại tổng tiền ngay lập tức
                CartVM.UpdateCartTotal();
            }
        }

        private async void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Hỏi xác nhận trước khi hủy
            var dialog = new ContentDialog
            {
                Title = "Xác nhận hủy",
                Content = "Bạn có chắc chắn muốn hủy đơn hàng này không? Dữ liệu giỏ hàng sẽ bị xóa.",
                PrimaryButtonText = "Đồng ý",
                CloseButtonText = "Không",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await CartVM.CancelOrderAsync();
                CloseTabRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            // Tránh spam click
            var btn = (Button)sender;
            btn.IsEnabled = false;

            var result = await CartVM.FinalizeOrderAsync();

            var dialog = new ContentDialog
            {
                Title = result.IsSuccess ? "Thành công" : "Thất bại",
                Content = result.Message,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();

            btn.IsEnabled = true;

            if (result.IsSuccess)
            {
                CloseTabRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        // Tải lại dữ liệu giỏ hàng khi người dùng chọn đơn nháp khác
        public async Task LoadDraftDataAsync(Guid draftId)
        {
            // Gọi ViewModel để tải chi tiết giỏ hàng
            await CartVM.LoadExistingDraftOrderAsync(draftId);
        }
    }
}