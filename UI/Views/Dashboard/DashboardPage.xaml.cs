using Core.Models;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using SkiaSharp;
using System;
using UI.Utils;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UI.Views.Dashboard
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage()
        {
            InitializeComponent();

            ViewModel = (App.Current as App)!.Services.GetService<DashboardViewModel>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await ViewModel.LoadDataAsync(7);
        }

        private void QuickAction_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem invokedItem && invokedItem.Tag != null)
            {
                string targetTag = invokedItem.Tag.ToString();

                Type pageType = PageHelper.GetPageTypeByTag(targetTag);

                if (pageType != null)
                {
                    this.Frame.Navigate(pageType);
                }

                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                bool isRestoreEnabled = localSettings.Values["RestoreSession"] as bool? ?? false;

                if (isRestoreEnabled)
                {
                    localSettings.Values["LastVisitedPage"] = targetTag;
                }
            }
        }

        private void ToggleDetails_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var headerGrid = sender as Grid;
            if (headerGrid == null) return;

            var parentGrid = headerGrid.Parent as Grid;
            if (parentGrid == null || parentGrid.Children.Count < 2) return;

            var detailsPanel = parentGrid.Children[1] as StackPanel;
            var icon = headerGrid.Children[1] as FontIcon;

            if (detailsPanel != null && detailsPanel.Visibility == Visibility.Collapsed)
            {
                detailsPanel.Visibility = Visibility.Visible;
                if (icon != null) icon.Glyph = "\xE70E"; // Đổi mũi tên chĩa lên (ChevronUp)
            }
            else if (detailsPanel != null)
            {
                detailsPanel.Visibility = Visibility.Collapsed;
                if (icon != null) icon.Glyph = "\xE70D"; // Đổi mũi tên chĩa xuống (ChevronDown)
            }
        }

        private async void Segmented_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            if (sender is CommunityToolkit.WinUI.Controls.Segmented segmented &&
                segmented.SelectedItem is CommunityToolkit.WinUI.Controls.SegmentedItem selectedItem)
            {
                string tagValue = selectedItem.Tag?.ToString() ?? "All";

                if (tagValue == "All")
                {
                    await ViewModel.LoadDataAsync(null);
                }
                else if (int.TryParse(tagValue, out int days))
                {
                    await ViewModel.LoadDataAsync(days);
                }
            }
        }
    }
}
