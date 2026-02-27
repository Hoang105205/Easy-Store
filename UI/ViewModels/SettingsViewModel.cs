using CommunityToolkit.Mvvm.ComponentModel;

namespace UI.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _greetingText = "Hello! MVVM đã hoạt động thành công!";
    }
}