using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using UI.ViewModels.Orders;

namespace UI.Views.Orders
{
    public sealed partial class OrderTabContentControl : UserControl
    {
        // Nhận ViewModel từ DataContext (do Container truyền vào), không tự khởi tạo bằng DI
        public NewOrderPageViewModel ViewModel { get; private set; }

        public OrderTabContentControl()
        {
            this.InitializeComponent();
            this.Loaded += OrderTabContentControl_Loaded;
            this.Unloaded += OrderTabContentControl_Unloaded;
        }

        // Force x:Bind hoạt động đúng với DataTemplate
        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue is NewOrderPageViewModel vm)
            {
                ViewModel = vm;
                this.Bindings.Update();
            }
        }

        private async void OrderTabContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            ViewModel.XamlRoot = this.XamlRoot;
        }

        private void OrderTabContentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.ForceSaveIfNeeded();
                // ViewModel.Cleanup();
                // Nếu gọi Cleanup() ở đây, Tab này sẽ bị ngắt kết nối Messenger
                // Khi Tab khác thêm/xóa sản phẩm, Tab này sẽ không được cập nhật lại MaxQuantity.
            }
        }

        private async void CartQuantity_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (sender.DataContext is CartItemModel item && ViewModel != null)
            {
                // parse giá trị dị thường (NaN) của WinUI 3
                int newValue = double.IsNaN(args.NewValue) ? 1 : (int)args.NewValue;

                // kiểm tra tại ViewModel
                int validValue = await ViewModel.HandleQuantityChangedAsync(item, newValue);

                // Nếu ViewModel trả về giá trị khác với UI đang hiện (bị ép rollback về Max), ta cập nhật lại UI
                if (sender.Value != validValue) sender.Value = validValue;
            }
        }
    }
}