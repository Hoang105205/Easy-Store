using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UI.Services.CategoryService;
using UI.Services.OrderService;
using UI.Services.ProductService;
using UI.ViewModels.Product;

namespace UI.ViewModels.Orders
{
    // Model đại diện cho 1 Tab trong giao diện
    public partial class OrderTabItemModel : ObservableObject
    {
        [ObservableProperty] private string header = "Đang tạo...";
        public Guid? DraftId { get; set; }
        public NewOrderPageViewModel TabViewModel { get; set; }
        public Action? RequestCloseAction { get; set; }
    }

    public partial class NewOrderContainerViewModel : ObservableObject
    {
        private readonly OrderService _orderService;
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;

        [ObservableProperty] private bool isLoading = false;

        public ObservableCollection<ProductModel> Products { get; } = new();
        public ObservableCollection<CategoryDropdownItem> Categories { get; } = new();

        [ObservableProperty] private string? searchProductText = string.Empty;
        [ObservableProperty] private CategoryDropdownItem? selectedCategory = null;

        public ObservableCollection<OrderTabItemModel> Tabs { get; } = new();
        [ObservableProperty] private OrderTabItemModel? selectedTab;

        public Action? NavigateBackAction { get; set; }

        public NewOrderContainerViewModel()
        {
            _orderService = App.Current.Services.GetRequiredService<OrderService>();
            _productService = App.Current.Services.GetRequiredService<ProductService>();
            _categoryService = App.Current.Services.GetRequiredService<CategoryService>();

            // Lắng nghe sự kiện Tab trả lại kho hoặc mua thêm từ Cart ViewModel
            WeakReferenceMessenger.Default.Register<CartStockUpdateMessage>(this, (r, m) =>
            {
                var product = Products.FirstOrDefault(p => p.Id == m.ProductId);
                if (product != null)
                {
                    // Cập nhật giỏ search bên trái
                    DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                    {
                        product.AvailableStockQuantity += m.StockChange;
                    });
                }
            });
        }

        public async Task InitializeTabsAsync()
        {
            IsLoading = true;
            Tabs.Clear();
            await LoadInitialGlobalDataAsync(); // Load Categories & Products

            try
            {
                var drafts = await _orderService.GetDraftOrdersAsync();

                if (drafts != null && drafts.Count > 0)
                {
                    foreach (var draft in drafts)
                    {
                        var tabVm = App.Current.Services.GetRequiredService<NewOrderPageViewModel>();
                        tabVm.Initialize(draft);

                        var newTab = new OrderTabItemModel
                        {
                            Header = "Hóa đơn #" + draft.ReceiptNumber,
                            DraftId = draft.Id,
                            TabViewModel = tabVm
                        };
                        newTab.RequestCloseAction = () => CloseTab(newTab);
                        tabVm.RequestCloseTabAction = () => CloseTab(newTab);
                        Tabs.Add(newTab);

                        // Load chi tiết cho Tab
                        await tabVm.LoadExistingDraftOrderAsync(draft.Id);
                    }
                }
                else
                {
                    await AddEmptyTabAsync();
                }

                if (Tabs.Count > 0) SelectedTab = Tabs[0];
            }
            catch
            {
                await AddEmptyTabAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadInitialGlobalDataAsync()
        {
            var cats = await _categoryService.GetCategoriesAsync();
            Categories.Clear();
            Categories.Add(new CategoryDropdownItem { Id = null, Name = "Tất cả danh mục" });
            foreach (var c in cats) Categories.Add(new CategoryDropdownItem { Id = c.Id, Name = c.Name });

            await LoadProductsFilteredAsync();
        }

        partial void OnSearchProductTextChanged(string? value) => LoadProductsFilteredAsync();
        partial void OnSelectedCategoryChanged(CategoryDropdownItem? value) => LoadProductsFilteredAsync();

        private async Task LoadProductsFilteredAsync()
        {
            string? search = string.IsNullOrWhiteSpace(SearchProductText) ? null : SearchProductText;
            Guid? catId = (SelectedCategory?.Id != null && SelectedCategory.Id != Guid.Empty) ? SelectedCategory.Id : null;

            var result = await _productService.GetProductsAsync(search, catId);
            Products.Clear();
            foreach (var p in result) Products.Add(p);
        }

        [RelayCommand]
        public async Task AddEmptyTabAsync()
        {
            IsLoading = true;
            try
            {
                var newDraft = await _orderService.UpsertDraftOrderAsync(null, string.Empty, new());

                if (newDraft != null)
                {
                    // tạo ViewModel cho Tab mới
                    var tabVm = App.Current.Services.GetRequiredService<NewOrderPageViewModel>();
                    tabVm.Initialize(newDraft);

                    var newTab = new OrderTabItemModel
                    {
                        Header = "Hóa đơn #" + newDraft.ReceiptNumber,
                        TabViewModel = tabVm
                    };

                    // Đăng ký sự kiện đóng tab
                    tabVm.RequestCloseTabAction = () => CloseTab(newTab);
                    newTab.RequestCloseAction = () => CloseTab(newTab);

                    Tabs.Add(newTab);
                    SelectedTab = newTab;
                }
            }
            catch
            {
                // Xử lý lỗi nếu cần
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void CloseTab(OrderTabItemModel tabToRemove)
        {
            if (Tabs.Contains(tabToRemove))
            {
                tabToRemove.TabViewModel.Cleanup(); // Hủy các liên kết rác
                Tabs.Remove(tabToRemove);
                if (Tabs.Count == 0) NavigateBackAction?.Invoke();
            }
        }

        // Xử lý Double Click sản phẩm từ XAML
        public void HandleProductDoubleTapped(ProductModel selectedProduct)
        {
            if (SelectedTab?.TabViewModel == null) return;

            // Bắn sang cho ViewModel của Tab hiện tại xử lý
            SelectedTab.TabViewModel.HandleAddProductFromGlobal(selectedProduct);
        }
    }
}