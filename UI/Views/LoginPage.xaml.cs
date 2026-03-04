using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace UI.Views;

public sealed partial class LoginPage : Page
{
    private bool _isPasswordVisible = false;

    public LoginPage()
    {
        InitializeComponent();
        
        var version = Windows.ApplicationModel.Package.Current.Id.Version;
        VersionTextBlock.Text = $"Phiên bản {version.Major}.{version.Minor}.{version.Build}";
    }

    //private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
    //{
    //    Debug.WriteLine($"Email changed: {EmailTextBox.Text}");
    //}

    //private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    //{
    //    Debug.WriteLine($"Password changed (length: {PasswordBox.Password.Length})");
    //}

    //private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
    //{
    //    _isPasswordVisible = !_isPasswordVisible;

    //    if (_isPasswordVisible)
    //    {
    //        PasswordIcon.Glyph = "\uF78D";
    //        Debug.WriteLine("Password visibility: Visible");
    //    }
    //    else
    //    {
    //        PasswordIcon.Glyph = "\uED1A";
    //        Debug.WriteLine("Password visibility: Hidden");
    //    }
    //}

    private async void SignInButton_Click(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("=== Sign In Button Clicked ===");
        //Debug.WriteLine($"Email: {EmailTextBox.Text}");
        //Debug.WriteLine($"Password length: {PasswordBox.Password.Length}");

        //if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
        //{
        //    await ShowMessageDialog("Lỗi", "Vui lòng nhập địa chỉ email");
        //    return;
        //}

        //if (string.IsNullOrWhiteSpace(PasswordBox.Password))
        //{
        //    await ShowMessageDialog("Lỗi", "Vui lòng nhập mật khẩu");
        //    return;
        //}

        //await ShowMessageDialog("Thông báo", "Chức năng đăng nhập sẽ được triển khai sau");
    }

    private async void ServerConfigButton_Click(object sender, RoutedEventArgs e)
    {
        // 1. Lấy URL cũ từ bộ nhớ máy (LocalSettings)
        var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
        string savedUrl = settings.Values["DbConnectionString"]?.ToString() ?? "";

        // 2. Khởi tạo và hiển thị Dialog
        var dialog = new ConfigDialog(savedUrl)
        {
            XamlRoot = this.XamlRoot
        };

        // Đăng ký sự kiện nút Test ngay trong lúc Dialog đang mở
        dialog.SecondaryButtonClick += async (s, args) => {
            args.Cancel = true; // Chặn không cho Dialog đóng lại
            await dialog.RunTestAsync();
        };

        var result = await dialog.ShowAsync();

        // 3. Nếu bấm "Lưu" (PrimaryButton), thực hiện lưu vào máy
        if (result == ContentDialogResult.Primary)
        {
            settings.Values["DbConnectionString"] = dialog.DbConnectionString;
            Debug.WriteLine("Đã lưu URL Database mới vào LocalSettings.");

            // Gợi ý: Hiển thị một thông báo nhỏ (InfoBar) báo thành công
        }
    }

    private async System.Threading.Tasks.Task ShowMessageDialog(string title, string message)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }
}
