using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
            return string.Format(new CultureInfo("vi-VN"), formatString, value);
        }

        return value.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value == null) return null;

        var str = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(str)) return null;

        var culture = new CultureInfo("vi-VN");

        try
        {
            if (targetType == typeof(int) || targetType == typeof(int?))
            {
                str = str.Replace(".", "").Replace(",", "").Replace("đ", "").Trim();
                if (int.TryParse(str, NumberStyles.Any, culture, out var i))
                    return i;
            }

            if (targetType == typeof(long) || targetType == typeof(long?))
            {
                str = str.Replace(".", "").Replace(",", "").Replace("đ", "").Trim();
                if (long.TryParse(str, NumberStyles.Any, culture, out var l))
                    return l;
            }

            if (targetType == typeof(double) || targetType == typeof(double?))
            {
                str = str.Replace(".", "").Replace(",", "").Replace("đ", "").Trim();
                if (double.TryParse(str, NumberStyles.Any, culture, out var d))
                    return d;
            }

            if (targetType == typeof(decimal) || targetType == typeof(decimal?))
            {
                str = str.Replace(".", "").Replace(",", "").Replace("đ", "").Trim();
                if (decimal.TryParse(str, NumberStyles.Any, culture, out var m))
                    return m;
            }

            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            {
                if (DateTime.TryParse(str, culture, DateTimeStyles.AssumeLocal, out var dt))
                    return dt;
            }

            if (targetType == typeof(DateTimeOffset) || targetType == typeof(DateTimeOffset?))
            {
                if (DateTimeOffset.TryParse(str, culture, DateTimeStyles.AssumeLocal, out var dto))
                    return dto;
            }
        }
        catch
        {
            Debug.WriteLine("[LỖI PARSE] Không thể chuyển đổi '" + str + "' về " + targetType.Name);
        }

        return value;
    }
}
