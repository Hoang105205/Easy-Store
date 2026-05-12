using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace UI.Utils.Converters;

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string activeColumn)
        {
            string currentColumn = parameter?.ToString() ?? string.Empty;

            return activeColumn == currentColumn ? Visibility.Visible : Visibility.Collapsed;
        }

        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }

        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
