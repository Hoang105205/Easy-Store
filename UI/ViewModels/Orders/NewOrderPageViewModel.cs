using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml; // Chứa DispatcherTimer
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UI.Services.OrderService;

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
        private readonly DispatcherTimer _autoSaveTimer;

        // Danh sách sản phẩm trong giỏ hàng
        public ObservableCollection<CartItemModel> CartItems { get; } = new();

        [ObservableProperty] private string receiptNumber = "Hóa đơn mới"; // Hoặc sinh mã random
        [ObservableProperty] private DateTimeOffset orderDate = DateTimeOffset.Now;
        [ObservableProperty] private string note = string.Empty;

        // Biến lưu ID của đơn nháp hiện tại
        public Guid? CurrentDraftOrderId { get; private set; } = null;

        // Tổng tiền (Lấy tổng của tất cả TotalPrice)
        public long TotalAmount => CartItems.Sum(x => x.TotalPrice);
        // Cờ để tránh gọi lưu nháp liên tục khi đã đang trong quá trình lưu
        private bool _isSaving = false;

        public NewOrderPageViewModel()
        {
            _orderService = App.Current.Services.GetRequiredService<OrderService>();

            // Thiết lập Timer: Chờ 1 giây sau thao tác cuối cùng mới gọi API lưu nháp
            _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _autoSaveTimer.Tick += async (s, e) => await ExecuteAutoSaveAsync();
        }

        // Bắt sự kiện khi người dùng gõ Ghi chú để kích hoạt Auto-save
        partial void OnNoteChanged(string value) => TriggerAutoSave();

        private void AddAndTrackItem(CartItemModel newItem)
        {
            newItem.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CartItemModel.Quantity))
                {
                    UpdateCartTotal(); // Kích hoạt tính lại tổng tiền UI & AutoSave
                }
            };
            CartItems.Add(newItem);
        }

        public void AddProductToCart(Guid productId, string name, long price)
        {
            var existingItem = CartItems.FirstOrDefault(x => x.ProductId == productId);
            if (existingItem != null)
            {
                // Nếu đã có trong giỏ, cộng dồn số lượng
                existingItem.Quantity++;

                // cập nhật lại giá mới nhất (Đề phòng giá thay đổi khi app đang mở)
                existingItem.UnitPrice = price;
            }
            else
            {
                // Nếu chưa có, thêm dòng mới
                AddAndTrackItem(new CartItemModel
                {
                    Stt = CartItems.Count + 1,
                    ProductId = productId,
                    ProductName = name,
                    UnitPrice = price,
                    Quantity = 1
                });
                UpdateCartTotal(); // Gọi thủ công lần đầu khi add món mới
            }
        }

        public void RemoveItem(CartItemModel item)
        {
            CartItems.Remove(item);
            // Cập nhật lại STT cho các item còn lại
            for (int i = 0; i < CartItems.Count; i++)
            {
                CartItems[i].Stt = i + 1;
            }
            UpdateCartTotal();
        }

        public void UpdateCartTotal()
        {
            // Kích hoạt cập nhật giao diện cho thuộc tính TotalAmount
            OnPropertyChanged(nameof(TotalAmount));
            TriggerAutoSave(); // Gọi auto-save mỗi khi có thay đổi về giỏ hàng
        }

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

        public async Task<(bool IsSuccess, string Message)> FinalizeOrderAsync()
        {
            if (CartItems.Count == 0) return (false, "Giỏ hàng đang trống!");

            try
            {
                // Nếu Timer đang chạy (người dùng vừa gõ xong bấm Tạo luôn), ép lưu nháp ngay lập tức
                if (_autoSaveTimer.IsEnabled) await ExecuteAutoSaveAsync();

                if (CurrentDraftOrderId == null) return (false, "Chưa tạo được đơn nháp để thanh toán.");

                bool isSuccess = await _orderService.FinalizeOrderAsync(CurrentDraftOrderId.Value);
                if (isSuccess)
                {
                    ResetCart(); // Thành công thì dọn giỏ hàng
                    return (true, "Tạo đơn hàng thành công!");
                }
                return (false, "Lỗi khi tạo đơn hàng.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task CancelOrderAsync()
        {
            try
            {
                _autoSaveTimer.Stop();
                // Nếu đã lưu nháp trên server thì gọi API xóa
                if (CurrentDraftOrderId.HasValue)
                {
                    await _orderService.DeleteOrderAsync(CurrentDraftOrderId.Value);
                }
                ResetCart();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI HỦY ĐƠN]: {ex.Message}");
            }
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

        // Đổ dữ liệu từ đơn nháp đã lưu lên giao diện để người dùng tiếp tục chỉnh sửa
        public async Task LoadExistingDraftOrderAsync(Guid draftOrderId)
        {
            try
            {
                var draftDetail = await _orderService.GetOrderByIdAsync(draftOrderId);
                if (draftDetail == null) return;

                // Tạm tắt auto-save để tránh bị gọi đè xuống DB khi đang load UI
                _autoSaveTimer.Stop();

                CurrentDraftOrderId = draftDetail.Id;
                ReceiptNumber = "Hóa đơn #" + draftDetail.ReceiptNumber;
                Note = draftDetail.Note != "Không có ghi chú" ? draftDetail.Note : string.Empty;
                OrderDate = draftDetail.OrderDate;

                CartItems.Clear();
                foreach (var item in draftDetail.OrderItems)
                {
                    // Tái sử dụng hàm AddAndTrackItem để gắn Event PropertyChanged
                    AddAndTrackItem(new CartItemModel
                    {
                        Stt = item.STT,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        UnitPrice = item.UnitSalePrice,
                        Quantity = item.Quantity
                    });
                }
                UpdateCartTotal();
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