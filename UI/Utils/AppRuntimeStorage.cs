using System;
using System.IO;
using System.Text.Json.Nodes;
using Windows.Storage;

namespace UI.Utils;

public static class AppRuntimeStorage
{
    private static readonly object SyncRoot = new();
    private static readonly string FallbackDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EasyStore");
    private static readonly string FallbackSettingsFilePath = Path.Combine(FallbackDirectory, "localsettings.json");

    private static bool? _canUseWindowsAppData;

    private static bool CanUseWindowsAppData
    {
        get
        {
            if (_canUseWindowsAppData.HasValue)
            {
                return _canUseWindowsAppData.Value;
            }

            try
            {
                _ = ApplicationData.Current.LocalSettings;
                _canUseWindowsAppData = true;
            }
            catch
            {
                _canUseWindowsAppData = false;
            }

            return _canUseWindowsAppData.Value;
        }
    }

    public static string? GetString(string key, string? defaultValue = null)
    {
        if (CanUseWindowsAppData)
        {
            return ApplicationData.Current.LocalSettings.Values[key] as string ?? defaultValue;
        }

        lock (SyncRoot)
        {
            var root = LoadFallbackSettings();
            if (root.TryGetPropertyValue(key, out var valueNode))
            {
                return valueNode?.GetValue<string>() ?? defaultValue;
            }

            return defaultValue;
        }
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
        if (CanUseWindowsAppData)
        {
            return ApplicationData.Current.LocalSettings.Values[key] as int? ?? defaultValue;
        }

        lock (SyncRoot)
        {
            var root = LoadFallbackSettings();
            if (root.TryGetPropertyValue(key, out var valueNode) && valueNode != null)
            {
                try
                {
                    return valueNode.GetValue<int>();
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }
    }

    public static bool GetBool(string key, bool defaultValue = false)
    {
        if (CanUseWindowsAppData)
        {
            return ApplicationData.Current.LocalSettings.Values[key] as bool? ?? defaultValue;
        }

        lock (SyncRoot)
        {
            var root = LoadFallbackSettings();
            if (root.TryGetPropertyValue(key, out var valueNode) && valueNode != null)
            {
                try
                {
                    return valueNode.GetValue<bool>();
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }
    }

    public static void SetValue(string key, string value)
    {
        if (CanUseWindowsAppData)
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
            return;
        }

        lock (SyncRoot)
        {
            var root = LoadFallbackSettings();
            root[key] = value;
            SaveFallbackSettings(root);
        }
    }

    public static void SetValue(string key, int value)
    {
        if (CanUseWindowsAppData)
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
            return;
        }

        lock (SyncRoot)
        {
            var root = LoadFallbackSettings();
            root[key] = value;
            SaveFallbackSettings(root);
        }
    }

    public static void SetValue(string key, bool value)
    {
        if (CanUseWindowsAppData)
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
            return;
        }

        lock (SyncRoot)
        {
            var root = LoadFallbackSettings();
            root[key] = value;
            SaveFallbackSettings(root);
        }
    }

    public static void RemoveValue(string key)
    {
        if (CanUseWindowsAppData)
        {
            ApplicationData.Current.LocalSettings.Values.Remove(key);
            return;
        }

        lock (SyncRoot)
        {
            var root = LoadFallbackSettings();
            if (root.Remove(key))
            {
                SaveFallbackSettings(root);
            }
        }
    }

    public static string GetTemporaryFolderPath()
    {
        if (CanUseWindowsAppData)
        {
            return ApplicationData.Current.TemporaryFolder.Path;
        }

        var tempPath = Path.Combine(Path.GetTempPath(), "EasyStore");
        Directory.CreateDirectory(tempPath);
        return tempPath;
    }

    private static JsonObject LoadFallbackSettings()
    {
        Directory.CreateDirectory(FallbackDirectory);

        if (!File.Exists(FallbackSettingsFilePath))
        {
            return new JsonObject();
        }

        var json = File.ReadAllText(FallbackSettingsFilePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(json) as JsonObject ?? new JsonObject();
    }

    private static void SaveFallbackSettings(JsonObject root)
    {
        Directory.CreateDirectory(FallbackDirectory);
        File.WriteAllText(FallbackSettingsFilePath, root.ToJsonString());
    }
}
