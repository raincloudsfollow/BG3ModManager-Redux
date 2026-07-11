using System.ComponentModel;

namespace DivinityModManager.Models.Metadata;

/// <summary>
/// Normalized metadata exposed by an online mod provider. Implementations may
/// retain their provider-specific cache and API models behind this contract.
/// </summary>
public interface IExternalModMetadata : INotifyPropertyChanged
{
	ModSourceType SourceType { get; }
	bool HasMetadata { get; }
	string Name { get; }
	string Author { get; }
	string Version { get; }
	string Summary { get; }
	string Description { get; }
	string ChangelogText { get; }
	DateTime? UpdatedAt { get; }
	string SourcePageUrl { get; }
	string GalleryPageUrl { get; }
	string ChangelogPageUrl { get; }
	string PreviewImageUrl { get; }
}
