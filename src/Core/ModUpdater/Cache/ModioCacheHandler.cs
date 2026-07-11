using DivinityModManager.Models;
using DivinityModManager.Models.Cache;
using DivinityModManager.Util;

using Newtonsoft.Json;

namespace DivinityModManager.ModUpdater.Cache;

public class ModioCacheHandler : IExternalModCacheHandler<ModioCachedData>
{
	public ModSourceType SourceType => ModSourceType.MODIO;
	public string FileName => "modiodata.json";
	public JsonSerializerSettings SerializerSettings => ModUpdateHandler.DefaultSerializerSettings;
	public bool IsEnabled { get; set; }
	public ModioCachedData CacheData { get; set; } = new();
	public string APIKey { get; set; }

	public async Task<bool> Update(IEnumerable<DivinityModData> mods, CancellationToken cancellationToken)
	{
		if (!IsEnabled || String.IsNullOrWhiteSpace(APIKey))
		{
			DivinityApp.Log("mod.io metadata lookup skipped because the provider is disabled or no API key is configured.");
			return false;
		}

		var candidates = mods.Where(mod => mod.PublishHandle > 0 && !mod.ModioData.HasMetadata).ToList();
		DivinityApp.Log($"mod.io metadata lookup found {candidates.Count} candidate mod(s).");

		var changed = false;
		foreach (var mod in candidates)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				DivinityApp.Log($"Requesting mod.io metadata for '{mod.DisplayName}' using PublishHandle {mod.PublishHandle}.");
				var data = await ModioDataLoader.LoadModDataAsync(mod, APIKey, cancellationToken);
				if (data != null)
				{
					mod.ModioData.Update(data);
					CacheData.Mods[mod.UUID] = data;
					changed = true;
					DivinityApp.Log($"Linked mod.io metadata for '{mod.DisplayName}' to mod {data.ModId}.");
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error loading mod.io metadata for '{mod.DisplayName}':\n{ex}");
			}
		}

		return changed;
	}
}
