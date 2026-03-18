using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace UI.Utils.Converters.ImportPage;

public class StatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var status = value?.ToString();
        // Giả sử API trả về chữ "Completed" hoặc "Draft". (Bạn sửa lại cho khớp với chữ trong DB của bạn nhé)
        if (status == "Completed" || status == "Hoàn thành")
            return new SolidColorBrush(Colors.SeaGreen);

        if (status == "Draft" || status == "Phiếu tạm")
            return new SolidColorBrush(Colors.DarkOrange);

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
