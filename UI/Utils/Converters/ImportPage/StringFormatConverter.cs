using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace UI.Utils.Converters.ImportPage;

public class StringFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null) return string.Empty;

        // BÍ QUYẾT Ở ĐÂY: Nếu dữ liệu là ngày giờ, ép nó về giờ của máy tính (VN +7)
        if (value is DateTime dt)
        {
            value = dt.ToLocalTime();
        }
        else if (value is DateTimeOffset dto)
        {
            value = dto.ToLocalTime();
        }

        // Khúc dưới giữ nguyên
        if (parameter is string formatString)
        {
            if (!formatString.Contains("{0")) formatString = "{0:" + formatString + "}";
            return string.Format(formatString, value);
        }

        return value.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
