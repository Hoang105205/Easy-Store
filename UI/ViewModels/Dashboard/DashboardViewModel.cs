using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UI;
using UI.Services.CategoryService;

public partial class DashboardViewModel : ObservableObject
{
    private readonly DashboardService _dashboardService;

    [ObservableProperty]
    private IGetDashboardStats_DashboardStats? stats;

    public ObservableCollection<IGetDashboardStats_DashboardStats_NearlyOutOfStock> OutOfStockList { get; } = new();

    [ObservableProperty]
    private string dateRangeText = "Đang tải dữ liệu";

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private ObservableCollection<ISeries> revenueSeries = new();

    [ObservableProperty]
    private ObservableCollection<ICartesianAxis> xAxes = new();

    [ObservableProperty]
    private ObservableCollection<ICartesianAxis> yAxes = new();

    public DashboardViewModel()
    {
        // Lấy service từ DI
        SetupChart();
        _dashboardService = App.Current.Services.GetRequiredService<DashboardService>();
    }

    public async Task LoadDataAsync(int? days = null)
    {
        IsLoading = true;

        Stats = await _dashboardService.GetDashboardOverviewAsync(days);

        IsLoading = false;

        OutOfStockList.Clear();
        if (Stats?.NearlyOutOfStock != null)
        {
            foreach (var item in Stats.NearlyOutOfStock)
            {
                OutOfStockList.Add(item);
            }
        }

        var endDate = DateTime.Now;
        if (days.HasValue)
        {
            var startDate = endDate.AddDays(-days.Value);
            DateRangeText = $"Đang hiển thị từ {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
        }
        else
        {
            DateRangeText = "Đang hiển thị toàn bộ dữ liệu hệ thống";
        }

        UpdateChartData(Stats.TotalRevenue);
    }

    private void SetupChart()
    {
        XAxes.Add(new Axis
        {
            Labels = new string[] {},
            LabelsPaint = new SolidColorPaint(SKColors.Gray),
            TextSize = 12
        });

        // Khởi tạo trục Y mặc định
        YAxes.Add(new Axis
        {
            MinLimit = 0,
            LabelsPaint = new SolidColorPaint(SKColors.Gray),
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(SKColors.LightGray)
        });
    }

    private void UpdateChartData(IReadOnlyList<IGetDashboardStats_DashboardStats_TotalRevenue>? data)
    {
        if (data == null || data.Count == 0) return;

        var values = data.Select(d => (double)(d.Revenue / 1000)).ToArray();
        var labels = data.Select(d => d.Date.ToString("dd/MM")).ToArray();

        if (XAxes.Count > 0)
        {
            XAxes[0].Labels = labels;
        }

        var mainColor = SKColor.Parse("#8B5CF6");

        var gradientFill = new LinearGradientPaint(
            new[] {
                mainColor.WithAlpha(100),
                mainColor.WithAlpha(10)
            },
            new SKPoint(0.5f, 0),
            new SKPoint(0.5f, 1)
        );

        RevenueSeries.Clear();
        RevenueSeries.Add(new LineSeries<double>
        {
            Values = values,

            Stroke = new SolidColorPaint(mainColor) { StrokeThickness = 4 },
            Fill = gradientFill,
            LineSmoothness = 0.8,

            GeometrySize = 12,
            GeometryStroke = new SolidColorPaint(mainColor) { StrokeThickness = 4 },
        });
    }
}