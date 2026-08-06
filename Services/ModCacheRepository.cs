using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace StellarisModChecker.Services;

public class ModCacheRepository
{
    private readonly string _dbPath;

    public ModCacheRepository(string? dbPath = null)
    {
        if (string.IsNullOrEmpty(dbPath))
        {
            // 1. Récupère le dossier AppData/Local (ou .local/share sur Linux)
            string appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "StellarisModChecker"
            );

            // 2. S'assure que le dossier existe
            Directory.CreateDirectory(appDataFolder);

            // 3. Fichier BDD final
            _dbPath = Path.Combine(appDataFolder, "mod_cache.sqlite");
        }
        else
        {
            _dbPath = dbPath;
        }
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Filename={_dbPath}");
        connection.Open();

        string createTablesQuery = @"
            CREATE TABLE IF NOT EXISTS cached_mods (
                steam_id TEXT PRIMARY KEY,
                last_scanned DATETIME
            );

            CREATE TABLE IF NOT EXISTS mod_dependencies (
                mod_id TEXT,
                required_mod_id TEXT,
                PRIMARY KEY (mod_id, required_mod_id)
            );
        ";

        using var command = new SqliteCommand(createTablesQuery, connection);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Vérifie si un mod est déjà en cache local.
    /// </summary>
    public bool IsModCached(string steamId)
    {
        using var connection = new SqliteConnection($"Filename={_dbPath}");
        connection.Open();

        string query = "SELECT COUNT(1) FROM cached_mods WHERE steam_id = @SteamId";
        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@SteamId", steamId);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    /// <summary>
    /// Récupère la liste des mods requis depuis la BDD locale.
    /// </summary>
    public List<string> GetCachedDependencies(string steamId)
    {
        var dependencies = new List<string>();

        using var connection = new SqliteConnection($"Filename={_dbPath}");
        connection.Open();

        string query = "SELECT required_mod_id FROM mod_dependencies WHERE mod_id = @ModId";
        using var command = new SqliteCommand(query, connection);
        command.Parameters.AddWithValue("@ModId", steamId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            dependencies.Add(reader.GetString(0));
        }

        return dependencies;
    }

    /// <summary>
    /// Enregistre les dépendances d'un mod dans la BDD locale.
    /// </summary>
    public void SaveDependencies(string steamId, List<string> requiredIds)
    {
        using var connection = new SqliteConnection($"Filename={_dbPath}");
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Enregistrer l'ID dans la liste des mods connus
            string insertMod = "INSERT OR REPLACE INTO cached_mods (steam_id, last_scanned) VALUES (@SteamId, @Now)";
            using (var cmd = new SqliteCommand(insertMod, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@SteamId", steamId);
                cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
                cmd.ExecuteNonQuery();
            }

            // Enregistrer les relations
            foreach (var reqId in requiredIds)
            {
                string insertDep = "INSERT OR IGNORE INTO mod_dependencies (mod_id, required_mod_id) VALUES (@ModId, @ReqId)";
                using var cmd = new SqliteCommand(insertDep, connection, transaction);
                cmd.Parameters.AddWithValue("@ModId", steamId);
                cmd.Parameters.AddWithValue("@ReqId", reqId);
                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}