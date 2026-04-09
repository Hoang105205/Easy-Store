using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using UI.ViewModels.Import;

namespace UI.Services.PrintService;

public class ImportReceiptData
{
    public string ImportId { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TotalAmount { get; set; } = string.Empty;
    public List<ImportDetailItemDto> Details { get; set; } = new();
}

public class ImportReceiptDocument : IDocument
{
    private readonly ImportReceiptData _model;

    public ImportReceiptDocument(ImportReceiptData model)
    {
        _model = model;
    }

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Margin(50);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("EASY STORE").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text("Hệ thống quản lý cửa hàng bán lẻ").FontSize(12).FontColor(Colors.Grey.Medium);
            });

            row.ConstantItem(150).AlignRight().Column(column =>
            {
                column.Item().Text("PHIẾU NHẬP KHO").FontSize(16).SemiBold();
                column.Item().Text($"Mã phiếu: {_model.ImportId.Substring(0, 8).ToUpper()}"); // Cắt ngắn GUID cho gọn
                column.Item().Text($"Ngày: {_model.CreatedAt}");

                // 1. Dùng Tuple để gán luôn cả Chữ và Màu cùng lúc
                var (displayStatus, statusColor) = _model.Status switch
                {
                    "Completed" => ("Hoàn thành", Colors.Green.Darken2),     // Màu Xanh lá đậm
                    "Draft" => ("Phiếu tạm (Lưu nháp)", Colors.Orange.Darken2), // Màu Cam đậm
                    _ => ("Không xác định", Colors.Black)
                };

                // 2. Dùng Span để tô màu riêng phần Trạng thái
                column.Item().Text(text =>
                {
                    text.Span("Trạng thái: "); // Chữ màu đen bình thường
                    text.Span(displayStatus).SemiBold().FontColor(statusColor); // Chữ trạng thái có màu
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Item().Element(ComposeTable);

            // Dòng tổng tiền ở dưới cùng
            column.Item().PaddingTop(25).AlignRight().Text($"Tổng thành tiền: {_model.TotalAmount}")
                .FontSize(14).SemiBold();
        });
    }

    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            // Định nghĩa 5 cột
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(40); // STT
                columns.RelativeColumn();   // Tên SP
                columns.ConstantColumn(80); // Số lượng
                columns.ConstantColumn(100); // Giá nhập
                columns.ConstantColumn(100); // Thành tiền
            });

            // Vẽ Header của bảng
            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("STT");
                header.Cell().Element(CellStyle).Text("Sản phẩm / SKU");
                header.Cell().Element(CellStyle).AlignRight().Text("Số lượng");
                header.Cell().Element(CellStyle).AlignRight().Text("Giá nhập");
                header.Cell().Element(CellStyle).AlignRight().Text("Thành tiền");

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                }
            });

            // Vẽ các dòng chi tiết
            int index = 1;
            foreach (var item in _model.Details)
            {
                table.Cell().Element(CellStyle).Text(index++.ToString());

                table.Cell().Element(CellStyle).Column(col =>
                {
                    col.Item().Text(item.ProductName).SemiBold();
                    col.Item().Text(item.ProductSku).FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                table.Cell().Element(CellStyle).AlignRight().Text(item.QuantityAdded.ToString());
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.ActualImportPrice:N0} đ");
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.TotalPrice:N0} đ");

                static IContainer CellStyle(IContainer container)
                {
                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Trang ");
            x.CurrentPageNumber();
            x.Span(" / ");
            x.TotalPages();
        });
    }
}
