using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace UI.Utils.Converters;

public class StatusIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double percentage)
        {
            if (percentage > 0)
                return "\uE70E"; // Mũi tên hướng lên (ChevronUp)
            if (percentage < 0)
                return "\uE70D"; // Mũi tên hướng xuống (ChevronDown)

            return "\uE73E"; // Dấu gạch ngang nếu bằng 0 (ChromeMinimize)
        }

        var status = value?.ToString();
        if (status == "Completed" || status == "Hoàn thành")
            return "\uE73E"; // Mã Unicode của dấu Check (Hoàn thành)

        if (status == "Draft" || status == "Phiếu tạm")
            return "\uE7C3"; // Mã Unicode của cây bút/giấy nháp (Phiếu tạm)

        if (status == "Created" || status == "Mới tạo")
            return "\uE7C3"; // Mã Unicode của cây bút/giấy nháp (Mới tạo)

        if (status == "Paid" || status == "Đã thanh toán")
            return "\uE73E"; // Mã Unicode của dấu Check (Đã thanh toán)

        return "\uE9CE"; // Dấu chấm hỏi nếu không xác định
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
