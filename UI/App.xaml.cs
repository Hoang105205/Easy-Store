using Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using UI.Services.AuthService;
using UI.Services.CategoryService;
using UI.Services.ExcelService;
using UI.Services.ImportService;
using UI.Services.OrderService;
using UI.Services.PrintService;
using UI.Services.ProductService;
using UI.Services.ProfileService;
using UI.Services.StatisticsService;
using UI.ViewModels;
using UI.ViewModels.Import;
using UI.ViewModels.Orders;
using UI.ViewModels.Product;
using UI.ViewModels.Profile;
using UI.ViewModels.Statistics;
using UI.Utils;
using UI.Views;
using UI.Views.Import;

namespace UI;

public partial class App : Application
{
    private MainWindow? _window;

    public static Window? ActiveWindow;

    public Process? ApiProcess { get; private set; }

    public IServiceProvider Services { get; private set; }

    public App()
    {
        InitializeComponent();

        // 2. Tạo một "giỏ hàng" chứa các dịch vụ (Services)
        var serviceCollection = new ServiceCollection();

        // 3. Cấu hình các dịch vụ cho App
        ConfigureServices(serviceCollection);

        // 4. Cấu hình giấy phép 
        ConfigureLicenses();

        // 5. "Đóng gói" các dịch vụ lại để sẵn sàng sử dụng
        Services = serviceCollection.BuildServiceProvider();
    }

    private void ConfigureLicenses()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<AuthService>();
        services.AddSingleton<CategoryService>();
        services.AddSingleton<ProductService>();
        services.AddSingleton<OrderService>();
        services.AddSingleton<ExcelService>();
        services.AddSingleton<ImportService>();
        services.AddSingleton<DashboardService>();
        services.AddSingleton<PdfService>();
        services.AddSingleton<StatisticsService>();
        services.AddSingleton<UserService>();

        services.AddEasyStoreClient()
            .ConfigureHttpClient(client =>
            {
                // Sử dụng cổng 5000 mà API của bạn đang lắng nghe
                client.BaseAddress = new Uri(Core.AppConstants.BaseApiUrl);
            });


        services.AddTransient<ImportViewModel>();
        services.AddTransient<ImportEditorViewModel>();
        services.AddTransient<CreateImportViewModel>();
        services.AddTransient<ProductViewModel>();
        services.AddTransient<ProductDetailViewModel>();
        services.AddTransient<CreateProductViewModel>();
        services.AddTransient<CategoryViewModel>();
        services.AddTransient<OrderPageViewModel>();
        services.AddTransient<NewOrderPageViewModel>();
        services.AddTransient<OrderDetailPageViewModel>();
        services.AddTransient<NewOrderContainerViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<StatisticsViewModel>();
        services.AddTransient<ProfileViewModel>();

