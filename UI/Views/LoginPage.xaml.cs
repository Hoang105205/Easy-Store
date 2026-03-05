using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace UI.Views;

public sealed partial class LoginPage : Page
{
    public LoginPage()
    {
        InitializeComponent();
        
        var version = Windows.ApplicationModel.Package.Current.Id.Version;
        VersionTextBlock.Text = $"Phiên bản {version.Major}.{version.Minor}.{version.Build}";
    }

    private async void SignInButton_Click(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("=== Sign In Button Clicked ===");
        // 1. Kiểm tra đầu vào cơ bản
        if (string.IsNullOrWhiteSpace(UsernameTextBox.Text) || string.IsNullOrWhiteSpace(PasswordBox.Password))
        {
            await ShowMessageDialog("Lỗi", "Vui lòng nhập đầy đủ email và mật khẩu.");
            return;
        }

        var button = sender as Button;
        
        if (button != null) button.IsEnabled = false; 

        try
        {
            var client = App.Current.Services.GetRequiredService<IEasyStoreClient>();

            // 4. Gọi hàm Login (Tham số u và p khớp với file Auth.graphql của bạn)
            // Tôi giả định bạn dùng Email làm Username
            var result = await client.Login.ExecuteAsync(UsernameTextBox.Text, PasswordBox.Password);

            // 5. Kiểm tra lỗi hệ thống (Lỗi mạng, lỗi cú pháp GraphQL...)
            if (result.Errors.Count > 0)
            {
                await ShowMessageDialog("Lỗi hệ thống", result.Errors[0].Message);
                return;
            }

            // 6. Kiểm tra kết quả nghiệp vụ từ API
            var loginInfo = result.Data?.Login;
            if (loginInfo != null && loginInfo.Success == true)
            {
                Debug.WriteLine("Đăng nhập thành công!");

                this.Frame.Navigate(typeof(ShellPage));

                // Xóa lịch sử để không quay lại trang Login bằng nút Back
                this.Frame.BackStack.Clear();
            }
            else
            {
                // Thất bại (Sai pass, user không tồn tại...)
                await ShowMessageDialog("Đăng nhập thất bại", loginInfo?.Message ?? "Thông tin không chính xác.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {ex.Message}");
            await ShowMessageDialog("Lỗi kết nối", "Không thể kết nối tới Server. Hãy chắc chắn API đang chạy ở cổng 5000.");
        }
        finally
        {
            // Bật lại nút bấm
            if (button != null) button.IsEnabled = true;
        }
    }

    // Hàm hỗ trợ hiển thị thông báo trong WinUI 3
    private async Task ShowMessageDialog(string title, string content)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "Đóng",
            XamlRoot = this.XamlRoot // Bắt buộc phải có XamlRoot trong WinUI 3
        };
        await dialog.ShowAsync();
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
            string newUrl = dialog.DbConnectionString?.Trim() ?? "";

            if (!string.IsNullOrEmpty(newUrl) && newUrl != savedUrl)
            {
                settings.Values["DbConnectionString"] = newUrl;

                // 3. RESTART LOGIC: Dừng cái cũ, bật cái mới
                App.Current.StopBackendApi(); // Dọn dẹp bản cũ
                App.Current.StartBackendApi(newUrl); // Khởi chạy bản mới với tham số mới

                Debug.WriteLine("=== Restart API thành công với URL mới ===");
            }

            // Gợi ý: Hiển thị một thông báo nhỏ (InfoBar) báo thành công
        }
    }
}
