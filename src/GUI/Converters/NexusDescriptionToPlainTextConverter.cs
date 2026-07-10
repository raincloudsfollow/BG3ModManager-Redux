using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace DivinityModManager.Converters;

public class NexusDescriptionToPlainTextConverter : IValueConverter
{
	private const string EmptyDescriptionText = "No Nexus Mods description is available for this mod.";

	private static readonly Regex ImagePattern = new(@"\[img(?:=[^\]]*)?\].*?\[/img\]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
	private static readonly Regex ListItemStartPattern = new(@"\[li(?:=[^\]]*)?\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	private static readonly Regex ListItemEndPattern = new(@"\[/li\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	private static readonly Regex BlockTagPattern = new(@"\[/?(?:br|p|quote|center|left|right|list|ul|ol|spoiler|h[1-6]|heading)(?:=[^\]]*)?\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	private static readonly Regex RemainingBbCodePattern = new(@"\[[^\]]+\]", RegexOptions.Compiled);
	private static readonly Regex HtmlBreakPattern = new(@"<(?:br\s*/?|/?p|/?div|/?li|/?ul|/?ol|/?blockquote)[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	private static readonly Regex RemainingHtmlPattern = new(@"<[^>]+>", RegexOptions.Compiled);
	private static readonly Regex HorizontalWhitespacePattern = new(@"[ \t]+", RegexOptions.Compiled);
	private static readonly Regex LinePaddingPattern = new(@"[ \t]*\n[ \t]*", RegexOptions.Compiled);
	private static readonly Regex ExcessBlankLinesPattern = new(@"\n{3,}", RegexOptions.Compiled);

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var emptyText = parameter as string ?? EmptyDescriptionText;
		if (value is not string description || String.IsNullOrWhiteSpace(description))
		{
			return emptyText;
		}

		var text = description.Replace("\r\n", "\n").Replace('\r', '\n');
		text = ImagePattern.Replace(text, "\n[Image available on the Nexus Mods page]\n");
		text = ListItemStartPattern.Replace(text, "\n• ");
		text = ListItemEndPattern.Replace(text, "\n");
		text = BlockTagPattern.Replace(text, "\n");
		text = RemainingBbCodePattern.Replace(text, String.Empty);
		text = HtmlBreakPattern.Replace(text, "\n");
		text = RemainingHtmlPattern.Replace(text, String.Empty);
		text = WebUtility.HtmlDecode(text).Replace('\u00A0', ' ');
		text = HorizontalWhitespacePattern.Replace(text, " ");
		text = LinePaddingPattern.Replace(text, "\n");
		text = ExcessBlankLinesPattern.Replace(text, "\n\n");

		return String.IsNullOrWhiteSpace(text) ? emptyText : text.Trim();
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
