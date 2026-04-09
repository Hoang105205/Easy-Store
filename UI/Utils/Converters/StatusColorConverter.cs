using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace UI.Utils.Converters;

public class StatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isBackground = parameter?.ToString() == "Background";
        SolidColorBrush brush;

        if (value is double percentage)
        {
            if (percentage > 0)
                brush = new SolidColorBrush(Colors.SeaGreen);
            else if (percentage < 0)
                brush = new SolidColorBrush(Colors.IndianRed);
            else
                brush = new SolidColorBrush(Colors.Gray);
        }
        else
        {
            var status = value?.ToString();
            if (status == "Completed" || status == "Hoàn thành" || status == "Paid" || status == "Đã thanh toán")
                brush = new SolidColorBrush(Colors.SeaGreen);
            else if (status == "Draft" || status == "Phiếu tạm" || status == "Created" || status == "Mới tạo")
                brush = new SolidColorBrush(Colors.DarkOrange);
            else
                brush = new SolidColorBrush(Colors.Gray);
        }

        if (isBackground)
        {
            brush.Opacity = 0.15;
        }

        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
