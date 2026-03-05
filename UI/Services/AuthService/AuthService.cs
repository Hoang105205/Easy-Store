using System;
using System.Collections.Generic;
using System.Text;
using Windows.Storage;

namespace UI.Services.AuthService;

public class AuthService
{
    private const string SessionKey = "UserSessionToken";
    private readonly ApplicationDataContainer _localSettings;

    public AuthService()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
    }

    // Lưu trạng thái đăng nhập
    public void SaveSession(string token)
    {
        _localSettings.Values[SessionKey] = token;
    }

    // Lấy thông tin phiên làm việc
    public string? GetSession()
    {
        return _localSettings.Values[SessionKey] as string;
    }

    // Kiểm tra đã đăng nhập chưa
    public bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(GetSession());
    }

    // Đăng xuất
    public void ClearSession()
    {
        _localSettings.Values.Remove(SessionKey);
    }
}
