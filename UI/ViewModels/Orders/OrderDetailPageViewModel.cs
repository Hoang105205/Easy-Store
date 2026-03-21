using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UI.Services.OrderService;

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
    public int Quantity { get; set; }
    public long UnitSalePrice { get; set; }
    public long UnitImportPrice { get; set; }
    public long TotalPrice { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
}

public partial class OrderDetailPageViewModel : ObservableObject
{
    private readonly OrderService _orderService;
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty] private OrderDetailModel? orderDetail = new OrderDetailModel();
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isActionVisible;

    public OrderDetailPageViewModel()
    {
        _orderService = App.Current.Services.GetRequiredService<OrderService>();
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
                // Chỉ hiện nút Pay/Delete khi Status là "Completed"
                IsActionVisible = data?.Status == "Completed";
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

    public async Task<bool> PayOrderAsync()
    {
        if (OrderDetail == null) return false;
        try
        {
            var success = await _orderService.PayOrderAsync(OrderDetail.Id);
            if (success)
            {
                _dispatcherQueue.TryEnqueue(() => {
                    OrderDetail.Status = "Paid";
                    IsActionVisible = false;
                    OnPropertyChanged(nameof(OrderDetail)); // Notify UI update
                });
            }
            return success;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteOrderAsync()
    {
        if (OrderDetail == null) return false;
        try
        {
            return await _orderService.DeleteOrderAsync(OrderDetail.Id);
        }
        catch { return false; }
    }
}