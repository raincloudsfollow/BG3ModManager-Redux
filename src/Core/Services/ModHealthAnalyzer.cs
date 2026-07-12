using DivinityModManager.Models;

namespace DivinityModManager.Models.Health;

/// <summary>
/// Builds read-only health snapshots from facts BG3MM has already detected.
/// It does not mutate mods, load orders, settings, files, or provider metadata.
/// </summary>
public sealed class ModHealthAnalyzer
{
	public IReadOnlyList<ModHealthSnapshot> AnalyzeAll(
		IEnumerable<DivinityModData> installedMods,
		IEnumerable<DivinityModData> activeMods,
		IEnumerable<DivinityModData> duplicateMods = null)
	{
		var installed = (installedMods ?? Enumerable.Empty<DivinityModData>())
			.Where(mod => mod != null && !mod.IsVisualDivider)
			.ToArray();
		var activeUuids = (activeMods ?? Enumerable.Empty<DivinityModData>())
			.Where(mod => mod != null && !String.IsNullOrWhiteSpace(mod.UUID))
			.Select(mod => mod.UUID)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		var installedByUuid = installed
			.Where(mod => !String.IsNullOrWhiteSpace(mod.UUID))
			.GroupBy(mod => mod.UUID, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
		var duplicateUuids = FindDuplicateUuids(installed, duplicateMods);

		return installed
			.Select(mod => Analyze(mod, installedByUuid, activeUuids, duplicateUuids))
			.ToArray();
	}

	private static ModHealthSnapshot Analyze(
		DivinityModData mod,
		IReadOnlyDictionary<string, DivinityModData> installedByUuid,
		IReadOnlySet<string> activeUuids,
		IReadOnlySet<string> duplicateUuids)
	{
		var findings = new List<ModHealthFinding>();

		AddIdentityFindings(mod, duplicateUuids, findings);
		AddDependencyFindings(mod, installedByUuid, activeUuids, findings);
		AddScriptExtenderFindings(mod, findings);
		AddLegacyAndOverrideFindings(mod, findings);
		AddSourceFindings(mod, findings);

		return new ModHealthSnapshot(mod, findings);
	}

	private static void AddIdentityFindings(
		DivinityModData mod,
		IReadOnlySet<string> duplicateUuids,
		ICollection<ModHealthFinding> findings)
	{
		if (mod.HasInvalidUUID)
		{
			findings.Add(new ModHealthFinding(
				ModHealthFindingCode.InvalidUuid,
				ModHealthSeverity.Error,
				"Invalid mod UUID",
				"This package has an invalid UUID and may fail to load or reset the exported load order."));
		}

		if (!String.IsNullOrWhiteSpace(mod.UUID) && duplicateUuids.Contains(mod.UUID))
		{
			findings.Add(new ModHealthFinding(
				ModHealthFindingCode.DuplicateUuid,
				ModHealthSeverity.Error,
				"Duplicate mod UUID",
				"More than one installed package uses this UUID. Redux is only reporting the duplicate; it has not changed either file.",
				new[] { mod.UUID }));
		}
	}

	private static void AddDependencyFindings(
		DivinityModData mod,
		IReadOnlyDictionary<string, DivinityModData> installedByUuid,
		IReadOnlySet<string> activeUuids,
		ICollection<ModHealthFinding> findings)
	{
		foreach (var dependency in mod.MissingDependencies.Items)
		{
			findings.Add(new ModHealthFinding(
				ModHealthFindingCode.MissingDependency,
				ModHealthSeverity.Error,
				"Missing dependency",
				$"{dependency.Name} is listed as a dependency but is not installed.",
				new[] { dependency.UUID }));
		}

		if (mod.IsActive)
		{
			foreach (var dependency in mod.Dependencies.Items)
			{
				if (String.IsNullOrWhiteSpace(dependency.UUID)
					|| !installedByUuid.TryGetValue(dependency.UUID, out var installedDependency)
					|| activeUuids.Contains(dependency.UUID)
					|| installedDependency.IsForceLoaded
					|| installedDependency.IsLarianMod)
				{
					continue;
				}

				findings.Add(new ModHealthFinding(
					ModHealthFindingCode.InactiveDependency,
					ModHealthSeverity.Warning,
					"Dependency is inactive",
					$"{dependency.Name} is installed but is not currently in the active load order.",
					new[] { dependency.UUID }));
			}
		}

		foreach (var conflict in mod.Conflicts.Items)
		{
			findings.Add(new ModHealthFinding(
				ModHealthFindingCode.DeclaredConflict,
				ModHealthSeverity.Warning,
				"Declared conflict",
				$"The package declares a conflict with {conflict.Name}.",
				new[] { conflict.UUID }));
		}
	}

	private static void AddScriptExtenderFindings(DivinityModData mod, ICollection<ModHealthFinding> findings)
	{
		var status = mod.ExtenderModStatus;
		if (status.HasFlag(DivinityExtenderModStatus.DisabledFromConfig)
			|| status.HasFlag(DivinityExtenderModStatus.MissingUpdater))
		{
			findings.Add(new ModHealthFinding(
				ModHealthFindingCode.ScriptExtenderUnavailable,
				ModHealthSeverity.Error,
				"Script Extender unavailable",
				mod.ScriptExtenderSupportToolTipText));
		}
		else if (status.HasFlag(DivinityExtenderModStatus.MissingRequiredVersion)
			|| status.HasFlag(DivinityExtenderModStatus.MissingAppData))
		{
			findings.Add(new ModHealthFinding(
				ModHealthFindingCode.ScriptExtenderVersionMismatch,
				ModHealthSeverity.Warning,
				"Script Extender needs attention",
				mod.ScriptExtenderSupportToolTipText));
		}
	}

	private static void AddLegacyAndOverrideFindings(DivinityModData mod, ICollection<ModHealthFinding> findings)
	{
		if (mod.OsirisModStatus == DivinityOsirisModStatus.MODFIXER)
		{
			findings.Add(new ModHealthFinding(
				ModHealthFindingCode.LegacyModFixerIncluded,
				ModHealthSeverity.Info,
				"Contains Mod Fixer",
				"Mod Fixer files were detected inside this package. BG3 Patch 7 and newer generally do not require Mod Fixer, and it does not need to be installed separately."));
		}

		if (!mod.IsForceLoaded)
		{
			return;
		}

		if (mod.ForceAllowInLoadOrder)
		{
			findings.Add(new ModHealthFinding(
				ModHealthFindingCode.AlwaysLoadedWithLoadOrderEntry,
				ModHealthSeverity.Info,
				"Always Loaded + Load Order Entry",
				"This package overrides game files and is also explicitly allowed in the normal load order."));
		}
		else if (mod.IsForceLoadedMergedMod)
		{
			findings.Add(new ModHealthFinding(
				ModHealthFindingCode.ContainsFileOverrides,
				ModHealthSeverity.Info,
				"Contains File Overrides",
				"Disabling this mod's load-order entry may not disable the files it directly overrides."));
		}
		else
		{
			findings.Add(new ModHealthFinding(
				ModHealthFindingCode.AlwaysLoaded,
				ModHealthSeverity.Info,
				"Always Loaded",
				"This package is loaded because the .pak exists and usually is not written to modsettings.lsx."));
		}
	}

	private static void AddSourceFindings(DivinityModData mod, ICollection<ModHealthFinding> findings)
	{
		if (mod.Metadata.SourceType == ModSourceType.MODIO)
		{
			findings.Add(new ModHealthFinding(
				ModHealthFindingCode.ModioManagedSource,
				ModHealthSeverity.Warning,
				"Limited mod.io support",
				"A subscribed mod.io mod can be restored or redownloaded by Baldur's Gate 3 after its local file is removed."));
		}
	}

	private static HashSet<string> FindDuplicateUuids(
		IEnumerable<DivinityModData> installedMods,
		IEnumerable<DivinityModData> duplicateMods)
	{
		var duplicates = installedMods
			.Where(mod => !String.IsNullOrWhiteSpace(mod.UUID))
			.GroupBy(mod => mod.UUID, StringComparer.OrdinalIgnoreCase)
			.Where(group => group.Count() > 1)
			.Select(group => group.Key)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		foreach (var duplicate in duplicateMods ?? Enumerable.Empty<DivinityModData>())
		{
			if (!String.IsNullOrWhiteSpace(duplicate?.UUID))
			{
				duplicates.Add(duplicate.UUID);
			}
		}

		return duplicates;
	}
}
