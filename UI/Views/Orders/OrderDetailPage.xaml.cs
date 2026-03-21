using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using UI.ViewModels.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace UI.Views.Orders
{
    public sealed partial class OrderDetailPage : Page
    {
        public OrderDetailPageViewModel ViewModel { get; }

        public OrderDetailPage()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<OrderDetailPageViewModel>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Guid orderId)
            {
                await ViewModel.LoadOrderAsync(orderId);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Xác nhận thanh toán",
                Content = "Bạn có chắc chắn muốn xác nhận thanh toán cho đơn hàng này?",
                PrimaryButtonText = "Thanh toán",
                CloseButtonText = "Hủy",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.PayOrderAsync();
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Xác nhận xóa",
                Content = "Hành động này không thể hoàn tác. Bạn có chắc chắn muốn xóa đơn hàng này?",
                PrimaryButtonText = "Xóa",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                bool isDeleted = await ViewModel.DeleteOrderAsync();
                if (isDeleted && Frame.CanGoBack)
                {
                    // Trở về trang trước sau khi xóa thành công
                    Frame.GoBack();
                }
            }
        }
    }
}