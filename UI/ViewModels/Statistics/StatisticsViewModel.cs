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
        if (FromDate > ToDate)
        {
            DateErrorMessage = "Ngày bắt đầu không được lớn hơn ngày kết thúc!";
            IsDateErrorVisible = true;
            return;
        }

        IsDateErrorVisible = false;
        if (IsLoading || FromDate == null || ToDate == null) return;
        IsLoading = true;

        try
        {
            var result = await _statsService.GetStatisticsAsync(FromDate.Value.DateTime, ToDate.Value.DateTime);
            if (result.IsSuccessResult() && result.Data?.Statistics != null)
            {
                var data = result.Data.Statistics;
                HasData = data.ChartData.Any(x => x.Revenue > 0 || x.Profit > 0);

                // --- LOGIC MÀU SẮC ĐƠN GIẢN ---
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                bool isDark = App.Current.RequestedTheme == ApplicationTheme.Dark;

                // 2. Nếu có cài đặt riêng trong LocalSettings thì ghi đè lên
                if (localSettings.Values["IsDarkMode"] is bool isUserPref)
                {
                    isDark = isUserPref;
                }

                var textColor = isDark ? SKColors.White : SKColor.Parse("#2D2D2D");
                var separatorColor = isDark ? SKColor.Parse("#22FFFFFF") : SKColor.Parse("#11000000");
                var revenueColor = isDark ? SKColor.Parse("#60CDFF") : SKColor.Parse("#0078D4"); // Xanh dương
                var profitColor = isDark ? SKColor.Parse("#6CCB5F") : SKColor.Parse("#107C10");  // Xanh lá

                var textPaint = new SolidColorPaint(textColor);
                var separatorPaint = new SolidColorPaint(separatorColor) { StrokeThickness = 1 };

                LegendTextPaint = textPaint;

                // 1. Cấu hình Series (Thêm Rx, Ry cho cột bo góc hiện đại)
                Series = new ISeries[]
                {
                    new ColumnSeries<long> {
                        Name = "Doanh thu",
                        Values = data.ChartData.Select(x => x.Revenue).ToArray(),
                        Fill = new SolidColorPaint(revenueColor),
                        Rx = 6, Ry = 6, MaxBarWidth = 35
                    },
                    new ColumnSeries<long> {
                        Name = "Lợi nhuận",
                        Values = data.ChartData.Select(x => x.Profit).ToArray(),
                        Fill = new SolidColorPaint(profitColor),
                        Rx = 6, Ry = 6, MaxBarWidth = 35
                    }
                };

                // 2. Cấu hình Trục X
                XAxes = new Axis[] {
                    new Axis {
                        Labels = data.ChartData.Select(x => x.Label).ToArray(),
                        LabelsPaint = textPaint,
                        SeparatorsPaint = separatorPaint, // Thêm đường kẻ dọc mờ
                        TextSize = 12
                    }
                };

                // 3. Cấu hình Trục Y
                YAxes = new Axis[] {
                    new Axis {
                        LabelsPaint = textPaint,
                        SeparatorsPaint = separatorPaint, // Thêm đường kẻ ngang mờ
                        TextSize = 12,
                        Labeler = val => val >= 1000000
                            ? (val/1000000.0).ToString("N1") + "M"
                            : (val >= 1000 ? (val/1000.0).ToString("N0") + "k" : val.ToString())
                    }
                };

                // Summary Data
                TotalRevenueText = data.Summary.TotalRevenue.ToString("N0") + " đ";
                TotalProfitText = data.Summary.TotalProfit.ToString("N0") + " đ";
                TotalOrders = data.Summary.TotalOrders;
                LowStockCount = data.Summary.LowStockProducts;

                TopProducts.Clear();
                foreach (var item in data.TopProducts) TopProducts.Add(item);
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
}

