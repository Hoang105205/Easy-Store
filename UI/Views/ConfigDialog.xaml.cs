using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UI.Views;

public sealed partial class ConfigDialog : ContentDialog
{
    public string DbConnectionString => UrlInput.Text;

    public ConfigDialog(String currentUrl)
    {
        InitializeComponent();
        UrlInput.Text = currentUrl;

        UrlInput.TextChanged += (s, e) => {
            IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(UrlInput.Text);
        };
    }

    // Logic cho nút "Kiểm tra" (SecondaryButton)
    public async Task<bool> RunTestAsync()
    {
        StatusTextBlock.Visibility = Visibility.Visible;
        StatusTextBlock.Text = "Đang kiểm tra kết nối...";
        StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange);

        try
        {
            using var conn = new NpgsqlConnection(DbConnectionString);
            await conn.OpenAsync();

            StatusTextBlock.Text = "Kết nối thành công! Bạn có thể lưu cấu hình.";
            StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
            return true;
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Lỗi: {ex.Message}";
            StatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            return false;
        }
    }
}
