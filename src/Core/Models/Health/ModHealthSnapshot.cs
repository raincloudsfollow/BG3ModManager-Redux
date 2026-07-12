namespace DivinityModManager.Models.Health;

/// <summary>
/// Immutable result of evaluating one mod at one point in time.
/// </summary>
public sealed class ModHealthSnapshot
{
	public DivinityModData Mod { get; }
	public IReadOnlyList<ModHealthFinding> Findings { get; }
	public bool HasFindings => Findings.Count > 0;
	public int ErrorCount => Findings.Count(finding => finding.Severity == ModHealthSeverity.Error);
	public int WarningCount => Findings.Count(finding => finding.Severity == ModHealthSeverity.Warning);
	public int InfoCount => Findings.Count(finding => finding.Severity == ModHealthSeverity.Info);
	public ModHealthSeverity HighestSeverity => Findings.Count == 0
		? ModHealthSeverity.Info
		: Findings.Max(finding => finding.Severity);

	public ModHealthSnapshot(DivinityModData mod, IEnumerable<ModHealthFinding> findings)
	{
		Mod = mod ?? throw new ArgumentNullException(nameof(mod));
		Findings = (findings ?? Enumerable.Empty<ModHealthFinding>()).ToArray();
	}
}
