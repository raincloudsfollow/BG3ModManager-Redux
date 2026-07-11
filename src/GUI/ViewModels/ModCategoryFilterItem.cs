namespace DivinityModManager.ViewModels;

/// <summary>
/// A category shown in the Redux category navigator. This is view metadata only;
/// it never changes a mod's position in the underlying load order.
/// </summary>
public sealed class ModCategoryFilterItem
{
	public string Name { get; }
	public string DisplayCategory => Name;
	public int Count { get; }
	public string Color { get; }
	public string SoftColor => String.IsNullOrWhiteSpace(Color) ? "#243A3346" : $"#33{Color.TrimStart('#')}";
	public bool HasNewMods { get; }

	public ModCategoryFilterItem(string name, int count, string color, bool hasNewMods = false)
	{
		Name = name;
		Count = count;
		Color = color;
		HasNewMods = hasNewMods;
	}
}
