using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Npgsql;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UI.Views;

public sealed partial class ConfigDialog : ContentDialog
{
    public string DbConnectionString => UrlInput.Text.Trim();
    public string SupabaseUri => SupabaseUriInput.Text.Trim();
    public string SupabaseApiKey => SupabaseApiKeyInput.Text.Trim();

    public ConfigDialog(string currentDbUrl, string currentUri, string currentApiKey)
    {
        InitializeComponent();

        UrlInput.Text = currentDbUrl;
        SupabaseUriInput.Text = currentUri;
        SupabaseApiKeyInput.Text = currentApiKey;

        IsPrimaryButtonEnabled = false;

        UrlInput.TextChanged += OnInputChanged;
        SupabaseUriInput.TextChanged += OnInputChanged;
        SupabaseApiKeyInput.TextChanged += OnInputChanged;
    }

    private void OnInputChanged(object sender, TextChangedEventArgs e)
    {
        IsPrimaryButtonEnabled = false;
        DefaultButton = ContentDialogButton.None;
        StatusTextBlock.Visibility = Visibility.Collapsed;
    }

    // Logic cho nút "Kiểm tra" (SecondaryButton)
    public async Task<bool> RunTestAsync()
    {
        IsPrimaryButtonEnabled = false;
        StatusTextBlock.Visibility = Visibility.Visible;
        StatusTextBlock.Text = "Đang kiểm tra kết nối...";
        StatusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);

        StringBuilder statusMsg = new StringBuilder();
        bool allTestsPassed = true;
        bool hasTestedSomething = false;

        // 1. Kiểm tra Database (Npgsql)
        if (!string.IsNullOrEmpty(DbConnectionString))
        {
            hasTestedSomething = true;
            try
            {
                using var conn = new NpgsqlConnection(DbConnectionString);
                await conn.OpenAsync();
                statusMsg.AppendLine("Database: Kết nối thành công.");
            }
            catch (Exception ex)
            {
                statusMsg.AppendLine($"Database: {ex.Message}");
                allTestsPassed = false;
            }
        }

        // 2. Kiểm tra Supabase REST API
        if (!string.IsNullOrEmpty(SupabaseUri) && !string.IsNullOrEmpty(SupabaseApiKey))
        {
            hasTestedSomething = true;
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("apikey", SupabaseApiKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseApiKey}");

                var response = await client.GetAsync($"{SupabaseUri.TrimEnd('/')}/rest/v1/");

                if (response.IsSuccessStatusCode)
                    statusMsg.AppendLine("Supabase API: Xác thực thành công.");
                else
                {
                    statusMsg.AppendLine($"Supabase API: Mã lỗi {response.StatusCode}");
                    allTestsPassed = false;
                }
            }
            catch (Exception ex)
            {
                statusMsg.AppendLine($"Supabase API: {ex.Message}");
                allTestsPassed = false;
            }
        }
        else if ((!string.IsNullOrEmpty(SupabaseUri) && string.IsNullOrEmpty(SupabaseApiKey)) ||
                 (string.IsNullOrEmpty(SupabaseUri) && !string.IsNullOrEmpty(SupabaseApiKey)))
        {
            statusMsg.AppendLine("Supabase API: Cần nhập đủ cả URI và API Key để test.");
            allTestsPassed = false;
            hasTestedSomething = true;
        }

        // Xử lý kết quả hiển thị
        if (!hasTestedSomething)
        {
            StatusTextBlock.Text = "Vui lòng nhập ít nhất một cấu hình để kiểm tra.";
            StatusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
            return false;
        }

        StatusTextBlock.Text = statusMsg.ToString().Trim();

        if (allTestsPassed)
        {
            StatusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
            IsPrimaryButtonEnabled = true;
            DefaultButton = ContentDialogButton.Primary;
            this.UpdateLayout();
            return true;
        }
        else
        {
            StatusTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
            return false;
        }
    }
}
