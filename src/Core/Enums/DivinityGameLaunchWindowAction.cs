using Newtonsoft.Json.Converters;

using System.ComponentModel;

namespace DivinityModManager;

[JsonConverter(typeof(StringEnumConverter))]
public enum DivinityGameLaunchWindowAction
{
	[Description("Stay Open")]
	None,
	[Description("Minimize")]
	Minimize,
	[Description("Close Manager")]
	Close
}
