using ClosedXML.Excel;
using System.IO;

namespace UI.Services.ExcelService;

public class ExcelService
{
    public void GenerateImportTemplate(Stream stream)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Template");

        // Header
        worksheet.Cell(1, 1).Value = "Mã SKU";
        worksheet.Cell(1, 2).Value = "Số lượng nhập";
        worksheet.Cell(1, 3).Value = "Giá nhập";

        var headerRange = worksheet.Range("A1:C1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Dữ liệu mẫu
        worksheet.Cell(2, 1).Value = "SKU-001";
        worksheet.Cell(2, 2).Value = 50;
        worksheet.Cell(2, 3).Value = 150000;

        var sampleRange = worksheet.Range("A2:C2");
        sampleRange.Style.Font.FontColor = XLColor.Gray;
        sampleRange.Style.Font.Italic = true;

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(stream);
    }
}
