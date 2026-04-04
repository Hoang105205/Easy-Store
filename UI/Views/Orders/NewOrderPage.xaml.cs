using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using UI.ViewModels.Orders;

namespace UI.Views.Orders
{
    public sealed partial class NewOrderPage : Page
    {
        public NewOrderContainerViewModel ViewModel { get; }

        public NewOrderPage()
        {
            InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<NewOrderContainerViewModel>();

            ViewModel.NavigateBackAction = () =>
            {
                if (Frame.CanGoBack) Frame.GoBack();
            };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.InitializeTabsAsync();
        }

        private void OrderTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            // Bắt sự kiện bấm nút 'X' trên UI và truyền xuống ViewModel
            if (args.Item is OrderTabItemModel tabItem)
            {
                ViewModel.CloseTabCommand.Execute(tabItem);
            }
        }
        private void ProductsGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // Lấy DataGrid phát ra sự kiện
            if (sender is CommunityToolkit.WinUI.UI.Controls.DataGrid grid &&
                grid.SelectedItem is UI.ViewModels.Product.ProductModel selectedProduct)
            {
                // Gọi logic ném sản phẩm sang tab hóa đơn bên phải
                ViewModel.HandleProductDoubleTapped(selectedProduct);
            }
        }

        private void PairProductsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Báo cho hệ thống là sự kiện đã được xử lý xong
            // DataGrid sẽ không nhận được tín hiệu click này nữa -> không bị giật focus.
            e.Handled = true;

            if (sender is Button btn)
            {
                FlyoutBase.ShowAttachedFlyout(btn);
            }
        }
        private void PairProductListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is UI.ViewModels.Product.ProductModel selectedPairProduct)
            {
                // Gọi logic ném sản phẩm sang tab hóa đơn
                ViewModel.HandleProductDoubleTapped(selectedPairProduct);

                // Đóng flyout: Lần ngược lên cây giao diện (Visual Tree) để tìm thẻ Popup chứa ListView này
                DependencyObject current = listView;
                while (current != null && !(current is Popup))
                {
                    current = VisualTreeHelper.GetParent(current);
                }

                // Nếu tìm thấy Popup, set IsOpen = false để đóng nó lại
                if (current is Popup popup)
                {
                    popup.IsOpen = false;
                }
            }
        }
    }
}