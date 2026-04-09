using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using UI.ViewModels.Statistics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UI.Views.Statistics;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class StatisticsPage : Page
{
    public StatisticsViewModel ViewModel { get; }
    public StatisticsPage()
    {
        ViewModel = (App.Current as App)!.Services!.GetService<StatisticsViewModel>()!;

        InitializeComponent();
    }
}
