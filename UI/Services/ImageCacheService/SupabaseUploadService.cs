using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Threading.Tasks;
using UI.Utils;
using Windows.Storage;

namespace UI.Services.ImageCacheService;

public class SupabaseUploadService
{
    private static readonly HttpClient _client = new HttpClient();

    public static async Task<string?> UploadImageAsync(StorageFile localFile)
    {
        try
        {
            string? supabaseUri = AppRuntimeStorage.GetString("SupabaseUri", "");
            string? supabaseKey = AppRuntimeStorage.GetString("SupabaseApiKey", "");

            if (string.IsNullOrEmpty(supabaseUri) || string.IsNullOrEmpty(supabaseKey))
            {
                Debug.WriteLine("[Upload] Thiếu cấu hình Supabase (URI hoặc API Key)!");
                return null;
            }

            string fileExtension = Path.GetExtension(localFile.Name);
            string newFileName = $"img_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{fileExtension}";

            string baseUrl = supabaseUri.TrimEnd('/');

            string requestUrl = $"{baseUrl}/storage/v1/object/images/{newFileName}";

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
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
                Debug.WriteLine($"[Upload] Thành công: {newFileName}");
                return newFileName;
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[Upload] Thất bại: Mã lỗi {response.StatusCode} - {error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Upload] Lỗi Exception: {ex.Message}");
            return null;
        }
    }

    public static async Task<bool> DeleteImageAsync(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;

        try
        {
            string? supabaseUri = AppRuntimeStorage.GetString("SupabaseUri", "");
            string? supabaseKey = AppRuntimeStorage.GetString("SupabaseApiKey", "");

            if (string.IsNullOrEmpty(supabaseUri) || string.IsNullOrEmpty(supabaseKey))
            {
                Debug.WriteLine("[Delete] Thiếu cấu hình Supabase (URI hoặc API Key)!");
                return false;
            }

            string baseUrl = supabaseUri.TrimEnd('/');
            string safeFileName = Uri.EscapeDataString(fileName);

            string requestUrl = $"{baseUrl}/storage/v1/object/images/{safeFileName}";

            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
            request.Headers.Add("apikey", supabaseKey);

            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[Delete] Thành công: {fileName}");
                return true;
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[Delete] Thất bại: Mã lỗi {response.StatusCode} - {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Delete] Lỗi Exception: {ex.Message}");
            return false;
        }
    }
}
