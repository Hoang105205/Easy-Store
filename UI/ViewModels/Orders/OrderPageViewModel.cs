using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using UI.Services.OrderService;

namespace UI.ViewModels.Orders
{
    public static class OrderUIStatuses
    {
        public const string Created = "Created";
        public const string Paid = "Paid";
    }

    public class OrderModel
    {
        public Guid Id { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? Status { get; set; }
        public long? TotalAmount { get; set; }
        public long? TotalProfit { get; set; }
        public bool IsDraft { get; set; }
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
        [ObservableProperty] private int totalOrdersCount = 0;
        [ObservableProperty] private int draftOrdersCount = 0;

        // --- Các biến Binding cho Filter ---
        [ObservableProperty] private string? searchReceiptNumber = null;
        [ObservableProperty] private DateTimeOffset? startDate = null;
        [ObservableProperty] private DateTimeOffset? endDate = null;

        // --- Navigation Actions ---
        public Action? NavigateToAddOrderAction { get; set; }
        public Action<Guid>? NavigateToOrderDetailAction { get; set; }

        // --- Quản lý Cursor ---
        private string? currentEndCursor = null;
        private Stack<string> previousCursors = new();
        private bool pressedButton = false;

        private CancellationTokenSource? _debounceCts;
        private CancellationTokenSource? _loadCts;
        private readonly int _debounceDelay = 500;

        public OrderPageViewModel()
        {
            _orderService = App.Current.Services.GetRequiredService<OrderService>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        // Kích hoạt Debounce mỗi khi các thuộc tính tìm kiếm thay đổi
        partial void OnSearchReceiptNumberChanged(string? value) => DebounceLoadOrders();
        partial void OnStartDateChanged(DateTimeOffset? value) => DebounceLoadOrders();
        partial void OnEndDateChanged(DateTimeOffset? value) => DebounceLoadOrders();

        private async void DebounceLoadOrders()
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            try
            {
                await Task.Delay(_debounceDelay, token);

                if (!token.IsCancellationRequested)
                {
                    // Reset phân trang khi có thay đổi tìm kiếm
                    pressedButton = false;
                    await LoadOrdersAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // Bỏ qua lỗi khi task bị hủy
            }
        }

        // Hàm Tải dữ liệu chính
        public async Task LoadOrdersAsync(string? afterCursor = null)
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            var currentToken = _loadCts.Token;

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
                    SearchReceiptNumber,
                    StartDate,
                    EndDate);

                var drafts = await _orderService.GetDraftOrdersAsync();

                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (currentToken.IsCancellationRequested) return;

                    TotalOrdersCount = result.TotalCount;
                    DraftOrdersCount = drafts.Count;
                    Orders.Clear();

                    foreach (var item in result.Orders)
                    {
                        Orders.Add(item);
                    }

                    currentEndCursor = result.EndCursor;
                    CanGoNext = result.HasNextPage;
                });

                if (!currentToken.IsCancellationRequested)
                {
                    UpdateDisplayRangeText(itemsPerPage, result.Orders.Count);
                }
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

        // --- Xử lý Navigation & Commands ---
        [RelayCommand]
        public void AddOrder()
        {
            NavigateToAddOrderAction?.Invoke();
        }

        [RelayCommand]
        public void GoToOrderDetail(OrderModel selectedOrder)
        {
            if (selectedOrder != null)
            {
                NavigateToOrderDetailAction?.Invoke(selectedOrder.Id);
            }
        }

        [RelayCommand]
        public async Task NextPage()
        {
            if (currentEndCursor != null)
            {
                previousCursors.Push(currentEndCursor);
            }
            pressedButton = true;
            CurrentPageNumber++;
            CanGoPrevious = CurrentPageNumber > 1;

            await LoadOrdersAsync(afterCursor: currentEndCursor);
        }

        [RelayCommand]
        public async Task PreviousPage()
        {
            if (CurrentPageNumber > 1 && previousCursors.Count > 0)
            {
                pressedButton = true;
                CurrentPageNumber--;
                CanGoPrevious = CurrentPageNumber > 1;

                previousCursors.Pop();
                string? cursorToLoad = previousCursors.Count > 0 ? previousCursors.Peek() : null;
                await LoadOrdersAsync(afterCursor: cursorToLoad);
            }
        }

        public void CancelOperations()
        {
            _debounceCts?.Cancel();
            _loadCts?.Cancel();
        }
    }
}
