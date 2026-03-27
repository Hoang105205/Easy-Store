using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
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

        private void OrderTabView_AddTabButtonClick(TabView sender, object args)
        {
            // Gọi Command của ViewModel
            ViewModel.AddEmptyTabCommand.Execute(null);
        }
    }
}