using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.Storage;

namespace UI.Services.ImageCacheService;

public class SupabaseUploadService
{
    private static readonly HttpClient _client = new HttpClient();

    public static async Task<string?> UploadImageAsync(StorageFile localFile)
    {
        try
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            string? supabaseUri = localSettings.Values["SupabaseUri"]?.ToString();
            string? supabaseKey = localSettings.Values["SupabaseApiKey"]?.ToString();

            if (string.IsNullOrEmpty(supabaseUri) || string.IsNullOrEmpty(supabaseKey))
            {
                System.Diagnostics.Debug.WriteLine("[Upload] Thiếu cấu hình Supabase (URI hoặc API Key)!");
                return null;
            }

            string fileExtension = Path.GetExtension(localFile.Name);
            string newFileName = $"img_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{fileExtension}";

            string baseUrl = supabaseUri.TrimEnd('/');

            string requestUrl = $"{baseUrl}/storage/v1/object/avatars/{newFileName}";

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);
            request.Headers.Add("apikey", supabaseKey);

            using var stream = await localFile.OpenStreamForReadAsync();
            var content = new StreamContent(stream);

            string mimeType = fileExtension.ToLower() switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                _ => "image/jpeg"
            };
            content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            request.Content = content;

            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[Upload] Thành công: {newFileName}");
                return newFileName;
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[Upload] Thất bại: Mã lỗi {response.StatusCode} - {error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Upload] Lỗi Exception: {ex.Message}");
            return null;
        }
    }
}
