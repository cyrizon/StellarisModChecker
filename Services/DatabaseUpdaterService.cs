using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Serilog;

namespace StellarisModChecker.Services;

public class DatabaseMetadata
{
    [JsonPropertyName("version")]
    public int Version { get; set; }
    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;
}

public class DatabaseUpdaterService
{
    private readonly HttpClient _httpClient;
    private readonly string _localDbPath;
    private readonly string _localVersionPath;

    private const string MetadataUrl = "https://raw.githubusercontent.com/cyrizon/StellarisModChecker/main/mod_cache_version.json";

    public DatabaseUpdaterService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "StellarisModCheckerApp");

        string appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StellarisModChecker"
        );
        Directory.CreateDirectory(appDataFolder);

        _localDbPath = Path.Combine(appDataFolder, "mod_cache.sqlite");
        _localVersionPath = Path.Combine(appDataFolder, "mod_cache_version.json");
    }

    /// <summary>
    /// Vérifie et télécharge la dernière version de la BDD si nécessaire.
    /// </summary>
    public async Task CheckAndDownloadDatabaseAsync()
    {
        try
        {
            Log.Information("Vérification des mises à jour de la BDD distante sur GitHub...");

            string jsonRemote = await _httpClient.GetStringAsync(MetadataUrl);
            var remoteMeta = JsonSerializer.Deserialize<DatabaseMetadata>(jsonRemote,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (remoteMeta == null || string.IsNullOrEmpty(remoteMeta.DownloadUrl))
            {
                Log.Warning("Métadonnées de la BDD distante invalides ou introuvables.");
                return;
            }

            int localVersion = GetLocalVersion();
            Log.Debug("BDD Cache version locale : {LocalVersion} | version distante : {RemoteVersion}", localVersion,
                remoteMeta.Version);

            if (remoteMeta.Version > localVersion || !File.Exists(_localDbPath))
            {
                Log.Information("[BDD Sync] Téléchargement de la BDD v{Version} depuis {Url}...", remoteMeta.Version,
                    remoteMeta.DownloadUrl);

                byte[] dbBytes = await _httpClient.GetByteArrayAsync(remoteMeta.DownloadUrl);

                await File.WriteAllBytesAsync(_localDbPath, dbBytes);
                await File.WriteAllTextAsync(_localVersionPath, jsonRemote);

                Log.Information("[BDD Sync] Mise à jour de la BDD effectuée avec succès (v{Version}) !",
                    remoteMeta.Version);
            }
            else
            {
                Log.Information("[BDD Sync] La base de données locale est à jour.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Impossible de vérifier ou télécharger la BDD distante sur GitHub.");
        }
    }

    private int GetLocalVersion()
    {
        if (!File.Exists(_localVersionPath)) return 0;

        try
        {
            string jsonLocal = File.ReadAllText(_localVersionPath);
            var localMeta = JsonSerializer.Deserialize<DatabaseMetadata>(jsonLocal, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return localMeta?.Version ?? 0;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Impossible de lire la version locale de la BDD dans {Path}", _localVersionPath);
            return 0;
        }
    }
}