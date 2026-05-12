using System;
using System.Collections.Generic;
using System.Text;
using UI.Utils;

namespace UI.Services.AuthService;

public class AuthService
{
    private const string SessionKey = "UserSessionToken";

    // Lưu trạng thái đăng nhập
    public void SaveSession(string token)
    {
        AppRuntimeStorage.SetValue(SessionKey, token);
    }

    // Lấy thông tin phiên làm việc
    public string? GetSession()
    {
        return AppRuntimeStorage.GetString(SessionKey);
    }

    // Kiểm tra đã đăng nhập chưa
    public bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(GetSession());
    }

    // Đăng xuất
    public void ClearSession()
    {
        AppRuntimeStorage.RemoveValue(SessionKey);
    }
}
