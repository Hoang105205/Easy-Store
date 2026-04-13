using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UI.Services.ProfileService;

namespace UI.ViewModels.Profile;

public partial class ProfileViewModel : ObservableObject
{
    private readonly UserService _userService;

    // Các thuộc tính cho Binding
    [ObservableProperty] private string currentPassword = string.Empty;
    [ObservableProperty] private string newPassword = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;
    [ObservableProperty] private bool isLoading;

    // Thuộc tính điều khiển InfoBar
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool isStatusOpen;
    [ObservableProperty] private InfoBarSeverity statusSeverity;

    public ProfileViewModel(UserService userService)
    {
        _userService = userService;
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        // 1. Validation tại chỗ (Client-side)
        if (!ValidateInputs()) return;

        IsLoading = true;

        // 2. Gọi Service
        var (isSuccess, message) = await _userService.ChangePasswordAsync(CurrentPassword, NewPassword);

        // 3. Xử lý kết quả
        if (isSuccess)
        {
            HandleSuccess(message);
        }
        else
        {
            HandleError(message);
        }

        IsLoading = false;
    }

    // --- CÁC HÀM HỖ TRỢ (PRIVATE METHODS) ---

    private bool ValidateInputs()
    {
        Debug.WriteLine("CurrentPassword: " + CurrentPassword);
        Debug.WriteLine("NewPassword: " + NewPassword);

        if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
        {
            ShowStatus("Vui lòng điền đầy đủ thông tin mật khẩu.", InfoBarSeverity.Warning);
            return false;
        }

        if (NewPassword.Length < 6)
        {
            ShowStatus("Mật khẩu mới phải từ 6 ký tự trở lên.", InfoBarSeverity.Warning);
            return false;
        }

        if (NewPassword != ConfirmPassword)
        {
            ShowStatus("Mật khẩu xác nhận không khớp với mật khẩu mới.", InfoBarSeverity.Error);
            return false;
        }

        IsStatusOpen = false; // Đóng infobar nếu mọi thứ hợp lệ
        return true;
    }

    private void HandleSuccess(string message)
    {
        ShowStatus(message, InfoBarSeverity.Success);

        // Xóa trắng các ô nhập liệu sau khi thành công
        CurrentPassword = NewPassword = ConfirmPassword = string.Empty;
    }

    private void HandleError(string message)
    {
        ShowStatus(message, InfoBarSeverity.Error);
    }

    private void ShowStatus(string message,InfoBarSeverity severity)
    {
        StatusMessage = message;
        StatusSeverity = severity;
        IsStatusOpen = true;
    }
}
