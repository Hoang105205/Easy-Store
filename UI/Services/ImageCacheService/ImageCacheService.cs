using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UI.Utils;

namespace UI.Services.ImageCacheService;

public class ImageCacheService
{
    private static readonly HttpClient _client = new HttpClient();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private static readonly string _cacheFolderPath = EnsureCacheFolder();

    private static string EnsureCacheFolder()
    {
        var folderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EasyStore",
            "ImageCache");

        Directory.CreateDirectory(folderPath);
        return folderPath;
    }

    public static async Task<string?> GetImagePathAsync(string? imagePathFromDb)
    {
        if (string.IsNullOrWhiteSpace(imagePathFromDb)) return null;

        if (imagePathFromDb.StartsWith("ms-appx:", StringComparison.OrdinalIgnoreCase) || 
            imagePathFromDb.StartsWith("ms-appdata:", StringComparison.OrdinalIgnoreCase))
        {
            if (imagePathFromDb.StartsWith("ms-appdata:", StringComparison.OrdinalIgnoreCase))
            {
                string appDataFileName = Path.GetFileName(imagePathFromDb);
                if (string.IsNullOrWhiteSpace(appDataFileName)) return null;

                string appDataFilePath = Path.Combine(_cacheFolderPath, appDataFileName);
                return File.Exists(appDataFilePath) ? appDataFilePath : null;
            }
            if (Uri.TryCreate(imagePathFromDb, UriKind.Absolute, out var appxUri))
            {
                var relativePath = appxUri.AbsolutePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var localAssetPath = Path.Combine(AppContext.BaseDirectory, relativePath);
                return File.Exists(localAssetPath) ? localAssetPath : null;
            }

            return null;
        }

        if (Path.IsPathRooted(imagePathFromDb))
        {
            return File.Exists(imagePathFromDb) ? imagePathFromDb : null;
        }

        string fileName = Path.GetFileName(imagePathFromDb);
        var filePath = Path.Combine(_cacheFolderPath, fileName);

        if (File.Exists(filePath)) return filePath;

        var fileLock = _locks.GetOrAdd(fileName, _ => new SemaphoreSlim(1, 1));
        await fileLock.WaitAsync().ConfigureAwait(false);

        try
        {
            if (File.Exists(filePath)) return filePath;

            string? supabaseUri = AppRuntimeStorage.GetString("SupabaseUri", "");
            string? supabaseKey = AppRuntimeStorage.GetString("SupabaseApiKey", "");

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

                return filePath;
            }
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ImageCache] Lỗi: {ex.Message}");
            return null;
        }
        finally
        {
            fileLock.Release();
        }
    }
}