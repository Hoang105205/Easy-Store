using HotChocolate.Data.Filters;
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
using UI.Utils;
using UI.Views.Settings;
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

        this.Loaded += ShellPage_Loaded;
    }

    private void ShellPage_Loaded(object sender, RoutedEventArgs e)
    {
        NavigateInitialPage();
    }

    private void NavigateInitialPage()
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        string pageToLoad = "Dashboard"; // Trang mặc định nếu không có gì
        bool isRestoreEnabled = localSettings.Values["RestoreSession"] as bool? ?? false;

        // Nếu tính năng BẬT và có lưu Tag cũ
        if (isRestoreEnabled && localSettings.Values["LastVisitedPage"] != null)
        {
            pageToLoad = localSettings.Values["LastVisitedPage"].ToString();
        }

        // 1. Chuyển Frame tới trang đó
        Type pageType = PageHelper.GetPageTypeByTag(pageToLoad);
        if (pageType == null)
        {
            ContentFrame.Content = null;
        }
        else
        {

            ContentFrame.Navigate(pageType);
        }

        // 2. Highlight (Làm sáng) đúng nút trên thanh Menu
        var allMenuItems = NavView.MenuItems.OfType<NavigationViewItem>()
                     .Concat(NavView.FooterMenuItems.OfType<NavigationViewItem>());

        NavView.SelectedItem = allMenuItems.FirstOrDefault(m => m.Tag.ToString() == pageToLoad);
    }

    private async void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer != null && args.InvokedItemContainer.Tag != null)
        {
            string targetTag = args.InvokedItemContainer.Tag.ToString() ?? "Dashboard";

            if (targetTag == "Logout")
            {
                await HandleLogoutAsync();
                return; // Dừng xử lý tiếp để không chuyển trang sau khi logout
            }

            // 1. Chuyển trang
            Type pageType = PageHelper.GetPageTypeByTag(targetTag);
            if (pageType == null)
            {
                ContentFrame.Content = null; // Hoặc bạn có thể điều hướng về một trang lỗi chung
            }
            else
            {
                ContentFrame.Navigate(pageType);
            }


            // 2. Logic "Lưu nháp" phiên làm việc
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            bool isRestoreEnabled = localSettings.Values["RestoreSession"] as bool? ?? false;

            if (isRestoreEnabled)
            {
                localSettings.Values["LastVisitedPage"] = targetTag;
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