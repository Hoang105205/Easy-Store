using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UI.Services.OrderService;
using UI.Services.PrintService;
using Microsoft.UI.Xaml; // nhận XamlRoot
using Microsoft.UI.Xaml.Controls; // gọi ContentDialog

namespace UI.ViewModels.Orders;

public class OrderDetailModel
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long TotalAmount { get; set; }
    public long TotalProfit { get; set; }
    public long TotalImportPrice { get; set; }
    public bool IsDraft { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTimeOffset OrderDate { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<OrderItemDetailModel> OrderItems { get; set; } = new();
}

public class OrderItemDetailModel
{
    public int STT { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public long UnitSalePrice { get; set; }
    public long UnitImportPrice { get; set; }
    public long TotalPrice { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int AvailableStockQuantity { get; set; }
}

public partial class OrderDetailPageViewModel : ObservableObject
{
    private readonly OrderService _orderService;
    private readonly PdfService _pdfService;
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty] private OrderDetailModel? orderDetail = new OrderDetailModel();
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isActionVisible;
    [ObservableProperty] private bool isPrintVisible;

    // --- Xử lý giao tiếp với View ---
    public Func<Task<bool>>? ConfirmPayAction { get; set; }
    public Func<Task<bool>>? ConfirmDeleteAction { get; set; }

    // --- Vẽ Dialog và chuyển trang ---
    public XamlRoot? XamlRoot { get; set; }
    public Action? NavigateBackAction { get; set; }

    public OrderDetailPageViewModel()
    {
        _orderService = App.Current.Services.GetRequiredService<OrderService>();
        _pdfService = App.Current.Services.GetRequiredService<PdfService>();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public async Task LoadOrderAsync(Guid orderId)
    {
        IsLoading = true;
        try
        {
            var data = await _orderService.GetOrderByIdAsync(orderId);
            _dispatcherQueue.TryEnqueue(() =>
            {
                OrderDetail = data;
                // Chỉ hiện nút Pay/Delete khi Status là "Created"
                IsActionVisible = data?.Status == OrderUIStatuses.Created;

                // Chỉ hiện nút khi Status là "Paid"
                IsPrintVisible = data?.Status == OrderUIStatuses.Paid;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LỖI] {ex.Message}");
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() => IsLoading = false);
        }
    }

    [RelayCommand]
    public void GoBack()
    {
        NavigateBackAction?.Invoke();
    }

    [RelayCommand]
    public async Task PayOrderAsync()
    {
        if (OrderDetail == null || XamlRoot == null) return;

        ContentDialog dialog = new ContentDialog
        {
            Title = "Xác nhận thanh toán",
            Content = "Bạn có chắc chắn muốn xác nhận thanh toán cho đơn hàng này?",
            PrimaryButtonText = "Thanh toán",
            CloseButtonText = "Hủy",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        try
        {
            var success = await _orderService.PayOrderAsync(OrderDetail.Id);
            if (success)
            {
                _dispatcherQueue.TryEnqueue(() => {
                    OrderDetail.Status = OrderUIStatuses.Paid;
                    IsActionVisible = false;
                    IsPrintVisible = true;
                    OnPropertyChanged(nameof(OrderDetail)); // Notify UI update
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LỖI THANH TOÁN] {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task DeleteOrderAsync()
    {
        if (OrderDetail == null || XamlRoot == null) return;

        ContentDialog dialog = new ContentDialog
        {
            Title = "Xác nhận xóa",
            Content = "Hành động này không thể hoàn tác. Bạn có chắc chắn muốn xóa đơn hàng này?",
            PrimaryButtonText = "Xóa",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        try
        {
            bool success = await _orderService.DeleteOrderAsync(OrderDetail.Id);
            if (success)
            {
                // Nếu xóa thành công, tự động quay về trang trước
                NavigateBackAction?.Invoke();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LỖI XÓA ĐƠN] {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ExportPdfAsync()
    {
        if (OrderDetail == null) return;

        IsLoading = true;
        try
        {
            var document = new OrderReceiptDocument(OrderDetail);
            string fileName = $"HoaDon_{OrderDetail.ReceiptNumber}.pdf";

            bool success = await _pdfService.GenerateAndOpenPdfAsync(document, fileName);

            if (!success)
            {
                System.Diagnostics.Debug.WriteLine("Có lỗi khi tạo PDF Hóa đơn");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}