using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.ViewModels.Orders;

namespace UI.Services.OrderService
{
    internal class OrderService
    {
        private readonly IEasyStoreClient _client;

        public OrderService()
        {
            _client = App.Current.Services.GetRequiredService<IEasyStoreClient>();
        }

        public async Task<(List<OrderModel> Orders, string? EndCursor, bool HasNextPage, int TotalCount)> GetOrdersPaginationAsync(
            int itemsPerPage,
            string? afterCursor,
            string? receiptNumber = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null)
        {
            var result = await _client.GetOrdersPagination.ExecuteAsync(
                first: itemsPerPage,
                after: afterCursor,
                receiptNumber: receiptNumber,
                startDate: startDate,
                endDate: endDate
            );

            if (result.Errors?.Count > 0)
            {
                throw new Exception(result.Errors[0].Message);
            }

            var mappedData = result.Data?.OrdersPagination?.Nodes?.Select(x => new OrderModel
            {
                Id = x.Id,
                ReceiptNumber = x.ReceiptNumber.ToString(),
                Status = x.Status.ToString(),
                TotalAmount = x.TotalAmount,
                TotalProfit = x.TotalProfit,
                OrderDate = x.OrderDate
            }).ToList() ?? new List<OrderModel>();

            var pageInfo = result.Data?.OrdersPagination?.PageInfo;

            var totalCount = result.Data?.OrdersPagination?.TotalCount ?? 0;

            return (mappedData, pageInfo?.EndCursor, pageInfo?.HasNextPage ?? false, totalCount);
        }

        public async Task<OrderDetailModel?> GetOrderByIdAsync(Guid id)
        {
            var result = await _client.GetOrderById.ExecuteAsync(id);
            if (result.Errors?.Count > 0) throw new Exception(result.Errors[0].Message);

            var orderData = result.Data?.OrderById;
            if (orderData == null) return null;

            int sttCounter = 1;
            long totalImport = 0;
            var items = new List<OrderItemDetailModel>();

            if (orderData.OrderItems != null)
            {
                foreach (var item in orderData.OrderItems)
                {
                    long calcTotalPrice = item.Quantity * item.UnitSalePrice; // Thành tiền = sl x đơn giá, khi nào làm tạo đơn mới thì tính, cái này chỉ để hiển thị
                    totalImport += item.Quantity * (item.UnitImportPrice ?? 0);

                    items.Add(new OrderItemDetailModel
                    {
                        STT = sttCounter++,
                        Quantity = item.Quantity,
                        UnitSalePrice = item.UnitSalePrice,
                        UnitImportPrice = item.UnitImportPrice ?? 0,
                        TotalPrice = calcTotalPrice,
                        ProductName = item.Product?.Name ?? "Không rõ",
                        ProductSku = item.Product?.Sku ?? string.Empty,
                        CategoryName = item.Product?.Category?.Name ?? "Không rõ danh mục",
                    });
                }
            }

            return new OrderDetailModel
            {
                Id = orderData.Id,
                ReceiptNumber = orderData.ReceiptNumber.ToString(),
                Status = orderData.Status.ToString(),
                TotalAmount = orderData.TotalAmount,
                TotalProfit = orderData.TotalProfit,
                TotalImportPrice = totalImport,
                Note = string.IsNullOrWhiteSpace(orderData.Note) ? "Không có ghi chú" : orderData.Note,
                IsDraft = orderData.IsDraft,
                OrderDate = orderData.OrderDate,
                UpdatedAt = orderData.UpdatedAt,
                OrderItems = items
            };
        }

        public async Task<bool> PayOrderAsync(Guid id)
        {
            var result = await _client.PayOrder.ExecuteAsync(id);
            if (result.Errors?.Count > 0) throw new Exception(result.Errors[0].Message);
            return result.Data?.PayOrder != null;
        }

        public async Task<bool> DeleteOrderAsync(Guid id)
        {
            var result = await _client.DeleteOrder.ExecuteAsync(id);
            if (result.Errors?.Count > 0) throw new Exception(result.Errors[0].Message);
            return result.Data?.DeleteOrder ?? false;
        }

        public async Task<OrderModel?> UpsertDraftOrderAsync(Guid? orderId, string? note, List<DraftOrderItemInput> items)
        {
            var input = new UpsertDraftOrderInput
            {
                OrderId = orderId,
                Note = note,
                Items = items // Map dữ liệu từ UI Models sang định dạng Input sinh ra bởi StrawberryShake
            };

            var result = await _client.UpsertDraftOrder.ExecuteAsync(input);
            if (result.Errors?.Count > 0) throw new Exception(result.Errors[0].Message);

            var data = result.Data?.UpsertDraftOrder;
            if (data == null) return null;

            return new OrderModel
            {
                Id = data.Id,
                ReceiptNumber = data.ReceiptNumber.ToString(),
                TotalAmount = data.TotalAmount,
                IsDraft = data.IsDraft
            };
        }

        public async Task<bool> FinalizeOrderAsync(Guid orderId)
        {
            var result = await _client.FinalizeOrder.ExecuteAsync(orderId);
            if (result.Errors?.Count > 0) throw new Exception(result.Errors[0].Message);

            // Nếu kết quả trả về không null nghĩa là thành công
            return result.Data?.FinalizeOrder != null;
        }
    }
}
