using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UI.Services.OrderService;

namespace UI.ViewModels.Orders
{
    // Model đại diện cho 1 Tab trong giao diện
    public partial class OrderTabItemModel : ObservableObject
    {
        [ObservableProperty] private string header = "Đơn mới";
        public Guid? DraftId { get; set; }
        public Action? RequestCloseAction { get; set; }
    }

    public partial class NewOrderContainerViewModel : ObservableObject
    {
        private readonly OrderService _orderService;
        private int _tabCounter = 0;

        // Danh sách các Tab
        public ObservableCollection<OrderTabItemModel> Tabs { get; } = new();

        // Tab đang được chọn
        [ObservableProperty] private OrderTabItemModel? selectedTab;

        // quay lại trang trước
        public Action? NavigateBackAction { get; set; }

        public NewOrderContainerViewModel()
        {
            _orderService = App.Current.Services.GetRequiredService<OrderService>();
        }

        public async Task InitializeTabsAsync()
        {
            Tabs.Clear();
            _tabCounter = 0;

            try
            {
                var drafts = await _orderService.GetDraftOrdersAsync();

                if (drafts != null && drafts.Count > 0)
                {
                    foreach (var draft in drafts)
                    {
                        var newTab = new OrderTabItemModel
                        {
                            Header = "Hóa đơn #" + draft.ReceiptNumber,
                            DraftId = draft.Id
                        };
                        newTab.RequestCloseAction = () => CloseTab(newTab);
                        Tabs.Add(newTab);
                    }
                }
                else
                {
                    AddEmptyTab();
                }

                if (Tabs.Count > 0)
                {
                    SelectedTab = Tabs[0];
                }
            }
            catch
            {
                AddEmptyTab(); // Fallback nếu lỗi mạng
            }
        }

        [RelayCommand]
        public void AddEmptyTab()
        {
            _tabCounter++;
            var newTab = new OrderTabItemModel
            {
                Header = $"Đơn mới {_tabCounter}",
                DraftId = null
            };

            // Đăng ký sự kiện đóng tab
            newTab.RequestCloseAction = () => CloseTab(newTab);

            Tabs.Add(newTab);
            SelectedTab = newTab;
        }

        [RelayCommand]
        public void CloseTab(OrderTabItemModel tabToRemove)
        {
            if (tabToRemove != null && Tabs.Contains(tabToRemove))
            {
                Tabs.Remove(tabToRemove);

                // Nết tắt hết tab thì tự động lùi trang
                if (Tabs.Count == 0)
                {
                    NavigateBackAction?.Invoke();
                }
            }
        }
    }
}