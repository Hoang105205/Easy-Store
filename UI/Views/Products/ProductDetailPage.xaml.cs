using Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Globalization;
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

            ViewModel = (App.Current as App)!.Services.GetService<ProductDetailViewModel>();
            ViewModel.EditImages.CollectionChanged += (s, e) => { UploadIcon.Visibility = ViewModel.EditImages.Count > 0 ? Visibility.Collapsed : Visibility.Visible; };
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

        private void BtnEdit_Click(object sender, RoutedEventArgs e) => ViewModel.EnableEditMode();

        private async void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (await ShowConfirmDialog("Xác nhận", "Bạn có chắc chắn muốn hủy? Các thay đổi trước đó sẽ bị xóa."))
            {
                ViewModel.CancelEditMode();
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (await ShowConfirmDialog("Xác nhận", "Bạn có chắc chắn muốn lưu các thay đổi này?"))
            {
                try
                {
                    await ViewModel.SaveChangesAsync();
                    ViewModel.CancelEditMode(); // Lưu xong thì thoát Edit Mode
                    await ShowDialog("Thành công", "Đã cập nhật sản phẩm thành công!");
                }
                catch (Exception ex) { await ShowDialog("Lỗi", ex.Message); }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (await ShowConfirmDialog("Cảnh báo nguy hiểm", $"Bạn có chắc chắn muốn xóa sản phẩm '{ViewModel.ProductName}' không? Hành động này không thể hoàn tác.", "Xóa", "Hủy"))
            {
                try
                {
                    await ViewModel.DeleteAsync();
                    await ShowDialog("Thành công", "Sản phẩm đã bị xóa.");
                    Frame.GoBack(); // Xóa xong thì tự động quay về trang Danh sách
                }
                catch (Exception ex) { await ShowDialog("Không thể xóa", ex.Message); }
            }
        }

        private void TxtPrice_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            var raw = textBox.Text;

            raw = raw
                .Replace(".", "")
                .Replace(",", "")
                .Replace("đ", "")
                .Trim();

            if (long.TryParse(raw, out var number))
            {
                textBox.Text = number.ToString("N0", new CultureInfo("vi-VN")) + " đ";
            }
        }

        private void TxtPrice_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            var raw = textBox.Text;

            raw = raw
                .Replace(".", "")
                .Replace(",", "")
                .Replace("đ", "")
                .Trim();

            textBox.Text = raw;
        }

        // Các hàm xử lý ảnh: Upload, Drag & Drop, Remove
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
                ViewModel.EditImages.Remove(imagePath);
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
    }
}