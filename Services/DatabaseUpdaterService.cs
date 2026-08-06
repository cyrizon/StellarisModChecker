using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace StellarisModChecker.Services;

public class DatabaseMetadata
{
    public int Version { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

public class DatabaseUpdaterService
{
    private readonly HttpClient _httpClient;
    private readonly string _localDbPath;
    private readonly string _localVersionPath;

    // URL raw pointant vers votre fichier version.json sur GitHub (Release ou Branche main)
    private const string MetadataUrl = "https://raw.githubusercontent.com/cyrizon/StellarisModChecker/main/mod_cache_version.json";

    public DatabaseUpdaterService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "StellarisModCheckerApp");

        // Cibler exactement le même dossier que ModCacheRepository !
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
            // 1. Récupérer les métadonnées sur GitHub
            string jsonRemote = await _httpClient.GetStringAsync(MetadataUrl);
            var remoteMeta = JsonSerializer.Deserialize<DatabaseMetadata>(jsonRemote, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (remoteMeta == null || string.IsNullOrEmpty(remoteMeta.DownloadUrl)) return;

            // 2. Vérifier la version locale
            int localVersion = GetLocalVersion();

            // 3. Télécharger si la version distante est plus récente (ou si la BDD locale n'existe pas)
            if (remoteMeta.Version > localVersion || !File.Exists(_localDbPath))
            {
                Console.WriteLine($"[BDD Sync] Téléchargement de la BDD v{remoteMeta.Version}...");

                byte[] dbBytes = await _httpClient.GetByteArrayAsync(remoteMeta.DownloadUrl);

                // Remplacer le fichier SQLite local au bon endroit
                await File.WriteAllBytesAsync(_localDbPath, dbBytes);

                // Mettre à jour le fichier version local
                await File.WriteAllTextAsync(_localVersionPath, jsonRemote);

                Console.WriteLine("[BDD Sync] Mise à jour de la BDD effectuée avec succès !");
            }
        }
        catch (Exception ex)
        {
            // En cas d'absence de réseau, l'application continue d'utiliser la BDD locale existante
            Console.WriteLine($"[BDD Sync Error] Impossible de vérifier la BDD distants : {ex.Message}");
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
        catch
        {
            return 0;
        }
    }
}