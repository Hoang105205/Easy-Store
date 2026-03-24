using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using UI.Services.OrderService;

namespace UI.Views.Orders
{
    public sealed partial class NewOrderPage : Page
    {
        private int _tabCounter = 0;
        private readonly OrderService _orderService;

        public NewOrderPage()
        {
            InitializeComponent();
            _orderService = App.Current.Services.GetRequiredService<OrderService>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            OrderTabView.TabItems.Clear();

            var drafts = await _orderService.GetDraftOrdersAsync();

            if (drafts != null && drafts.Count > 0)
            {
                // Nếu có đơn nháp, tạo Tab cho từng đơn
                foreach (var draft in drafts)
                {
                    AddNewTab(draft.Id, "Hóa đơn #" + draft.ReceiptNumber);
                }
            }
            else
            {
                // Nếu không có, tạo 1 tab trống
                AddNewEmptyTab();
            }
        }

        private void OrderTabView_AddTabButtonClick(TabView sender, object args)
        {
            AddNewEmptyTab();
        }

        // Hàm tạo Tab trống
        private void AddNewEmptyTab()
        {
            _tabCounter++;
            var newTab = new TabViewItem
            {
                Header = $"Đơn mới {_tabCounter}",
                IconSource = new SymbolIconSource { Symbol = Symbol.Document }
            };

            var contentControl = new OrderTabContentControl();

            // đăng ký sự kiện đóng tab để ép lưu dữ liệu nháp nếu có
            contentControl.CloseTabRequested += ContentControl_CloseTabRequested;

            newTab.Content = contentControl;
            OrderTabView.TabItems.Add(newTab);
            OrderTabView.SelectedItem = newTab;
        }

        // Hàm tạo Tab có chứa dữ liệu đơn nháp
        private void AddNewTab(Guid draftId, string headerTitle)
        {
            var newTab = new TabViewItem
            {
                Header = headerTitle,
                IconSource = new SymbolIconSource { Symbol = Symbol.Document }
            };

            var contentControl = new OrderTabContentControl();
            // đăng ký sự kiện đóng tab để ép lưu dữ liệu nháp nếu có
            contentControl.CloseTabRequested += ContentControl_CloseTabRequested;

            bool isDraftLoaded = false;

            // Đợi UI load xong mới đổ data vào giỏ hàng
            contentControl.Loaded += async (s, e) =>
            {
                if (!isDraftLoaded)
                {
                    isDraftLoaded = true; // Đánh dấu là đã load rồi
                    await contentControl.LoadDraftDataAsync(draftId);
                }
            };

            newTab.Content = contentControl;
            OrderTabView.TabItems.Add(newTab);

            // Focus vào tab nháp đầu tiên (hoặc tab vừa thêm)
            OrderTabView.SelectedItem = newTab;
        }

        private void ContentControl_CloseTabRequested(object sender, EventArgs e)
        {
            var contentControl = sender as OrderTabContentControl;
            TabViewItem tabToRemove = null;

            // Tìm xem TabViewItem nào đang chứa cái lõi contentControl này
            foreach (TabViewItem tab in OrderTabView.TabItems)
            {
                if (tab.Content == contentControl)
                {
                    tabToRemove = tab;
                    break;
                }
            }

            // Nếu tìm thấy thì xóa nó đi
            if (tabToRemove != null)
            {
                OrderTabView.TabItems.Remove(tabToRemove);

                // Nếu xóa xong mà hết sạch Tab thì quay về màn hình trước (giống nút X)
                if (OrderTabView.TabItems.Count == 0 && Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
            }
        }

        private void OrderTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            // Lưu ý: Đóng tab thì đơn nháp vẫn còn ở DB
            sender.TabItems.Remove(args.Tab);

            if (sender.TabItems.Count == 0 && Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}