using CommunityToolkit.WinUI.UI.Controls;
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
using System.Diagnostics;
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
public sealed partial class ImportPage : Page
{
    public ImportViewModel ViewModel { get; }

    public ImportPage()
    {
        ViewModel = (App.Current as App)!.Services!.GetService<ImportViewModel>()!;

        InitializeComponent();

        this.NavigationCacheMode = NavigationCacheMode.Enabled;

        ViewModel.NavigateToCreateImportAction = (excelFile) =>
        {
            Frame.Navigate(typeof(CreateImportPage), excelFile);
        };
    }

    private void OnCreateNewImportClicked(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(CreateImportPage));
    }

    private async void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadDataAsync(null);
    }

    private void OnImportRowDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        // Lấy dòng được chọn
        var grid = (DataGrid)sender;
        var selectedItem = grid.SelectedItem as IGetImportHistory_ImportHistory_Nodes;

        Debug.WriteLine($"Selected Item: {selectedItem?.Id}, Status: {selectedItem?.Status}");

        if (selectedItem != null)
        {
            Frame.Navigate(typeof(ImportEditorPage), selectedItem.Id);
        }
    }

    private void ImportHistoryGrid_Sorting(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridColumnEventArgs e)
    {
        string columnName = e.Column.Tag?.ToString();
        if (string.IsNullOrEmpty(columnName)) return;

        ViewModel.SortCommand.Execute(columnName);

        foreach (var col in ImportHistoryGrid.Columns)
        {
            if (col.Tag?.ToString() == ViewModel.ActiveSortColumn)
            {
                col.SortDirection = ViewModel.IsAscending
                    ? CommunityToolkit.WinUI.UI.Controls.DataGridSortDirection.Ascending
                    : CommunityToolkit.WinUI.UI.Controls.DataGridSortDirection.Descending;
            }
            else
            {
                col.SortDirection = null;
            }
        }
    }
}
