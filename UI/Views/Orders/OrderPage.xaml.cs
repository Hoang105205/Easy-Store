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
using Windows.Foundation;
using Windows.Foundation.Collections;

using Microsoft.Extensions.DependencyInjection;
using UI.ViewModels.Orders;

namespace UI.Views.Orders
{
    public sealed partial class OrderPage : Page
    {
        public OrderPageViewModel OrderVM { get; }

        private bool _isPageReady = false;
        private int _waitingInterval = 500;
        private DispatcherTimer _debounceTimer;

        // Các biến lưu trạng thái lọc hiện tại
        private string? currentSearchReceiptNumber = null;
        private DateTimeOffset? currentStartDate = null;
        private DateTimeOffset? currentEndDate = null;

        public OrderPage()
        {
            InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            this.Loaded += (s, e) => _isPageReady = true;

            OrderVM = (App.Current as App)!.Services.GetRequiredService<OrderPageViewModel>();

            // Khởi tạo bộ đếm Debounce
            _debounceTimer = new DispatcherTimer();
            _debounceTimer.Interval = TimeSpan.FromMilliseconds(_waitingInterval);
            _debounceTimer.Tick += DebounceTimer_Tick;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await OrderVM.LoadOrdersAsync();
        }


        // SearchBox
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

        // 2 CalendarDatePicker là StartDatePicker và EndDatePicker
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

            // Cập nhật trạng thái từ UI
            currentSearchReceiptNumber = SearchBox.Text.Trim();
            if (string.IsNullOrEmpty(currentSearchReceiptNumber))
            {
                currentSearchReceiptNumber = null;
            }

            currentStartDate = StartDatePicker.Date;
            currentEndDate = EndDatePicker.Date;

            // Gọi API thông qua ViewModel
            await OrderVM.LoadOrdersAsync(
                receiptNumber: currentSearchReceiptNumber,
                startDate: currentStartDate,
                endDate: currentEndDate
            );
        }

        // phân trang
        private async void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            await OrderVM.NextPageAsync(
                receiptNumber: currentSearchReceiptNumber,
                startDate: currentStartDate,
                endDate: currentEndDate
            );
        }

        private async void BtnPreviousPage_Click(object sender, RoutedEventArgs e)
        {
            await OrderVM.PreviousPageAsync(
                receiptNumber: currentSearchReceiptNumber,
                startDate: currentStartDate,
                endDate: currentEndDate
            );
        }

        private void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            // this.Frame.Navigate(typeof(CreateOrderPage)); // chưa làm
        }

        private void OrdersGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // Lấy ra dòng vừa double click
            if (OrdersGrid.SelectedItem is OrderModel selectedOrder)
            {
                // truyền Guid
                this.Frame.Navigate(typeof(OrderDetailPage), selectedOrder.Id);
            }
        }
    }
}
