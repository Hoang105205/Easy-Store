using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace UI.Utils.Converters;

public class StatusIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var status = value?.ToString();
        if (status == "Completed" || status == "Hoàn thành")
            return "\uE73E"; // Mã Unicode của dấu Check (Hoàn thành)

        if (status == "Draft" || status == "Phiếu tạm")
            return "\uE7C3"; // Mã Unicode của cây bút/giấy nháp (Phiếu tạm)

        return "\uE9CE"; // Dấu chấm hỏi nếu không xác định
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
