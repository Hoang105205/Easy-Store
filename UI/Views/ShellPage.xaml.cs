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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UI.Services.AuthService;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UI.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        InitializeComponent();

        // Khi thanh điều hướng load xong thì tự động chọn một menu mặc định
        NavView.Loaded += (s, e) =>
        {
            // MenuItems[1] chính là "Sản phẩm" (Index 0 là Trang chủ)
            NavView.SelectedItem = NavView.MenuItems[1];
            ContentFrame.Navigate(typeof(ProductsPage));
        };
    }


    private async void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        // Kiểm tra Tag của item được chọn
        var tag = args.InvokedItemContainer?.Tag?.ToString();

        if (tag == "Logout")
        {
            await HandleLogoutAsync();
        }
        else
        {
            // Xử lý điều hướng cho các menu khác (Dashboard, Products...)
            // NavigationService.NavigateTo(tag);
            switch (tag)
            {
                case "Products":
                    ContentFrame.Navigate(typeof(ProductsPage));
                    break;
                case "Dashboard":
                case "Orders":
                case "Reports":
                case "Profile":
                case "Settings":
                    // Tạm thời làm trắng màn hình khi bấm vào các menu chưa code xong
                    ContentFrame.Content = null;
                    break;
            }
        }
    }

    private async Task HandleLogoutAsync()
    {
        // 1. Tạo hộp thoại xác nhận
        ContentDialog logoutDialog = new ContentDialog
        {
            Title = "Xác nhận đăng xuất",
            Content = "Bạn có chắc chắn muốn thoát khỏi phiên làm việc này không?",
            PrimaryButtonText = "Đăng xuất",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot 
        };

        // 2. Hiển thị dialog và chờ kết quả
        ContentDialogResult result = await logoutDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // 3. Xử lý xóa session
            var authService = App.Current.Services.GetRequiredService<AuthService>();
            authService.ClearSession();

            // 4. Điều hướng về trang Login
            // Lưu ý: Dùng Frame của Window (thường là Frame cha của ShellPage)
            if (this.Frame != null)
            {
                this.Frame.Navigate(typeof(LoginPage));

                // Xóa sạch lịch sử để không thể nhấn Back quay lại Shell
                this.Frame.BackStack.Clear();
            }
        }
        else
        {
            // Nếu người dùng chọn Hủy, ta chọn lại menu cũ (ví dụ Dashboard) để không bị kẹt ở nút Logout
            NavView.SelectedItem = NavView.MenuItems[0];
        }
    }
}