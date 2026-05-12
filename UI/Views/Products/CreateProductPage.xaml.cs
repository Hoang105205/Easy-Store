using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Threading.Tasks;
using UI.Dialog;
using UI.ViewModels;
using UI.ViewModels.Import;

namespace UI.Views.Products
{
    public sealed partial class CreateProductPage : Page
    {
        public CreateProductViewModel ViewModel { get; }
        private LoadingDialog loadingDialog;

        public CreateProductPage()
        {
            ViewModel = (App.Current as App)!.Services.GetRequiredService<CreateProductViewModel>();

            InitializeComponent();

            loadingDialog = new LoadingDialog();

            ViewModel.ShowLoadingAction = async () =>
            {
                loadingDialog.XamlRoot = this.XamlRoot;
                await loadingDialog.ShowAsync();
            };

            ViewModel.HideLoadingAction = () =>
            {
                loadingDialog.Hide();
            };

            // Đăng ký sự kiện: Cứ mỗi khi danh sách ảnh thay đổi (thêm, xóa, reset), hàm bên dưới sẽ chạy
            ViewModel.SelectedImages.CollectionChanged += SelectedImages_CollectionChanged;

            ViewModel.GoBackAction = () => Frame.GoBack();
            ViewModel.ShowAlertAction = async (title, content) => await ShowDialog(title, content);
            ViewModel.ShowConfirmAction = async (title, content) => await ShowConfirmDialog(title, content);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ViewModel.SelectedImages.CollectionChanged -= SelectedImages_CollectionChanged;
            ViewModel.SelectedImages.CollectionChanged += SelectedImages_CollectionChanged;

            await ViewModel.LoadCategoriesAsync();
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ViewModel.SelectedImages.CollectionChanged -= SelectedImages_CollectionChanged;

            if (!ViewModel.IsSavedSuccessfully)
            {
                _ = ViewModel.CleanupDraftImagesAsync();
            }
        }

        private void SelectedImages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UploadIcon.Visibility = ViewModel.SelectedImages.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void UploadImages_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedImages.Count >= 3)
            {
                await ShowDialog("Thông báo", "Chỉ được chọn tối đa 3 ảnh.");
                return;
            }

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.AppMainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                await ViewModel.UploadAndAddImageAsync(file);
            }
        }

        private void ImageDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            }
            else
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            }
        }
        private async void ImageDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                foreach (var item in items)
                {
                    if (ViewModel.SelectedImages.Count >= 3)
                    {
                        await ShowDialog("Thông báo", "Chỉ được chọn tối đa 3 ảnh.");
                        break;
                    }

                    if (item is Windows.Storage.StorageFile file)
                    {
                        string ext = file.FileType.ToLower();
                        if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                        {
                            await ViewModel.UploadAndAddImageAsync(file);
                        }
                        else
                        {
                            await ShowDialog("Lỗi định dạng", $"File '{file.Name}' không được hỗ trợ. Vui lòng chọn ảnh .jpg hoặc .png");
                        }
                    }
                }
            }
        }

        private void RemoveImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is string imagePath)
            {
                ViewModel.RemoveImageCommand.Execute(imagePath);
            }
        }

        private async Task ShowDialog(string title, string content)
        {
            var dialog = new ContentDialog { Title = title, Content = content, CloseButtonText = "Đóng", XamlRoot = this.XamlRoot, RequestedTheme = this.ActualTheme };
            await dialog.ShowAsync();
        }

        private async Task<bool> ShowConfirmDialog(string title, string content)
        {
            var dialog = new ContentDialog { Title = title, Content = content, PrimaryButtonText = "Có", CloseButtonText = "Không", XamlRoot = this.XamlRoot, RequestedTheme = this.ActualTheme };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private void NumberTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                string rawNumber = new string(textBox.Text.Where(char.IsDigit).ToArray());
                textBox.Text = rawNumber;
                textBox.Select(textBox.Text.Length, 0);
            }
        }

        private void NumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                string rawNumber = new string(textBox.Text.Where(char.IsDigit).ToArray());

                if (long.TryParse(rawNumber, out long value))
                {
                    string formatString = textBox.Tag?.ToString() ?? "{0:N0}";
                    textBox.Text = string.Format(new System.Globalization.CultureInfo("vi-VN"), formatString, value);
                }
                else
                {
                    textBox.Text = string.Empty;
                }
            }
        }
    }
}