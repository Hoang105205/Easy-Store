using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UI.Views;

namespace UI.Utils;

public class DbConfigManager
{
    // Nhận XamlRoot từ Page gọi nó để Dialog biết đường hiển thị
    public static async Task ShowConfigDialogAsync(XamlRoot xamlRoot)
    {
        // 1. Lấy URL cũ từ bộ nhớ máy (LocalSettings)
        string savedDbUrl = AppRuntimeStorage.GetString("DbConnectionString", "") ?? "";
        string savedUri = AppRuntimeStorage.GetString("SupabaseUri", "") ?? "";
        string savedApiKey = AppRuntimeStorage.GetString("SupabaseApiKey", "") ?? "";

        // 2. Khởi tạo và hiển thị Dialog
        var dialog = new ConfigDialog(savedDbUrl, savedUri, savedApiKey)
        {
            XamlRoot = xamlRoot // Truyền XamlRoot vào đây
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
            string newDbUrl = dialog.DbConnectionString;
            string newUri = dialog.SupabaseUri;
            string newApiKey = dialog.SupabaseApiKey;

            if (newDbUrl != savedDbUrl || newUri != savedUri || newApiKey != savedApiKey)
            {
                AppRuntimeStorage.SetValue("DbConnectionString", newDbUrl);
                AppRuntimeStorage.SetValue("SupabaseUri", newUri);
                AppRuntimeStorage.SetValue("SupabaseApiKey", newApiKey);

                // 3. RESTART LOGIC: Dừng cái cũ, bật cái mới
                App.Current.StopBackendApi(); // Gọi từ biến Current bạn đã setup
                App.Current.StartBackendApi(newDbUrl);

                Debug.WriteLine("=== Restart API thành công với URL mới ===");

                // Gợi ý: Nếu muốn bắn InfoBar báo thành công, bạn có thể trả về một biến bool 
                // từ hàm ShowConfigDialogAsync này để Page bên ngoài tự xử lý UI.
            }
        }
    }
}
