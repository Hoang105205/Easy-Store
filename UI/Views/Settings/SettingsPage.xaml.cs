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
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UI.Views.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();

            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            // Pagination
            ItemsPerPageComboBox.SelectedIndex = 0;

            // Theme
            if (localSettings.Values["IsDarkMode"] is bool isDark)
            {
                DarkThemeToggle.IsOn = isDark;
            }

            // Session
            bool isRestoreEnabled = localSettings.Values["RestoreSession"] as bool? ?? false;
            RestoreSessionToggle.IsOn = isRestoreEnabled;

            // Database
            DatabaseUrlTextBox.Text = "http://localhost:5000";
            Debug.WriteLine("[Settings] SettingsPage loaded.");
        }

        private void ItemsPerPageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemsPerPageComboBox.SelectedItem is string value)
            {
                Debug.WriteLine($"[Settings] Items per page changed: {value}");
            }
        }

        private void DarkThemeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                bool isDarkMode = toggleSwitch.IsOn;

                // 1. Lưu cấu hình vào LocalSettings để lần sau mở app lên nó nhớ
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["IsDarkMode"] = isDarkMode;

                // 2. Gọi cửa sổ chính (MainWindow) ra và đổi Theme toàn bộ ứng dụng
                if (App.Current.AppMainWindow?.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = isDarkMode ? ElementTheme.Dark : ElementTheme.Light;
                }
            }
        }

        private void RestoreSessionToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                bool isEnabled = toggleSwitch.IsOn;
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

                // 1. Lưu thiết lập Bật/Tắt mới nhất
                localSettings.Values["RestoreSession"] = isEnabled;

                // 2. Dọn dẹp: Nếu người dùng TẮT, xóa luôn vết tích của trang cũ
                if (!isEnabled)
                {
                    localSettings.Values.Remove("LastVisitedPage");
                }
            }
        }

        private void SaveDbUrlButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"[Settings] Save DB URL clicked: {DatabaseUrlTextBox.Text}");
        }

        private void TestDbUrlButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"[Settings] Test DB URL clicked: {DatabaseUrlTextBox.Text}");
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[Settings] About button clicked.");
        }
    }
}
