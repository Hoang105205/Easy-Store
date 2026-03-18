using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UI.ViewModels.Import;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UI.Views.Import;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class CreateImportPage : Page
{
    public CreateImportViewModel ViewModel { get; }

    public CreateImportPage()
    {
        InitializeComponent();

        ViewModel = (App.Current as App)!.Services.GetRequiredService<CreateImportViewModel>();

        ViewModel.GoBackAction = () =>
        {
            // Kiểm tra xem có trang trước đó không rồi mới lùi (tránh lỗi crash app)
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        };
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await ViewModel.LoadExistingAutoSaveAsync();
    }

    private async void OnInputLostFocus(object sender, RoutedEventArgs e)
    {
        // Kích hoạt tiến trình lưu ngầm
        await ViewModel.TriggerAutoSaveAsync();
    }
}
