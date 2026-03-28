using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UI.Services.OrderService;
using UI.ViewModels.Product;

namespace UI.ViewModels.Orders
{
    public class CartStockUpdateMessage
    {
        public Guid TabId { get; }
        public Guid ProductId { get; }
        public int StockChange { get; } // Dương = trả hàng về kho, Âm = lấy hàng khỏi kho

        public CartStockUpdateMessage(Guid tabId, Guid productId, int stockChange)
        {
            TabId = tabId;
            ProductId = productId;
            StockChange = stockChange;
        }
    }

    // Model đại diện cho 1 dòng sản phẩm trong giỏ hàng
    public partial class CartItemModel : ObservableObject
    {
        [ObservableProperty] private int stt;
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public long UnitPrice { get; set; }

        // Giới hạn số lượng tối đa có thể chọn
        [ObservableProperty] private int maxQuantity;

        // Khi số lượng thay đổi, cần báo cho UI biết TotalPrice cũng thay đổi theo
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPrice))]
        private int quantity = 1;

        public long TotalPrice => Quantity * UnitPrice;
        public Action<CartItemModel>? RemoveAction { get; set; }

        [RelayCommand]
        private void Remove() => RemoveAction?.Invoke(this);
    }

    public partial class NewOrderPageViewModel : ObservableObject
    {
        // Mỗi Tab sẽ có một instance của NewOrderPageViewModel, dùng TabId để phân biệt khi gửi tin nhắn cập nhật tồn kho
        public Guid TabId { get; } = Guid.NewGuid();

        private readonly OrderService _orderService;
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

        // --- UI Interaction ---
        public XamlRoot? XamlRoot { get; set; }
        public Action? RequestCloseTabAction { get; set; } // Gọi ngược ra View để đóng tab

        // quản lý trạng thái Dialog
        private bool _isShowingDialog = false;

        public NewOrderPageViewModel(OrderService orderService)
        {
            _orderService = orderService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _autoSaveTimer.Tick += async (s, e) => await ExecuteAutoSaveAsync();

            // Nhận Messenger từ TAB KHÁC để chỉnh lại MaxQuantity của chính mình (nếu tab khác mua mất hàng)
            WeakReferenceMessenger.Default.Register<CartStockUpdateMessage>(this, (r, m) =>
            {
                if (m.TabId != this.TabId)
                {
                    var cartItem = CartItems.FirstOrDefault(x => x.ProductId == m.ProductId);
                    if (cartItem != null)
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            cartItem.MaxQuantity += m.StockChange;
                            if (cartItem.MaxQuantity <= 0) RemoveItem(cartItem);
                            else if (cartItem.Quantity > cartItem.MaxQuantity) cartItem.Quantity = cartItem.MaxQuantity;
                        });
                    }
                }
            });
        }

        public void Initialize(OrderModel draftOrder)
        {
            CurrentDraftOrderId = draftOrder.Id;
            ReceiptNumber = "Hóa đơn #" + draftOrder.ReceiptNumber;
            OrderDate = draftOrder.OrderDate ?? DateTimeOffset.Now;
        }

        // Nhận Item từ Container khi Double Click
        public async void HandleAddProductFromGlobal(ProductModel product)
        {
            if (product.AvailableStockQuantity <= 0) return;
            if (CartItems.Any(c => c.ProductId == product.Id)) return; // Đã có thì không làm gì thêm

            var newItem = new CartItemModel
            {
                Stt = CartItems.Count + 1,
                ProductId = product.Id,
                ProductName = product.Name ?? "",
                UnitPrice = product.SalePrice ?? 0,
                Quantity = 1,
                MaxQuantity = product.AvailableStockQuantity ?? 0
            };
            newItem.RemoveAction = this.RemoveItem;

            CartItems.Add(newItem);
            UpdateCartTotal();

            // Trừ kho Global
            WeakReferenceMessenger.Default.Send(new CartStockUpdateMessage(this.TabId, product.Id, -1));
        }

        // Xử lý khi đổi số lượng bằng NumberBox
        public async Task<int> HandleQuantityChangedAsync(CartItemModel item, int newQuantity)
        {
            if (newQuantity < 1) newQuantity = 1;

            // Kiểm tra vượt quá tồn kho
            if (newQuantity > item.MaxQuantity)
            {
                if (!_isShowingDialog)
                {
                    _isShowingDialog = true;
                    try
                    {
                        await ShowLimitWarningDialogAsync(item.ProductName, item.MaxQuantity);
                    }
                    finally { _isShowingDialog = false; }
                }

                // Trả về mức tối đa cho phép để UI biết đường tự Rollback
                return item.MaxQuantity;
            }

            // Nếu số lượng hợp lệ và có sự thay đổi -> Tiến hành trừ kho và lưu Data
            if (item.Quantity != newQuantity)
            {
                int diff = item.Quantity - newQuantity; // cũ 1, mới 3 => diff = -2 (trừ 2 kho global)
                item.Quantity = newQuantity;
                UpdateCartTotal();

                WeakReferenceMessenger.Default.Send(new CartStockUpdateMessage(this.TabId, item.ProductId, diff));
                TriggerAutoSave();
            }

            // Trả về số lượng đã được chấp nhận
            return newQuantity;
        }

        // Bấm icon Thùng rác
        public void RemoveItem(CartItemModel item)
        {
            if (item == null) return;

            // Hoàn trả kho Global
            WeakReferenceMessenger.Default.Send(new CartStockUpdateMessage(this.TabId, item.ProductId, item.Quantity));

            CartItems.Remove(item);
            for (int i = 0; i < CartItems.Count; i++) CartItems[i].Stt = i + 1;
            UpdateCartTotal();
            TriggerAutoSave();
        }

        partial void OnNoteChanged(string value) => TriggerAutoSave();

        public void UpdateCartTotal()
        {
            OnPropertyChanged(nameof(TotalAmount));
        }

        // hủy đơn hàng: trả toàn bộ hàng về kho global, xóa draft order nếu có, đóng tab
        [RelayCommand]
        public async Task CancelOrderAsync()
        {
            if (XamlRoot == null) return;

            var dialog = new ContentDialog { Title = "Xác nhận hủy", Content = "Hủy đơn hàng này? Dữ liệu sẽ bị xóa.", PrimaryButtonText = "Đồng ý", CloseButtonText = "Không", XamlRoot = this.XamlRoot };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                // Trả toàn bộ hàng lại kho Global
                foreach (var item in CartItems)
                {
                    WeakReferenceMessenger.Default.Send(new CartStockUpdateMessage(this.TabId, item.ProductId, item.Quantity));
                }

                _autoSaveTimer.Stop();
                if (CurrentDraftOrderId.HasValue) await _orderService.DeleteOrderAsync(CurrentDraftOrderId.Value);

                RequestCloseTabAction?.Invoke(); // Đóng Tab
            }
        }

        // tạo đơn hàng: cập nhật lại thông tin đầy đủ của đơn hàng, lưu lại, sau đó đóng tab
        [RelayCommand]
        public async Task FinalizeOrderAsync()
        {
            if (XamlRoot == null) return;
            if (CartItems.Count == 0) { await ShowSimpleDialog("Lỗi", "Giỏ hàng trống!"); return; }

            _autoSaveTimer.Stop();
            while (_isSaving) await Task.Delay(50); // Chờ lưu ngầm xong
            await ExecuteAutoSaveAsync(); // Ép lưu lần cuối

            if (CurrentDraftOrderId == null) return;

            try
            {
                bool isSuccess = await _orderService.FinalizeOrderAsync(CurrentDraftOrderId.Value);
                if (isSuccess)
                {
                    await ShowSimpleDialog("Thành công", "Tạo đơn hàng thành công!");

                    RequestCloseTabAction?.Invoke(); // Đóng Tab
                }
            }
            catch (Exception ex) { await ShowSimpleDialog("Lỗi", ex.Message); }
        }

        public async Task ShowLimitWarningDialogAsync(string productName, int maxQuantity)
        {
            await ShowSimpleDialog("Vượt giới hạn", $"Sản phẩm '{productName}' chỉ còn tối đa {maxQuantity} sản phẩm khả dụng.");
        }

        private async Task ShowSimpleDialog(string title, string content)
        {
            if (XamlRoot == null) return;
            var dialog = new ContentDialog { Title = title, Content = content, CloseButtonText = "Đóng", XamlRoot = this.XamlRoot };
            await dialog.ShowAsync();
        }

        // --- AutoSave ---
        private void TriggerAutoSave()
        {
            _autoSaveTimer.Stop();
            if (CartItems.Count >= 0) _autoSaveTimer.Start();
        }

        private async Task ExecuteAutoSaveAsync()
        {
            if (_isSaving || !CurrentDraftOrderId.HasValue) return;
            _isSaving = true;
            _autoSaveTimer.Stop();

            try
            {
                var inputs = CartItems.Select(c => new DraftOrderItemInput { 
                    ProductId = c.ProductId, 
                    Quantity = c.Quantity, 
                    UnitSalePrice = c.UnitPrice 
                }).ToList();
                await _orderService.UpsertDraftOrderAsync(CurrentDraftOrderId, Note, inputs);
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"[AutoSave Error]: {ex.Message}");
            }
            finally { _isSaving = false; }
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
                        var cartItem = new CartItemModel
                        {
                            Stt = item.STT,
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            UnitPrice = item.UnitSalePrice,
                            Quantity = item.Quantity,
                            MaxQuantity = item.Quantity + item.AvailableStockQuantity,
                        };
                        cartItem.RemoveAction = this.RemoveItem;
                        CartItems.Add(cartItem);
                    }
                    UpdateCartTotal();
                });
            }
            catch { }
        }

        public void ForceSaveIfNeeded()
        {
            if (_autoSaveTimer.IsEnabled && CurrentDraftOrderId.HasValue)
            {
                _autoSaveTimer.Stop();
                var currentOrderId = CurrentDraftOrderId;
                var note = Note;
                var inputs = CartItems.Select(c => new DraftOrderItemInput { ProductId = c.ProductId, Quantity = c.Quantity, UnitSalePrice = c.UnitPrice }).ToList();

                Task.Run(async () => await _orderService.UpsertDraftOrderAsync(currentOrderId, note, inputs));
            }
        }

        public void Cleanup()
        {
            _autoSaveTimer.Stop();
            WeakReferenceMessenger.Default.UnregisterAll(this); // Ngắt kết nối để tránh memory leak
        }
    }
}