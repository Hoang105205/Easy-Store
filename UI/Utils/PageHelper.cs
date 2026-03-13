using System;
using System.Collections.Generic;
using System.Text;
using UI.Views;
using UI.Views.Dashboard;
using UI.Views.Settings;
using Windows.Networking.NetworkOperators;

namespace UI.Utils;

public class PageHelper
{
    public static Type? GetPageTypeByTag(string tag)
    {
        return tag switch
        {
            "Dashboard" => typeof(DashboardPage), // Thay bằng tên Page thật của team bạn
            "Products" => typeof(ProductsPage),
            "Orders" => null,
            "Reports" => null,
            "Profile" => null,
            "Settings" => typeof(SettingsPage),
            _ => null // Mặc định nếu lỗi thì về trang chủ
        };
    }
}
