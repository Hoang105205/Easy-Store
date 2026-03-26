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

            this.Loaded += (s, e) =>
            {
                ViewModel.XamlRoot = this.XamlRoot;
            };

            ViewModel.NavigateBackAction = () =>
            {
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
            };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Guid orderId)
            {
                await ViewModel.LoadOrderAsync(orderId);
            }
        }
    }
}