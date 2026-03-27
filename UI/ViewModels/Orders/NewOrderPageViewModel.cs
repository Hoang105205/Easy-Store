using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls; // Dùng cho ContentDialog
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UI.Services.CategoryService;
using UI.Services.OrderService;
using UI.Services.ProductService;
using UI.ViewModels.Product;
namespace UI.ViewModels.Orders
{
    // Model đại diện cho 1 dòng sản phẩm trong giỏ hàng
    public partial class CartItemModel : ObservableObject
    {
        [ObservableProperty] private int stt;
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public long UnitPrice { get; set; }

        // Khi số lượng thay đổi, cần báo cho UI biết TotalPrice cũng thay đổi theo
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPrice))]
        private int quantity = 1;

        public long TotalPrice => Quantity * UnitPrice;
    }

    public partial class NewOrderPageViewModel : ObservableObject
    {
        private readonly OrderService _orderService;
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherTimer _autoSaveTimer;

        // --- Dữ liệu Giỏ hàng ---
        public ObservableCollection<CartItemModel> CartItems { get; } = new();
        [ObservableProperty] private string receiptNumber = "Hóa đơn mới";
        [ObservableProperty] private DateTimeOffset orderDate = DateTimeOffset.Now;
        [ObservableProperty] private string note = string.Empty;
        public Guid? CurrentDraftOrderId { get; private set; } = null;
        public long TotalAmount => CartItems.Sum(x => x.TotalPrice);
        private bool _isSaving = false;

        // --- Dữ liệu Sản phẩm & Danh mục ---
        public ObservableCollection<ProductModel> Products { get; } = new();
        public ObservableCollection<CategoryDropdownItem> Categories { get; } = new();

        [ObservableProperty] private string? searchProductText = string.Empty;
        [ObservableProperty] private CategoryDropdownItem? selectedCategory = null;

        // --- UI Interaction ---
        public XamlRoot? XamlRoot { get; set; }
        public Action? RequestCloseTabAction { get; set; } // Gọi ngược ra View để đóng tab

        private CancellationTokenSource? _debounceCts;
        private readonly int _debounceDelay = 500;

        public NewOrderPageViewModel()
        {
            _orderService = App.Current.Services.GetRequiredService<OrderService>();
            _productService = App.Current.Services.GetRequiredService<ProductService>();
            _categoryService = App.Current.Services.GetRequiredService<CategoryService>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _autoSaveTimer.Tick += async (s, e) => await ExecuteAutoSaveAsync();
        }

        // --- Các hàm lọc sản phẩm và danh mục ---
        public async Task LoadInitialDataAsync()
        {
            // Tải danh mục
            var cats = await _categoryService.GetCategoriesAsync();
            _dispatcherQueue.TryEnqueue(() =>
            {
                Categories.Clear();
                Categories.Add(new CategoryDropdownItem { Id = null, Name = "Tất cả danh mục" });
                foreach (var c in cats) Categories.Add(new CategoryDropdownItem { Id = c.Id, Name = c.Name });
            });

            // Tải tất cả sản phẩm lần đầu
            await LoadProductsFilteredAsync();
        }

        // Tự động tìm kiếm khi gõ chữ hoặc chọn danh mục
        partial void OnSearchProductTextChanged(string? value) => DebounceLoadProducts();
        partial void OnSelectedCategoryChanged(CategoryDropdownItem? value) => DebounceLoadProducts();

        private async void DebounceLoadProducts()
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            try
            {
                await Task.Delay(_debounceDelay, token);
                if (!token.IsCancellationRequested)
                {
                    await LoadProductsFilteredAsync();
                }
            }
            catch (TaskCanceledException) { }
        }

        private async Task LoadProductsFilteredAsync()
        {
            string? search = string.IsNullOrWhiteSpace(SearchProductText) ? null : SearchProductText;
            Guid? catId = (SelectedCategory?.Id != null && SelectedCategory.Id != Guid.Empty) ? SelectedCategory.Id : null;

            try
            {
                var result = await _productService.GetProductsAsync(search, catId);
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Products.Clear();
                    foreach (var p in result) Products.Add(p);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tải sản phẩm: {ex.Message}");
            }
        }

        // --- Các hàm quản lý giỏ hàng, lưu nháp và tạo đơn ---

        // Bắt sự kiện khi người dùng gõ Ghi chú để kích hoạt Auto-save
        partial void OnNoteChanged(string value) => TriggerAutoSave();

        [RelayCommand]
        public void AddProductToCart(ProductModel product)
        {
            if (product == null) return;
            var existingItem = CartItems.FirstOrDefault(x => x.ProductId == product.Id);
            if (existingItem != null)
            {
                existingItem.Quantity++;
                existingItem.UnitPrice = product.SalePrice ?? 0;
            }
            else
            {
                var newItem = new CartItemModel
                {
                    Stt = CartItems.Count + 1,
                    ProductId = product.Id,
                    ProductName = product.Name ?? string.Empty,
                    UnitPrice = product.SalePrice ?? 0,
                    Quantity = 1
                };
                newItem.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(CartItemModel.Quantity)) UpdateCartTotal(); };
                CartItems.Add(newItem);
            }
            UpdateCartTotal();
        }

        [RelayCommand]
        public void RemoveItem(CartItemModel item)
        {
            if (item == null) return;
            CartItems.Remove(item);
            for (int i = 0; i < CartItems.Count; i++) CartItems[i].Stt = i + 1;
            UpdateCartTotal();
        }

        public void UpdateCartTotal()
        {
            OnPropertyChanged(nameof(TotalAmount));
            TriggerAutoSave();
        }

        // --- dialog & thanh toán ---
        [RelayCommand]
        public async Task CancelOrderAsync()
        {
            if (XamlRoot == null) return;

            var dialog = new ContentDialog
            {
                Title = "Xác nhận hủy",
                Content = "Bạn có chắc chắn muốn hủy đơn hàng này không? Dữ liệu giỏ hàng sẽ bị xóa.",
                PrimaryButtonText = "Đồng ý",
                CloseButtonText = "Không",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await CancelOrderLogicAsync();
                RequestCloseTabAction?.Invoke();
            }
        }

        [RelayCommand]
        public async Task FinalizeOrderAsync()
        {
            if (XamlRoot == null) return;
            if (CartItems.Count == 0)
            {
                await ShowSimpleDialog("Thất bại", "Giỏ hàng đang trống!");
                return;
            }

            try
            {
                if (_autoSaveTimer.IsEnabled) await ExecuteAutoSaveAsync();
                if (CurrentDraftOrderId == null)
                {
                    await ShowSimpleDialog("Thất bại", "Chưa tạo được đơn nháp để thanh toán.");
                    return;
                }

                bool isSuccess = await _orderService.FinalizeOrderAsync(CurrentDraftOrderId.Value);
                if (isSuccess)
                {
                    ResetCart();
                    await ShowSimpleDialog("Thành công", "Tạo đơn hàng thành công!");
                    RequestCloseTabAction?.Invoke();
                }
                else
                {
                    await ShowSimpleDialog("Thất bại", "Lỗi khi tạo đơn hàng.");
                }
            }
            catch (Exception ex)
            {
                await ShowSimpleDialog("Lỗi", ex.Message);
            }
        }

        private async Task ShowSimpleDialog(string title, string content)
        {
            var dialog = new ContentDialog { Title = title, Content = content, CloseButtonText = "Đóng", XamlRoot = this.XamlRoot };
            await dialog.ShowAsync();
        }

        // --- AutoSave ---
        private void TriggerAutoSave()
        {
            // Mỗi khi có thay đổi, reset lại Timer
            _autoSaveTimer.Stop();
            // Chỉ bắt đầu đếm giờ lưu nháp nếu có ít nhất 1 sản phẩm
            if (CartItems.Count > 0) _autoSaveTimer.Start();
        }

        // hàm map Data để tái sử dụng
        private List<DraftOrderItemInput> GetCartItemInputs() =>
            CartItems.Select(c => new DraftOrderItemInput
            {
                ProductId = c.ProductId,
                Quantity = c.Quantity,
                UnitSalePrice = c.UnitPrice
            }).ToList();

        private async Task ExecuteAutoSaveAsync()
        {
            if (_isSaving) return;
            _isSaving = true;
            _autoSaveTimer.Stop(); // Dừng timer để tránh gọi lặp

            if (CartItems.Count == 0)
            {
                _isSaving = false;
                return;
            }

            try
            {
                var result = await _orderService.UpsertDraftOrderAsync(CurrentDraftOrderId, Note, GetCartItemInputs());

                if (result != null)
                {
                    CurrentDraftOrderId = result.Id;
                    // Cập nhật mã hóa đơn từ Server trả về (nếu là đơn mới tạo), cái này xài thấy hơi kỳ, có gì ko cần thì xóa sau
                    ReceiptNumber = "Hóa đơn #" + result.ReceiptNumber;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTO-SAVE LỖI]: {ex.Message}");
            }
            finally
            {
                _isSaving = false;
            }
        }

        private async Task CancelOrderLogicAsync()
        {
            _autoSaveTimer.Stop();
            if (CurrentDraftOrderId.HasValue) await _orderService.DeleteOrderAsync(CurrentDraftOrderId.Value);
            ResetCart();
        }

        // Dọn dẹp giao diện về trạng thái ban đầu
        private void ResetCart()
        {
            CurrentDraftOrderId = null;
            CartItems.Clear();
            ReceiptNumber = "Hóa đơn mới";
            Note = string.Empty;
            OrderDate = DateTimeOffset.Now;
            UpdateCartTotal();
        }

        public async Task LoadExistingDraftOrderAsync(Guid draftOrderId)
        {
            try
            {
                var draftDetail = await _orderService.GetOrderByIdAsync(draftOrderId);
                if (draftDetail == null) return;

                _autoSaveTimer.Stop();
                _dispatcherQueue.TryEnqueue(() => {
                    CurrentDraftOrderId = draftDetail.Id;
                    ReceiptNumber = "Hóa đơn #" + draftDetail.ReceiptNumber;
                    Note = draftDetail.Note != "Không có ghi chú" ? draftDetail.Note : string.Empty;
                    OrderDate = draftDetail.OrderDate;

                    CartItems.Clear();
                    foreach (var item in draftDetail.OrderItems)
                    {
                        var cartItem = new CartItemModel { Stt = item.STT, ProductId = item.ProductId, ProductName = item.ProductName, UnitPrice = item.UnitSalePrice, Quantity = item.Quantity };
                        cartItem.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(CartItemModel.Quantity)) UpdateCartTotal(); };
                        CartItems.Add(cartItem);
                    }
                    UpdateCartTotal();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI LOAD ĐƠN NHÁP]: {ex.Message}");
            }
        }

        public void ForceSaveIfNeeded()
        {
            if (_autoSaveTimer.IsEnabled)
            {
                _autoSaveTimer.Stop();
                if (CartItems.Count == 0) return;

                // Trích xuất dữ liệu ngay lập tức trên UI Thread
                var currentOrderId = CurrentDraftOrderId;
                var note = Note;
                var inputs = GetCartItemInputs();

                // Ném việc gọi mạng sang Thread Pool để UI Unload thoải mái không bị ngắt Request
                Task.Run(async () =>
                {
                    try
                    {
                        await _orderService.UpsertDraftOrderAsync(currentOrderId, note, inputs);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AUTO-SAVE LỖI]: {ex.Message}");
                    }
                });
            }
        }
    }
}