        // Bạn có thể đăng ký thêm các Service khác tại đây (ví dụ: NavigationService, DialogService)
    }


    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            ActiveWindow = _window;

            Frame rootFrame = _window.RootFrame;

            // Đưa toàn bộ logic vào try-catch
            ConfigTheme();
            CheckAndStartBackendApi();
            CheckIsLogedIn();

            _window.Closed += OnWindowClosed;
            _window.Activate();
        }
        catch (Exception ex)
        {
            // Nếu crash, ít nhất nó cũng hiện ra thông báo thay vì biến mất
            Debug.WriteLine($"CRITICAL ERROR: {ex.Message}");
            // Bạn có thể tạm thời comment các hàm Check... để test xem app trắng có lên không
        }
    }

    private void CheckAndStartBackendApi()
    {
        string? dbUrl = AppRuntimeStorage.GetString("DbConnectionString");
        if (!string.IsNullOrEmpty(dbUrl))
        {
            Debug.WriteLine("tìm thấy dbUrl trong LocalSettings");
            StartBackendApi(dbUrl);
        }
    }

    private void CheckIsLogedIn()
    {
        string? dbUrl = AppRuntimeStorage.GetString("DbConnectionString");
        bool isFirstTime = AppRuntimeStorage.GetBool("IsFirstTime", true);

        var authService = Services.GetRequiredService<AuthService>();

        if (isFirstTime)
        {
            Debug.WriteLine("Người dùng này lần đầu tiên sử dụng ứng dụng, điều hướng về OnboardingPage");
            _window?.RootFrame.Navigate(typeof(OnboardingPage));
        }
        else
        {
            if (string.IsNullOrEmpty(dbUrl) || !authService.IsLoggedIn())
            {
                Debug.WriteLine("Người dùng chưa đăng nhập, điều hướng về LoginPage");
                _window?.RootFrame.Navigate(typeof(LoginPage));
            }
            else
            {
                Debug.WriteLine("Người dùng đã đăng nhập, điều hướng về ShellPage");
                _window?.RootFrame.Navigate(typeof(ShellPage));
            }
        }
    }

    private void ConfigTheme()
    {
        var isDark = AppRuntimeStorage.GetBool("IsDarkMode", false);
        if (_window?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

    public void StartBackendApi(string connectionString)
    {
        try
        {
            // 1. Kiểm tra nếu API đang chạy thì không bật thêm cái nữa
            var existingProcesses = Process.GetProcessesByName("Api");
            if (existingProcesses.Length > 0) return;

            ApiProcess = new Process();

            // 2. Xác định đường dẫn file EXE
            string apiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backend", "Api.exe");

            if (!File.Exists(apiPath))
            {
                apiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Api.exe");
            }

            ApiProcess.StartInfo.FileName = apiPath;

            // 3. Truyền ConnectionString qua Command Line Arguments
            ApiProcess.StartInfo.Arguments = $"--ConnectionStrings:DefaultConnection=\"{connectionString}\"";

            // CỰC KỲ QUAN TRỌNG: Đặt thư mục làm việc là nơi chứa Api.exe
            // Điều này giúp Api.exe tìm thấy Api.dll và các file appsettings.json
            ApiProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(apiPath);

            // 4. Cấu hình chạy ngầm (Silent Mode)
            ApiProcess.StartInfo.CreateNoWindow = true;
            ApiProcess.StartInfo.UseShellExecute = false;
            ApiProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            ApiProcess.StartInfo.RedirectStandardOutput = true;
            ApiProcess.StartInfo.RedirectStandardError = true;

            ApiProcess.OutputDataReceived += (s, args) => Debug.WriteLine($"[API_LOG]: {args.Data}");
            ApiProcess.ErrorDataReceived += (s, args) => Debug.WriteLine($"[API_ERROR]: {args.Data}");

            ApiProcess.Start();
            ApiProcess.BeginOutputReadLine();
            ApiProcess.BeginErrorReadLine();
            Debug.WriteLine("=== Backend API đã khởi chạy thành công ===");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Không thể khởi chạy API: {ex.Message}");
        }
    }

    public void StopBackendApi()
    {
        try
        {
            // 1. Tiêu diệt tiến trình mà App đang theo dõi
            if (ApiProcess is { HasExited: false })
            {
                ApiProcess.Kill();
                ApiProcess.WaitForExit(2000); // Đợi tối đa 2 giây để nó đóng hoàn toàn
                Debug.WriteLine("=== Đã đóng tiến trình API hiện tại ===");
            }

            // 2. Quét thêm một lần nữa để chắc chắn không còn "Api" nào chạy ngầm (Ghost Process)
            var ghostProcesses = Process.GetProcessesByName("Api");
            foreach (var p in ghostProcesses)
            {
                p.Kill();
                p.WaitForExit(2000);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Lỗi khi dừng API: {ex.Message}");
        }
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        StopBackendApi();
    }

    public static new App Current => (App)Application.Current;

    public MainWindow? AppMainWindow => _window;
}
