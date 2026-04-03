using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;
using UI.ViewModels.Orders; 

namespace UI.Services.PrintService;

public class OrderReceiptDocument : IDocument
{
    private readonly OrderDetailModel _model;

    public OrderReceiptDocument(OrderDetailModel model)
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
                column.Item().Text("HÓA ĐƠN").FontSize(16).SemiBold();
                column.Item().Text($"Số: {_model.ReceiptNumber}");
                column.Item().Text($"Ngày: {_model.OrderDate:dd/MM/yyyy HH:mm}");

                // Trạng thái fix cứng là Đã thanh toán vì nút In chỉ hiện khi Paid
                column.Item().Text(text =>
                {
                    text.Span("Trạng thái: ");
                    text.Span("Đã thanh toán").SemiBold().FontColor(Colors.Green.Darken2);
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
            column.Item().PaddingTop(25).AlignRight().Text($"Tổng thành tiền: {_model.TotalAmount:N0} VNĐ")
                .FontSize(14).SemiBold();
        });
    }

    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(40);  // STT
                columns.RelativeColumn();    // Tên SP
                columns.ConstantColumn(80);  // Số lượng
                columns.ConstantColumn(100); // Đơn giá (Giá bán)
                columns.ConstantColumn(100); // Thành tiền
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("STT");
                header.Cell().Element(CellStyle).Text("Sản phẩm / SKU");
                header.Cell().Element(CellStyle).AlignRight().Text("Số lượng");
                header.Cell().Element(CellStyle).AlignRight().Text("Đơn giá");
                header.Cell().Element(CellStyle).AlignRight().Text("Thành tiền");

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                }
            });

            int index = 1;
            foreach (var item in _model.OrderItems)
            {
                table.Cell().Element(CellStyle).Text(index++.ToString());

                table.Cell().Element(CellStyle).Column(col =>
                {
                    col.Item().Text(item.ProductName).SemiBold();
                    col.Item().Text(item.ProductSku).FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.UnitSalePrice:N0} đ");
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