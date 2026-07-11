using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DivinityModManager.Converters;

internal class UriToBitmapImageConverter : IValueConverter
{
	private static readonly Dictionary<string, BitmapImage> ImageCache = new(StringComparer.OrdinalIgnoreCase);

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Uri uri)
		{
			try
			{
				var cacheKey = uri.AbsoluteUri;
				if (ImageCache.TryGetValue(cacheKey, out var cachedBitmap))
				{
					return cachedBitmap;
				}

				var bitmap = new BitmapImage();
				bitmap.BeginInit();
				bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
				bitmap.CacheOption = BitmapCacheOption.OnDemand;
				bitmap.UriSource = uri;
				bitmap.EndInit();
				ImageCache[cacheKey] = bitmap;
				return bitmap;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Failed to create BitmapImage from '{uri}':\n{ex}");
			}
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return "";
	}
}
