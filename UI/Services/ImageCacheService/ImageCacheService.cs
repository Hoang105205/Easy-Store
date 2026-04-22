using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace UI.Services.ImageCacheService;

public class ImageCacheService
{
    private static readonly HttpClient _client = new HttpClient();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private static readonly string _cacheFolderPath = ApplicationData.Current.LocalFolder.Path;

    public static async Task<string?> GetImagePathAsync(string? imagePathFromDb)
    {
        if (string.IsNullOrWhiteSpace(imagePathFromDb)) return null;

        if (imagePathFromDb.StartsWith("ms-appx:", StringComparison.OrdinalIgnoreCase) ||
            imagePathFromDb.StartsWith("ms-appdata:", StringComparison.OrdinalIgnoreCase))
        {
            return imagePathFromDb;
        }

        if (Path.IsPathRooted(imagePathFromDb))
        {
            return File.Exists(imagePathFromDb) ? imagePathFromDb : null;
        }

        string fileName = Path.GetFileName(imagePathFromDb);
        var filePath = Path.Combine(_cacheFolderPath, fileName);
        string msAppDataUri = $"ms-appdata:///local/{fileName}";

        if (File.Exists(filePath)) return msAppDataUri;

        var fileLock = _locks.GetOrAdd(fileName, _ => new SemaphoreSlim(1, 1));
        await fileLock.WaitAsync().ConfigureAwait(false);

        try
        {
            if (File.Exists(filePath)) return msAppDataUri;

            var localSettings = ApplicationData.Current.LocalSettings;
            string? supabaseUri = localSettings.Values["SupabaseUri"]?.ToString();
            string? supabaseKey = localSettings.Values["SupabaseApiKey"]?.ToString();

            if (string.IsNullOrEmpty(supabaseUri) || string.IsNullOrEmpty(supabaseKey)) return null;

            string imgUrl = $"{supabaseUri.TrimEnd('/')}/storage/v1/object/public/avatars/{fileName}";

            using var request = new HttpRequestMessage(HttpMethod.Get, imgUrl);
            request.Headers.Add("apikey", supabaseKey);

            var response = await _client.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                string tempPath = Path.Combine(_cacheFolderPath, $"{Guid.NewGuid()}.tmp");
                await File.WriteAllBytesAsync(tempPath, bytes).ConfigureAwait(false);

                if (File.Exists(filePath)) File.Delete(filePath);
                File.Move(tempPath, filePath);

                return msAppDataUri;
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ImageCache] Lỗi: {ex.Message}");
            return null;
        }
        finally
        {
            fileLock.Release();
        }
    }
}