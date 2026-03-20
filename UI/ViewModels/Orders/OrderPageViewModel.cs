using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UI.Services.OrderService;

namespace UI.ViewModels.Orders
{
    public class OrderModel
    {
        public Guid Id { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? Status { get; set; }
        public long? TotalAmount { get; set; }
        public long? TotalProfit { get; set; }
        public DateTimeOffset? OrderDate { get; set; }
    }

    public partial class OrderPageViewModel : ObservableObject
    {
        private readonly OrderService _orderService;
        private readonly DispatcherQueue _dispatcherQueue;

        public ObservableCollection<OrderModel> Orders { get; } = new();

        // --- Các biến UI ---
        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private int currentPageNumber = 1;
        [ObservableProperty] private bool canGoNext;
        [ObservableProperty] private bool canGoPrevious = false;
        [ObservableProperty] private string displayRangeText = string.Empty;

        // --- Quản lý Cursor ---
        private string? currentEndCursor = null;
        private Stack<string> previousCursors = new();
        private bool pressedButton = false;

        public OrderPageViewModel()
        {
            _orderService = App.Current.Services.GetRequiredService<OrderService>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        // Hàm Tải dữ liệu chính
        public async Task LoadOrdersAsync(
            string? afterCursor = null,
            string? receiptNumber = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null)
        {
            IsLoading = true;
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            int itemsPerPage = localSettings.Values["ItemsPerPage"] as int? ?? 10;

            if (!pressedButton)
            {
                CurrentPageNumber = 1;
                afterCursor = null;

                previousCursors.Clear();
                currentEndCursor = null;
                CanGoPrevious = false;
            }
            pressedButton = false;

            try
            {
                // Truyền trực tiếp các tham số filter từ Property của ViewModel
                var result = await _orderService.GetOrdersPaginationAsync(
                    itemsPerPage,
                    afterCursor,
                    receiptNumber,
                    startDate,
                    endDate);

                _dispatcherQueue.TryEnqueue(() =>
                {
                    Orders.Clear();
                    foreach (var item in result.Orders)
                    {
                        Orders.Add(item);
                    }

                    currentEndCursor = result.EndCursor;
                    CanGoNext = result.HasNextPage;
                });

                UpdateDisplayRangeText(itemsPerPage, result.Orders.Count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI LẤY ĐƠN HÀNG] {ex.Message}");
            }
            finally
            {
                _dispatcherQueue.TryEnqueue(() => IsLoading = false);
            }
        }

        public void UpdateDisplayRangeText(int itemsPerPage, int currentCount)
        {
            if (currentCount == 0)
            {
                DisplayRangeText = "Không có đơn hàng nào";
                return;
            }

            int startIndex = (CurrentPageNumber - 1) * itemsPerPage + 1;
            int endIndex = Math.Min(itemsPerPage * CurrentPageNumber, itemsPerPage * (CurrentPageNumber - 1) + currentCount);

            DisplayRangeText = $"Đang hiển thị đơn hàng từ {startIndex} - {endIndex}";
        }

        // --- Hàm Thực thi Tìm Kiếm (Gọi khi bấm Nút Tìm kiếm hoặc đổi ngày) ---
        public async Task SearchOrdersAsync()
        {
            // Reset toàn bộ state phân trang khi tìm kiếm mới
            currentEndCursor = null;
            previousCursors.Clear();
            pressedButton = false;
            CurrentPageNumber = 1;
            CanGoPrevious = false;

            await LoadOrdersAsync();
        }

        // --- Xử lý Nút Next / Prev ---
        public async Task NextPageAsync(string? receiptNumber = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            if (currentEndCursor != null)
            {
                previousCursors.Push(currentEndCursor);
            }
            pressedButton = true;
            CurrentPageNumber++;
            CanGoPrevious = CurrentPageNumber > 1;

            await LoadOrdersAsync(afterCursor: currentEndCursor, receiptNumber, startDate, endDate);
        }

        public async Task PreviousPageAsync(string? receiptNumber = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            if (CurrentPageNumber > 1 && previousCursors.Count > 0)
            {
                pressedButton = true;
                CurrentPageNumber--;
                CanGoPrevious = CurrentPageNumber > 1;

                previousCursors.Pop();
                string? cursorToLoad = previousCursors.Count > 0 ? previousCursors.Peek() : null;
                await LoadOrdersAsync(afterCursor: cursorToLoad, receiptNumber, startDate, endDate);
            }
        }
    }
}
