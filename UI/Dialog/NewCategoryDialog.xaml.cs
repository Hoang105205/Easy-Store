using Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using UI.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UI.Dialog
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewCategoryDialog : ContentDialog
    {
        private readonly CategoryViewModel _categoryVM;

        public string CreatedCategoryName { get; private set; } = string.Empty;

        public NewCategoryDialog(CategoryViewModel categoryVM)
        {
            InitializeComponent();

            _categoryVM = categoryVM;
            Title = "Tạo danh mục mới";
            PrimaryButtonText = "Tạo mới";
            CloseButtonText = "Hủy";

            this.PrimaryButtonClick += ContentDialog_PrimaryButtonClick;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            string inputName = TxtNewCategoryName.Text.Trim();

            if (string.IsNullOrEmpty(inputName))
            {
                ShowError("Tên danh mục không được để trống.");
                args.Cancel = true;
                return;
            }

            if (_categoryVM.IsCategoryNameDuplicate(inputName))
            {
                ShowError($"Danh mục '{inputName}' đã tồn tại. Vui lòng chọn tên khác.");
                args.Cancel = true;
                return;
            }

            HideError();
            CreatedCategoryName = inputName;
        }

        private void TxtNewCategoryName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtCategoryError.Visibility == Visibility.Visible)
            {
                TxtCategoryError.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowError(string message)
        {
            TxtCategoryError.Text = message;
            TxtCategoryError.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }

        private void HideError()
        {
            TxtCategoryError.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }
}
