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
                Status = x.Status.ToString(), // Chuyển đổi Enum status sang chuỗi nếu cần
                TotalAmount = x.TotalAmount,
                TotalProfit = x.TotalProfit,
                OrderDate = x.OrderDate
            }).ToList() ?? new List<OrderModel>();

            var pageInfo = result.Data?.OrdersPagination?.PageInfo;

            var totalCount = result.Data?.OrdersPagination?.TotalCount ?? 0;

            return (mappedData, pageInfo?.EndCursor, pageInfo?.HasNextPage ?? false, totalCount);
        }
    }
}
