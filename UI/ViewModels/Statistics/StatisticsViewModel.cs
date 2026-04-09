using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Drawing;
using Microsoft.UI.Xaml;
using SkiaSharp;
using StrawberryShake;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UI.Services.StatisticsService;

namespace UI.ViewModels.Statistics;

public partial class StatisticsViewModel : ObservableObject
{
    private readonly StatisticsService _statsService;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private DateTimeOffset? fromDate = DateTimeOffset.Now.AddDays(-7);
    [ObservableProperty] private DateTimeOffset? toDate = DateTimeOffset.Now;
    [ObservableProperty] private string totalRevenueText = "0 đ";
    [ObservableProperty] private string totalProfitText = "0 đ";
    [ObservableProperty] private int totalOrders;
    [ObservableProperty] private int lowStockCount;
    [ObservableProperty] private bool hasData = true;
    [ObservableProperty] private bool isDateErrorVisible;
    [ObservableProperty] private string dateErrorMessage = "";

    // --- LIVECHARTS PROPERTIES ---
    [ObservableProperty]
    private SolidColorPaint legendTextPaint;
    [ObservableProperty] private ISeries[] series;
    [ObservableProperty]
    private IEnumerable<ICartesianAxis> xAxes;
    [ObservableProperty]
    private IEnumerable<ICartesianAxis> yAxes;

    public ObservableCollection<IGetStatistics_Statistics_TopProducts> TopProducts { get; } = new();

    public StatisticsViewModel(StatisticsService statsService)
    {
        _statsService = statsService;
        LoadStatisticsCommand.Execute(null);
    }

    partial void OnFromDateChanged(DateTimeOffset? value) => LoadStatisticsCommand.Execute(null);
    partial void OnToDateChanged(DateTimeOffset? value) => LoadStatisticsCommand.Execute(null);

    [RelayCommand]
    private async Task LoadStatisticsAsync()
    {
        // 1. Kiểm tra điều kiện đầu vào
        if (!ValidateInput()) return;

        IsLoading = true;

        try
        {
            var result = await _statsService.GetStatisticsAsync(FromDate.Value.DateTime, ToDate.Value.DateTime);
            if (result.IsSuccessResult() && result.Data?.Statistics != null)
            {
                var data = result.Data.Statistics;

                // 2. Cập nhật trạng thái dữ liệu
                HasData = data.ChartData.Any(x => x.Revenue > 0 || x.Profit > 0);

                // 3. Cấu hình giao diện biểu đồ (Màu sắc + Trục)
                ConfigureChart(data.ChartData);

                // 4. Cập nhật các thông số tổng hợp (Summary)
                UpdateSummary(data.Summary);

                // 5. Cập nhật danh sách Top Products
                UpdateTopProducts(data.TopProducts);
            }
        }
        catch (Exception ex) 
        { 
            Debug.WriteLine($"[ERROR]: {ex.Message}"); 
        }
        finally 
        { 
            IsLoading = false; 
        }
    }

    private bool ValidateInput()
    {
        if (FromDate > ToDate)
        {
            DateErrorMessage = "Ngày bắt đầu không được lớn hơn ngày kết thúc!";
            IsDateErrorVisible = true;
            return false;
        }

        IsDateErrorVisible = false;
        return !IsLoading && FromDate != null && ToDate != null;
    }

    private void UpdateSummary(IGetStatistics_Statistics_Summary summary)
    {
        TotalRevenueText = summary.TotalRevenue.ToString("N0") + " đ";
        TotalProfitText = summary.TotalProfit.ToString("N0") + " đ";
        TotalOrders = summary.TotalOrders;
        LowStockCount = summary.LowStockProducts;
    }

    private void UpdateTopProducts(IEnumerable<IGetStatistics_Statistics_TopProducts> products)
    {
        TopProducts.Clear();
        foreach (var item in products)
        {
            TopProducts.Add(item);
        }
    }

    private void ConfigureChart(IReadOnlyList<IGetStatistics_Statistics_ChartData> chartData)
    {
        // Tách logic xác định Theme
        bool isDark = GetCurrentTheme();

        // Tách logic lấy bảng màu
        var colors = GetChartColors(isDark);
        var textPaint = new SolidColorPaint(colors.Text);
        var separatorPaint = new SolidColorPaint(colors.Separator) { StrokeThickness = 1 };

        LegendTextPaint = textPaint;

        // Cấu hình Series
        Series = new ISeries[]
        {
            new ColumnSeries<long> {
                Name = "Doanh thu",
                Values = chartData.Select(x => x.Revenue).ToArray(),
                Fill = new SolidColorPaint(colors.Revenue),
                Rx = 6, Ry = 6, MaxBarWidth = 35
            },
            new ColumnSeries<long> {
                Name = "Lợi nhuận",
                Values = chartData.Select(x => x.Profit).ToArray(),
                Fill = new SolidColorPaint(colors.Profit),
                Rx = 6, Ry = 6, MaxBarWidth = 35
            }
        };

        // Cấu hình các trục
        XAxes = new Axis[] {
            new Axis {
                Labels = chartData.Select(x => x.Label).ToArray(),
                LabelsPaint = textPaint,
                SeparatorsPaint = separatorPaint,
                TextSize = 12
            }
        };

        YAxes = new Axis[] {
            new Axis {
                LabelsPaint = textPaint,
                SeparatorsPaint = separatorPaint,
                TextSize = 12,
                Labeler = val => val >= 1000000
                    ? (val/1000000.0).ToString("N1") + "M"
                    : (val >= 1000 ? (val/1000.0).ToString("N0") + "k" : val.ToString())
            }
        };
    }

    private bool GetCurrentTheme()
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        if (localSettings.Values["IsDarkMode"] is bool isUserPref)
        {
            return isUserPref;
        }
        return App.Current.RequestedTheme == ApplicationTheme.Dark;
    }

    // Record hoặc Tuple để quản lý bảng màu cho gọn
    private (SKColor Text, SKColor Separator, SKColor Revenue, SKColor Profit) GetChartColors(bool isDark)
    {
        return (
            Text: isDark ? SKColors.White : SKColor.Parse("#2D2D2D"),
            Separator: isDark ? SKColor.Parse("#22FFFFFF") : SKColor.Parse("#11000000"),
            Revenue: isDark ? SKColor.Parse("#60CDFF") : SKColor.Parse("#0078D4"),
            Profit: isDark ? SKColor.Parse("#6CCB5F") : SKColor.Parse("#107C10")
        );
    }
}

