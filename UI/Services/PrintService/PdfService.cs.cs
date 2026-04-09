using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace UI.Services.PrintService;

public class PdfService
{
    // Hàm này sau này có thể dùng chung để in Hóa đơn
    public async Task<bool> GenerateAndOpenPdfAsync(IDocument document, string fileName)
    {
        try
        {
            // 1. Tạo thư mục tạm để lưu file PDF
            var tempFolder = ApplicationData.Current.TemporaryFolder;
            var file = await tempFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            var filePath = file.Path;

            // 2. QuestPDF render thẳng vào đường dẫn này
            document.GeneratePdf(filePath);

            // 3. Gọi Windows mở file PDF này lên (Sẽ mở bằng Edge hoặc trình đọc PDF mặc định)
            var options = new Windows.System.LauncherOptions { DisplayApplicationPicker = false };
            await Windows.System.Launcher.LaunchFileAsync(file, options);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Lỗi tạo file PDF: {ex.Message}");
            return false;
        }
    }
}
