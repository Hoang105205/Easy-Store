using Api.GraphQL.Queries;
using Core.Data;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.GraphQL.Queries;

public class OrderQueriesTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetOrders_NenTraVeDonHangChinhThuc_VaSapXepGiamDanTheoNgay()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var queries = new OrderQueries();

        context.Orders.Add(new Order { Id = Guid.NewGuid(), IsDraft = true }); // Đơn nháp (bị loại)
        context.Orders.Add(new Order { Id = Guid.NewGuid(), IsDraft = false, OrderDate = new DateTime(2026, 4, 10) });
        context.Orders.Add(new Order { Id = Guid.NewGuid(), IsDraft = false, OrderDate = new DateTime(2026, 4, 15) });
        await context.SaveChangesAsync();

        // Act
        var result = queries.GetOrders(context).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, o => Assert.False(o.IsDraft));
        Assert.True(result[0].OrderDate > result[1].OrderDate); // Đảm bảo sắp xếp giảm dần theo ngày
    }

    [Fact]
    public async Task GetDraftOrders_NenTraVeChiDonNhap()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var queries = new OrderQueries();

        context.Orders.Add(new Order { Id = Guid.NewGuid(), IsDraft = true });
        context.Orders.Add(new Order { Id = Guid.NewGuid(), IsDraft = false });
        await context.SaveChangesAsync();

        // Act
        var result = queries.GetDraftOrders(context).ToList();

        // Assert
        Assert.Single(result);
        Assert.True(result.First().IsDraft);
    }

    [Fact]
    public async Task GetOrderById_NenTraVeDungDonHang()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var queries = new OrderQueries();
        var orderId = Guid.NewGuid();

        context.Orders.Add(new Order { Id = orderId });
        await context.SaveChangesAsync();

        // Act
        var result = queries.GetOrderById(orderId, context).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(orderId, result.First().Id);
    }

    // Hàm tiện ích tạo dữ liệu mẫu cho Pagination
    private async Task<(AppDbContext Context, Order O1, Order O2, Order O3)> SetupPaginationData()
    {
        var context = GetInMemoryDbContext();

        // Ép cứng ReceiptNumber để InMemory không gán trùng số 0
        var o1 = new Order { Id = Guid.NewGuid(), ReceiptNumber = 1001, IsDraft = false, OrderDate = new DateTime(2026, 4, 10) };
        var o2 = new Order { Id = Guid.NewGuid(), ReceiptNumber = 1002, IsDraft = false, OrderDate = new DateTime(2026, 4, 15) };
        var o3 = new Order { Id = Guid.NewGuid(), ReceiptNumber = 1003, IsDraft = false, OrderDate = new DateTime(2026, 4, 20) };
        var draft = new Order { Id = Guid.NewGuid(), ReceiptNumber = 1004, IsDraft = true, OrderDate = new DateTime(2026, 4, 15) };

        context.Orders.AddRange(o1, o2, o3, draft);
        await context.SaveChangesAsync();

        // Trả về context và các object để lấy được ReceiptNumber tự tăng do DB giả lập sinh ra
        return (context, o1, o2, o3);
    }

    [Fact]
    public async Task GetOrdersPagination_TruongHop1_KhongSearch_KhongFilter()
    {
        // Arrange
        var data = await SetupPaginationData();
        var queries = new OrderQueries();

        // Act
        // receiptNumber = null, startDate = null, endDate = null
        var result = queries.GetOrdersPagination(data.Context).ToList();

        // Assert
        Assert.Equal(3, result.Count); // Lấy tất cả 3 đơn chính thức
        // Kiểm tra sắp xếp giảm dần (thằng o3 ngày 20/4 phải nằm đầu)
        Assert.Equal(data.O3.Id, result[0].Id);
        Assert.Equal(data.O2.Id, result[1].Id);
        Assert.Equal(data.O1.Id, result[2].Id);
    }

    [Fact]
    public async Task GetOrdersPagination_TruongHop2_KhongSearch_CoFilter()
    {
        // Arrange
        var data = await SetupPaginationData();
        var queries = new OrderQueries();

        var startDate = new DateTime(2026, 4, 12);
        var endDate = new DateTime(2026, 4, 18);

        // Act
        // Lọc từ ngày 12 đến ngày 18 (Sẽ chỉ dính thằng o2 ngày 15)
        var result = queries.GetOrdersPagination(data.Context, null, startDate, endDate).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(data.O2.Id, result.First().Id);
    }

    [Fact]
    public async Task GetOrdersPagination_TruongHop3_CoSearch_KhongFilter()
    {
        // Arrange
        var data = await SetupPaginationData();
        var queries = new OrderQueries();
        string targetReceipt = data.O3.ReceiptNumber.ToString(); // Lấy mã của thằng o3

        // Act
        var result = queries.GetOrdersPagination(data.Context, targetReceipt, null, null).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(data.O3.Id, result.First().Id);
        Assert.Equal(data.O3.ReceiptNumber, result.First().ReceiptNumber);
    }

    [Fact]
    public async Task GetOrdersPagination_TruongHop4_CoSearch_CoFilter()
    {
        // Arrange
        var data = await SetupPaginationData();
        var queries = new OrderQueries();

        string targetReceipt = data.O2.ReceiptNumber.ToString();
        var startDate = new DateTime(2026, 4, 10);
        var endDate = new DateTime(2026, 4, 20);

        // Act
        // Tìm mã của thằng o2, đồng thời nằm trong khoảng 10/4 -> 20/4
        var result = queries.GetOrdersPagination(data.Context, targetReceipt, startDate, endDate).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(data.O2.Id, result.First().Id);

        // Case phụ: Search đúng mã o2, nhưng Filter sai khoảng thời gian -> Trả về rỗng
        var badStartDate = new DateTime(2026, 4, 18); // Sau ngày mua của o2
        var emptyResult = queries.GetOrdersPagination(data.Context, targetReceipt, badStartDate, endDate).ToList();
        Assert.Empty(emptyResult);
    }
}