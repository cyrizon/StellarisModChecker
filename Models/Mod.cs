namespace StellarisModChecker.Models;

public class Mod
{
    public string SteamId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int? Position { get; set; }
}