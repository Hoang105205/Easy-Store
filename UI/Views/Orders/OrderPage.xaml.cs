using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using Microsoft.Extensions.DependencyInjection;
using UI.ViewModels.Orders;

namespace UI.Views.Orders
{
    public sealed partial class OrderPage : Page
    {
        public OrderPageViewModel OrderVM { get; }
        private bool _isPageReady = false;

        public OrderPage()
        {
            InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            this.Loaded += (s, e) => _isPageReady = true;

            this.Unloaded += OrderPage_Unloaded;

            OrderVM = (App.Current as App)!.Services.GetRequiredService<OrderPageViewModel>();

            // Setup Navigation Actions
            OrderVM.NavigateToAddOrderAction = () => this.Frame.Navigate(typeof(NewOrderPage));
            OrderVM.NavigateToOrderDetailAction = (orderId) => this.Frame.Navigate(typeof(OrderDetailPage), orderId);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await OrderVM.LoadOrdersAsync();
        }

        private void OrderPage_Unloaded(object sender, RoutedEventArgs e)
        {
            OrderVM?.CancelOperations();
        }

        // Vẫn giữ sự kiện này vì đây là UI Logic (Ràng buộc phạm vi lịch)
        private void DatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            // Bỏ qua nếu page chưa load xong
            if (!_isPageReady) return;

            // Ràng buộc 1: Nếu chọn "Từ ngày", thì "Đến ngày" tối thiểu phải bằng "Từ ngày"
            if (sender == StartDatePicker && StartDatePicker.Date.HasValue)
            {
                EndDatePicker.MinDate = StartDatePicker.Date.Value;

                // Nếu "Đến ngày" đang nhỏ hơn "Từ ngày" thì reset nó
                if (EndDatePicker.Date.HasValue && EndDatePicker.Date.Value < StartDatePicker.Date.Value)
                {
                    EndDatePicker.Date = null;
                }
            }

            // Ràng buộc 2: Nếu chọn "Đến ngày", thì "Từ ngày" tối đa chỉ được bằng "Đến ngày"
            if (sender == EndDatePicker && EndDatePicker.Date.HasValue)
            {
                StartDatePicker.MaxDate = EndDatePicker.Date.Value;

                // Nếu "Từ ngày" đang lớn hơn "Đến ngày" thì reset nó
                if (StartDatePicker.Date.HasValue && StartDatePicker.Date.Value > EndDatePicker.Date.Value)
                {
                    StartDatePicker.Date = null;
                }
            }

            // Nếu người dùng xóa (clear) "Từ ngày", gỡ bỏ giới hạn MaxDate của "Đến ngày"
            if (sender == StartDatePicker && !StartDatePicker.Date.HasValue)
            {
                EndDatePicker.MinDate = new DateTimeOffset(new DateTime(1920, 1, 1)); // Reset về mặc định
            }

            // Nếu người dùng xóa (clear) "Đến ngày", gỡ bỏ giới hạn MinDate của "Từ ngày"
            if (sender == EndDatePicker && !EndDatePicker.Date.HasValue)
            {
                StartDatePicker.MaxDate = new DateTimeOffset(new DateTime(2100, 1, 1)); // Reset về mặc định
            }

            // Không cần gọi ExecuteSearchAndFilter() ở đây vì TwoWay Binding ở XAML sẽ tự động trigger Event ở ViewModel
        }

        private void OrdersGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (OrdersGrid.SelectedItem is OrderModel selectedOrder)
            {
                // Thay vì chuyển Frame ở đây, ta dùng Command
                OrderVM.GoToOrderDetailCommand.Execute(selectedOrder);
            }
        }
    }
}
