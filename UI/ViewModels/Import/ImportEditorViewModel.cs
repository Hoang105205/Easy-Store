using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using StrawberryShake;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Services.ImportService;
using UI.Services.PrintService;

namespace UI.ViewModels.Import;

public partial class ImportEditorViewModel : ObservableObject
{
    private readonly ImportService _importService;
    private readonly PdfService _pdfService;
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

    public Action? GoBackAction { get; set; }

    // Danh sách phẳng hóa để đổ ra DataGrid
    public ObservableCollection<ImportDetailItemDto> Details { get; } = new();

    public ImportEditorViewModel(ImportService importService, PdfService pdfService)
    {
        _importService = importService;
        _pdfService = pdfService;
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
            var result = await _importService.GetImportByIdAsync(importId);

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
        // 1. Tạo hộp thoại xác nhận (Tránh người dùng lỡ tay bấm nhầm)
        var dialog = new ContentDialog
        {
            Title = "Xác nhận hoàn thành phiếu",
            Content = $"Bạn có chắc chắn muốn hoàn thành phiếu nhập này không?\nDữ liệu sẽ không thể thu hồi.",
            PrimaryButtonText = "Chắc chắn",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Close,

            XamlRoot = App.Current!.AppMainWindow!.Content.XamlRoot
        };

        // 2. Hiện hộp thoại lên và chờ người dùng bấm nút
        var confirmResult = await dialog.ShowAsync();

        if (confirmResult == ContentDialogResult.Primary)
        {

            try
            {
                IsLoading = true;

                var result = await _importService.MarkImportCompletedAsync(_currentImportId);

                if (result.IsSuccessResult())
                {
                    StatusText = ImportStatus.Completed.ToString();
                    IsDraft = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi chốt phiếu: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task DeleteImportAsync()
    {
        // 1. Tạo hộp thoại xác nhận (Tránh người dùng lỡ tay bấm nhầm)
        var dialog = new ContentDialog
        {
            Title = "Xác nhận xóa phiếu",
            Content = $"Bạn có chắc chắn muốn xóa phiếu nhập này không?\nDữ liệu sẽ bị xóa vĩnh viễn.",
            PrimaryButtonText = "Xóa",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Close, 

            XamlRoot = App.Current!.AppMainWindow!.Content.XamlRoot
        };

        // 2. Hiện hộp thoại lên và chờ người dùng bấm nút
        var confirmResult = await dialog.ShowAsync();

        // 3. Nếu người dùng chọn "Xóa" (PrimaryButton)
        if (confirmResult == ContentDialogResult.Primary)
        {
            try
            {
                IsLoading = true;

                // 4. Gọi API Mutation xóa phiếu từ Strawberry Shake
                var result = await _importService.DeleteImportAsync(_currentImportId);

                if (result.IsSuccessResult())
                {
                    // 5. Xóa thành công trên DB -> Lùi về trang danh sách
                    App.Current!.AppMainWindow!.DispatcherQueue.TryEnqueue(() =>
                    {
                        GoBackAction?.Invoke();
                    });
                }
                else
                {
                    // (Tùy chọn) Hiện Toast thông báo lỗi từ Backend nếu có
                    Debug.WriteLine("Xóa thất bại do lỗi từ Backend GraphQL.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi mạng khi xóa phiếu: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        IsLoading = true;

        try
        {
            // 1. Chuẩn bị dữ liệu từ các biến Bindings hiện có
            var data = new ImportReceiptData
            {
                ImportId = ImportIdText,
                CreatedAt = CreatedAtText,
                Status = StatusText,
                TotalAmount = Details.Sum(x => x.TotalPrice).ToString("N0") + " VNĐ", // Tính tổng tiền
                Details = Details.ToList()
            };

            // 2. Nhét dữ liệu vào Template
            var document = new ImportReceiptDocument(data);

            // 3. Gọi Service để xuất và mở file
            string fileName = $"PhieuNhap_{data.ImportId.Substring(0, 8)}.pdf";
            bool success = await _pdfService.GenerateAndOpenPdfAsync(document, fileName);

            if (!success)
            {
                // Báo lỗi (Dùng cách hiện dialog mà bạn đã biết)
                Debug.WriteLine("Có lỗi khi tạo PDF");
            }
        }
        finally
        {
            IsLoading = false;
        }
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
