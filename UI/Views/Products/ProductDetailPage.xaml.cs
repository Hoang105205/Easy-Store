using Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UI.ViewModels;

namespace UI.Views.Products
{
    public sealed partial class ProductDetailPage : Page
    {
        public ProductDetailViewModel ViewModel { get; }

        public ProductDetailPage()
        {
            this.InitializeComponent();

            ViewModel = (App.Current as App)!.Services.GetRequiredService<ProductDetailViewModel>();
            ViewModel.EditImages.CollectionChanged += (s, e) => { UploadIcon.Visibility = ViewModel.EditImages.Count > 0 ? Visibility.Collapsed : Visibility.Visible; };

            ViewModel.GoBackAction = () => Frame.GoBack();
            ViewModel.ShowConfirmAction = async (title, content, pri, close) => await ShowConfirmDialog(title, content, pri, close);
            ViewModel.ShowAlertAction = async (title, content) => await ShowDialog(title, content);
        }

        // Lấy Id sản phẩm từ tham số khi điều hướng đến trang detail, sau đó gọi ViewModel để load dữ liệu
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Guid productId)
            {
                await ViewModel.LoadCategoriesAsync();
                await ViewModel.LoadDataAsync(productId);
            }
        }

        private void Thumbnail_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is string clickedImagePath)
            {
                ViewModel.MainImage = clickedImagePath; // Đổi ảnh bự khi click ảnh nhỏ
            }
        }

        private async void UploadImages_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.EditImages.Count >= 3) { await ShowDialog("Thông báo", "Tối đa 3 ảnh."); return; }
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.Current.AppMainWindow));
            picker.FileTypeFilter.Add(".jpg"); picker.FileTypeFilter.Add(".png");
            var file = await picker.PickSingleFileAsync();
            if (file != null) ViewModel.EditImages.Add(file.Path);
        }

        private void ImageDragOver(object sender, DragEventArgs e) => e.AcceptedOperation = e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems) ? Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy : Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;

        private async void ImageDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    if (ViewModel.EditImages.Count >= 3) break;
                    if (item is Windows.Storage.StorageFile file && (file.FileType.ToLower() == ".jpg" || file.FileType.ToLower() == ".png"))
                        ViewModel.EditImages.Add(file.Path);
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

        // Các hàm hiển thị dialog đơn giản
        private async Task ShowDialog(string title, string content)
        {
            var dialog = new ContentDialog { Title = title, Content = content, CloseButtonText = "Đóng", XamlRoot = this.XamlRoot, RequestedTheme = this.ActualTheme };
            await dialog.ShowAsync();
        }

        private async Task<bool> ShowConfirmDialog(string title, string content, string priBtn = "Có", string closeBtn = "Không")
        {
            var dialog = new ContentDialog { Title = title, Content = content, PrimaryButtonText = priBtn, CloseButtonText = closeBtn, XamlRoot = this.XamlRoot, RequestedTheme = this.ActualTheme };
            return await dialog.ShowAsync() == ContentDialogResult.Primary;
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