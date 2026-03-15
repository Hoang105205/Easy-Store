using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using UI.ViewModels;

namespace UI.Views.Products
{
    public sealed partial class CreateProductPage : Page
    {
        public CreateProductViewModel ViewModel { get; } = new CreateProductViewModel();

        public CreateProductPage()
        {
            this.InitializeComponent();

            // Đăng ký sự kiện: Cứ mỗi khi danh sách ảnh thay đổi (thêm, xóa, reset), hàm bên dưới sẽ chạy
            ViewModel.SelectedImages.CollectionChanged += SelectedImages_CollectionChanged;
        }
        private void SelectedImages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Nếu số lượng ảnh > 0 thì ẩn Icon đi (Collapsed), ngược lại thì hiện ra (Visible)
            UploadIcon.Visibility = ViewModel.SelectedImages.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void UploadImages_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedImages.Count >= 3)
            {
                await ShowDialog("Thông báo", "Chỉ được chọn tối đa 3 ảnh.");
                return;
            }

            // Gọi FilePicker của Windows
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.AppMainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                ViewModel.SelectedImages.Add(file.Path); // Tạm lưu path cục bộ
            }
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.SaveProductAsync();
                await ShowDialog("Thành công", "Sản phẩm được tạo mới thành công");
                Frame.GoBack(); // Trở về trang danh sách
            }
            catch (Exception ex)
            {
                await ShowDialog("Lỗi", ex.Message);
            }
        }

        private async void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = await ShowConfirmDialog("Xác nhận", "Bạn có chắc muốn hủy? Các thông tin đã nhập trước đó sẽ bị xóa.");
            if (result) Frame.GoBack();
        }

        private async void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = await ShowConfirmDialog("Xác nhận", "Bạn có muốn nhập lại? Các thông tin đã nhập trước đó sẽ bị xóa.");
            if (result) ViewModel.ResetForm();
        }

        private void ImageDragOver(object sender, DragEventArgs e)
        {
            // Kiểm tra xem dữ liệu được kéo vào có phải là File hay không
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                // Hiển thị icon "Copy" (dấu cộng) khi kéo file vào vùng hợp lệ
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            }
            else
            {
                // Từ chối nếu kéo text hoặc dữ liệu không phải file
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            }
        }
        private async void ImageDrop(object sender, DragEventArgs e)
        {
            // Lấy danh sách các file được thả vào
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                foreach (var item in items)
                {
                    // Kiểm tra số lượng ảnh tối đa
                    if (ViewModel.SelectedImages.Count >= 3)
                    {
                        await ShowDialog("Thông báo", "Chỉ được chọn tối đa 3 ảnh.");
                        break;
                    }

                    // Chỉ xử lý nếu item là File (không phải Folder)
                    if (item is Windows.Storage.StorageFile file)
                    {
                        string ext = file.FileType.ToLower();
                        // Validate định dạng hình ảnh
                        if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                        {
                            ViewModel.SelectedImages.Add(file.Path); // Thêm path vào danh sách để UI tự update
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
            // Lấy đối tượng Button vừa được click
            if (sender is Button btn)
            {
                // Mặc định DataContext của DataTemplate chính là object item của danh sách (ở đây là đường dẫn string)
                if (btn.DataContext is string imagePath)
                {
                    ViewModel.SelectedImages.Remove(imagePath);
                }
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
    }
}