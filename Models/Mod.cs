using System.Collections.Generic;

namespace StellarisModChecker.Models;

public class Mod
{
    public string SteamId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int? Position { get; set; }
    public List<string> RequiredByModIds { get; set; } = new();
    public List<string> RequiredByModNames { get; set; } = new();
    public string RequiredBySummary => RequiredByModNames.Count switch
    {
        0 => "Inconnu",
        1 => RequiredByModNames[0],
        _ => $"{RequiredByModNames[0]} (+{RequiredByModNames.Count - 1} autre(s))"
    };
    public string RequiredByToolTipText => RequiredByModNames.Count > 0
        ? "Requis par :\n• " + string.Join("\n• ", RequiredByModNames)
        : "Requis par un mod inconnu";
}