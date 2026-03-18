using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StrawberryShake;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.ViewModels.Import;

public partial class ImportEditorViewModel : ObservableObject
{
    private readonly IEasyStoreClient _client;
    private Guid _currentImportId; // Lưu lại ID của phiếu đang xem

    // === CÁC BIẾN BINDING RA GIAO DIỆN ===

    // (Lưu ý: Mình đang dùng cú pháp private chuẩn, nếu bạn dùng cách 1 lúc nãy thì đổi thành public partial nhé)
    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isDraft; // Biến quyết định sống còn việc ẩn/hiện 2 nút Thao tác

    [ObservableProperty]
    private string importIdText = string.Empty;

    [ObservableProperty]
    private string createdAtText = string.Empty;

    [ObservableProperty]
    private string statusText = string.Empty;

    // Danh sách phẳng hóa để đổ ra DataGrid
    public ObservableCollection<ImportDetailItemDto> Details { get; } = new();

    public ImportEditorViewModel(IEasyStoreClient client)
    {
        _client = client;
    }

    // === HÀM KHỞI TẠO DỮ LIỆU (Được gọi từ XAML.cs) ===
    public async Task InitializeAsync(Guid importId)
    {
        if (IsLoading) return;
        IsLoading = true;
        _currentImportId = importId;

        try
        {
            // Gọi hàm GraphQL lấy chi tiết 1 phiếu
            var result = await _client.GetImportById.ExecuteAsync(importId);

            if (result.IsSuccessResult() && result.Data?.ImportById != null)
            {
                var data = result.Data.ImportById;

                // 1. Cập nhật thông tin Header
                ImportIdText = data.Id.ToString();

                CreatedAtText = data.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

                // Ép Enum thành chữ ("Draft" hoặc "Completed") để XAML Converter vẫn hoạt động bình thường
                StatusText = data.Status.ToString();

                // 2. Logic cốt lõi: Đặt cờ IsDraft
                IsDraft = (data.Status == ImportStatus.Draft);

                // 3. Đổ dữ liệu chi tiết vào bảng
                Details.Clear();
                if (data.Details != null)
                {
                    foreach (var detail in data.Details)
                    {
                        var product = detail.Product;

                        // Xử lý lấy ảnh đại diện: Lấy ảnh IsPrimary, không có thì lấy ảnh đầu tiên, không có nữa thì lấy ảnh mặc định
                        var primaryImage = product?.Images?.FirstOrDefault(i => i.IsPrimary)?.ImagePath
                                        ?? product?.Images?.FirstOrDefault()?.ImagePath
                                        ?? "ms-appx:///Assets/default-product.png";

                        Details.Add(new ImportDetailItemDto
                        {
                            ProductName = product?.Name ?? "Sản phẩm không xác định",
                            ProductSku = product?.Sku ?? "N/A",
                            ProductImageUrl = primaryImage,
                            QuantityAdded = detail.QuantityAdded,
                            ActualImportPrice = detail.ActualImportPrice,
                            TotalPrice = detail.QuantityAdded * detail.ActualImportPrice // Tự tính thành tiền
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // TODO: Báo lỗi mạng
            System.Diagnostics.Debug.WriteLine($"Lỗi tải chi tiết phiếu nhập: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // === CÁC LỆNH (COMMANDS) CHO NÚT BẤM ===

    [RelayCommand]
    private async Task CompleteImportAsync()
    {
        // TODO: Gọi Mutation GraphQL để chốt phiếu. Chốt xong thì chuyển trạng thái và load lại trang
        System.Diagnostics.Debug.WriteLine("Bấm nút chốt hoàn thành phiếu: " + _currentImportId);
    }

    [RelayCommand]
    private async Task DeleteImportAsync()
    {
        // TODO: Hiện Dialog xác nhận. Nếu OK thì gọi Mutation xóa phiếu trên DB, sau đó quay về trang trước
        System.Diagnostics.Debug.WriteLine("Bấm nút xóa phiếu tạm: " + _currentImportId);
    }
}

// =========================================================
// CLASS DTO TRUNG GIAN DÙNG CHO DATAGRID
// Mục đích: Phẳng hóa dữ liệu từ GraphQL cho DataGrid dễ đọc
// =========================================================
public class ImportDetailItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string ProductImageUrl { get; set; } = string.Empty;
    public int QuantityAdded { get; set; }
    public decimal ActualImportPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
