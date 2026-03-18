using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace UI.Utils.Converters.ImportPage;

public class StatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var status = value?.ToString();

        // Dùng switch expression (C# 8+) cho gọn và dễ đọc
        return status switch
        {
            "Completed" => "Hoàn thành",
            "Draft" => "Phiếu tạm",
            "Cancelled" => "Đã hủy", // Cứ làm dư ra một cái phòng hờ sau này bạn cần
            _ => status ?? "Không xác định" // Nếu có chữ lạ, cứ in nguyên bản ra
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
