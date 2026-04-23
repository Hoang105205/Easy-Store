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
using UI.Utils;
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
            // Pagination
            int savedItemsPerPage = AppRuntimeStorage.GetInt("ItemsPerPage", 10);
            ItemsPerPageComboBox.SelectedItem = savedItemsPerPage.ToString();

            // Theme
            DarkThemeToggle.IsOn = AppRuntimeStorage.GetBool("IsDarkMode", false);

            // Session
            bool isRestoreEnabled = AppRuntimeStorage.GetBool("RestoreSession", false);
            RestoreSessionToggle.IsOn = isRestoreEnabled;
        }

        private void ItemsPerPageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemsPerPageComboBox.SelectedItem is string selectedValue && int.TryParse(selectedValue, out int itemsPerPage))
            {
                // Ghi đè số mới vào LocalSettings
                AppRuntimeStorage.SetValue("ItemsPerPage", itemsPerPage);
            }
        }

        private void DarkThemeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                bool isDarkMode = toggleSwitch.IsOn;

                // 1. Lưu cấu hình vào LocalSettings để lần sau mở app lên nó nhớ
                AppRuntimeStorage.SetValue("IsDarkMode", isDarkMode);

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

                // 1. Lưu thiết lập Bật/Tắt mới nhất
                AppRuntimeStorage.SetValue("RestoreSession", isEnabled);

                // 2. Dọn dẹp: Nếu người dùng TẮT, xóa luôn vết tích của trang cũ
                if (!isEnabled)
                {
                    AppRuntimeStorage.RemoveValue("LastVisitedPage");
                }
            }
        }

        private async void OpenServerConfigButton_Click(object sender, RoutedEventArgs e)
        {
            await DbConfigManager.ShowConfigDialogAsync(this.XamlRoot);
        }
    }
}
