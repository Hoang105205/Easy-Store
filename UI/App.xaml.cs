using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Core.Data;
using System;

namespace UI;

public partial class App : Application
{
    // Cung cấp ServiceProvider để toàn bộ app có thể lấy service ra xài
    public IServiceProvider Services { get; }

    private Window? _window;

    public App()
    {
        InitializeComponent();

        // Khởi động ServiceProvider ngay khi app vừa bật
        Services = ConfigureServices();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }

    // --- HÀM CẤU HÌNH DEPENDENCY INJECTION ---
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // 1. Khai báo chuỗi kết nối (Connection String) tới PostgreSQL (Đang Hard code)
        var connectionString = "Host=localhost;Database=EasyStoreDb;Username=postgres;Password=123456";

        // 2. Tiêm AppDbContext vào hệ thống
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // (Tuần 2) Member B sẽ vào đây để đăng ký các ViewModels:
        // services.AddTransient<MainViewModel>();
        // services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }

    public static new App Current => (App)Application.Current;
}
