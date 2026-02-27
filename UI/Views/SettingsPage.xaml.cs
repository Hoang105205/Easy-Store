using Microsoft.UI.Xaml.Controls;
using UI.ViewModels;

namespace UI.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            this.DataContext = new SettingsViewModel(); // Gắn ViewModel vào View
        }
    }
}