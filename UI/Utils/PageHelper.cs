using System;
using System.Collections.Generic;
using System.Text;
using UI.Views;
using UI.Views.Dashboard;
using UI.Views.Import;
using UI.Views.Orders;
using UI.Views.Products;
using UI.Views.Settings;
using UI.Views.Statistics;
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
            "Import" => typeof(ImportPage),
            "Orders" => typeof(OrderPage),
            "Statistics" => typeof(StatisticsPage),
            "Profile" => null,
            "Settings" => typeof(SettingsPage),
            "CreateProduct" => typeof(CreateProductPage),
            "CreateImport" => typeof(CreateImportPage),
            "CreateOrder" => typeof(NewOrderPage),
            _ => null // Mặc định nếu lỗi thì về trang chủ
        };
    }
}
