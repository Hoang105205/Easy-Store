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

        // SWITCH TO MyShop.Api
        // Khởi động ServiceProvider ngay khi app vừa bật
        // Services = ConfigureServices();
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

    public static new App Current => (App)Application.Current;
}
