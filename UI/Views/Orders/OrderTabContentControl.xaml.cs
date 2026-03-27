using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using UI.ViewModels.Orders;

namespace UI.Views.Orders
{
    public sealed partial class OrderTabContentControl : UserControl
    {
        public NewOrderPageViewModel ViewModel { get; }

        private bool _isDataLoaded = false; // chặn load nhiều lần

        public OrderTabContentControl()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<NewOrderPageViewModel>();

            this.Loaded += OrderTabContentControl_Loaded;
            this.Unloaded += OrderTabContentControl_Unloaded;
        }

        private async void OrderTabContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Cung cấp XamlRoot để VM gọi Dialog
            ViewModel.XamlRoot = this.XamlRoot;

            // Lấy Model của Tab hiện tại từ DataContext do XAML DataTemplate truyền vào
            if (this.DataContext is OrderTabItemModel tabModel)
            {
                // Nối tín hiệu Hủy/Thanh toán xong từ VM ra Action tắt Tab của Container
                ViewModel.RequestCloseTabAction = () =>
                {
                    tabModel.RequestCloseAction?.Invoke();
                };
            }

            if (_isDataLoaded) return;
            _isDataLoaded = true;

            await ViewModel.LoadInitialDataAsync();

            // Nếu Tab này có DraftId, tự động load chi tiết đơn nháp
            if (this.DataContext is OrderTabItemModel model && model.DraftId.HasValue)
            {
                await ViewModel.LoadExistingDraftOrderAsync(model.DraftId.Value);
            }
        }

        private void OrderTabContentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ForceSaveIfNeeded();
        }

        private void ProductsGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is UI.ViewModels.Product.ProductModel selectedProduct)
            {
                ViewModel.AddProductToCartCommand.Execute(selectedProduct);
            }
        }

        private void CartGrid_LoadingRow(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridRowEventArgs e)
        {
            var menuFlyout = new MenuFlyout();
            var deleteItem = new MenuFlyoutItem { Text = "Xóa sản phẩm", Icon = new SymbolIcon(Symbol.Delete) };

            deleteItem.Click += (s, args) =>
            {
                if (e.Row.DataContext is CartItemModel itemToRemove)
                {
                    ViewModel.RemoveItemCommand.Execute(itemToRemove);
                }
            };

            menuFlyout.Items.Add(deleteItem);
            e.Row.ContextFlyout = menuFlyout;
        }

        private void CartQuantity_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (sender.DataContext is CartItemModel item)
            {
                if (double.IsNaN(args.NewValue) || args.NewValue < 1)
                {
                    sender.Value = 1;
                    item.Quantity = 1;
                }
                else
                {
                    item.Quantity = (int)args.NewValue;
                }
                ViewModel.UpdateCartTotal();
            }
        }
    }
}