using System.Text.Json.Serialization;

namespace StellarisModChecker.Models;

public class DatabaseMetadata
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("last_updated")]
    public string LastUpdated { get; set; } = string.Empty;
}