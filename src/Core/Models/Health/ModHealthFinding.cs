namespace DivinityModManager.Models.Health;

/// <summary>
/// One immutable, read-only observation about a mod. Findings never perform an action.
/// </summary>
public sealed class ModHealthFinding
{
	public ModHealthFindingCode Code { get; }
	public ModHealthSeverity Severity { get; }
	public string Title { get; }
	public string Message { get; }
	public IReadOnlyList<string> RelatedModUuids { get; }

	public ModHealthFinding(
		ModHealthFindingCode code,
		ModHealthSeverity severity,
		string title,
		string message,
		IEnumerable<string> relatedModUuids = null)
	{
		Code = code;
		Severity = severity;
		Title = title ?? String.Empty;
		Message = message ?? String.Empty;
		RelatedModUuids = (relatedModUuids ?? Enumerable.Empty<string>())
			.Where(uuid => !String.IsNullOrWhiteSpace(uuid))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}
}
