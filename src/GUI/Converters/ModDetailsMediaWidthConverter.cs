using System.Globalization;
using System.Windows.Data;

namespace DivinityModManager.Converters;

public class ModDetailsMediaWidthConverter : IValueConverter
{
	private const double MinimumWidth = 250;
	private const double MaximumWidth = 620;
	private const double FooterHeight = 29;
	private const double WidescreenAspectRatio = 16d / 9d;

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not double cardHeight || Double.IsNaN(cardHeight) || Double.IsInfinity(cardHeight))
		{
			return MinimumWidth;
		}

		var previewHeight = Math.Max(0, cardHeight - FooterHeight);
		return Math.Clamp(previewHeight * WidescreenAspectRatio, MinimumWidth, MaximumWidth);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
