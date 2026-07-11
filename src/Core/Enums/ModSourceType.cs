using System.ComponentModel;

namespace DivinityModManager;

public enum ModSourceType
{
	[Description("None")]
	NONE,
	[Description("Steam Workshop")]
	STEAM,
	[Description("Nexus Mods")]
	NEXUSMODS,
	[Description("mod.io")]
	MODIO,
	[Description("Github")]
	GITHUB
}
