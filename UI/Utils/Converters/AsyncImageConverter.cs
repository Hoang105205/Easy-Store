using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading.Tasks;
using UI.Services.ImageCacheService;

namespace UI.Utils.Converters;

public class AsyncImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        string? imageName = value as string;
        if (string.IsNullOrEmpty(imageName)) return null;

        var bitmap = new BitmapImage();

        _ = LoadImageAsync(imageName, bitmap);

        return bitmap;
    }

    private async Task LoadImageAsync(string imagePathFromDb, BitmapImage bitmap)
    {
        string? finalUriString = await ImageCacheService.GetImagePathAsync(imagePathFromDb);

        if (!string.IsNullOrEmpty(finalUriString))
        {
            bitmap.DispatcherQueue.TryEnqueue(() =>
            {
                bitmap.UriSource = new Uri(finalUriString);
            });
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}