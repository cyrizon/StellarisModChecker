using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Serilog;
using StellarisModChecker.Models;

public class PlaysetRepository : IPlaysetRepository
{
    private readonly string _playsetPath;
    private Dictionary<string, string> playsets = new Dictionary<string, string>();

    public PlaysetRepository(string playsetPath)
    {
        _playsetPath = playsetPath;
    }

    public void LoadPlaysets()
    {
        try
        {
            using var db = new SqliteConnection($"Filename={_playsetPath}");
            db.Open();

            var newPlaysets = new Dictionary<string, string>();
            string selectPlaysetsQuery = "SELECT id, name FROM playsets";

            using (var selectCommand = new SqliteCommand(selectPlaysetsQuery, db))
            using (var query = selectCommand.ExecuteReader())
            {
                while (query.Read())
                {
                    string id = query.GetString(0);
                    string playsetName = query.GetString(1);
                    newPlaysets.Add(id, playsetName);
                }
            }

            this.playsets = newPlaysets;
            Log.Information("{Count} playset(s) chargé(s) depuis la base de données Stellaris", playsets.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors de la lecture des playsets dans {DbPath}", _playsetPath);
        }
    }

    public Dictionary<string, string> GetPlaysets()
    {
        return playsets;
    }

    public List<string> GetPlaysetsID()
    {
        return new List<string>(playsets.Keys);
    }
    
    public List<Mod> GetModsForPlayset(string playsetId)
    {
        var mods = new List<Mod>();

        try
        {
            using var db = new SqliteConnection($"Filename={_playsetPath}");
            db.Open();

            string selectModsQuery = @"
                SELECT m.steamId, m.displayName, m.version, pm.enabled, pm.position
                FROM playsets_mods pm
                JOIN mods m ON pm.modId = m.id
                WHERE pm.playsetId = @PlaysetId
                ORDER BY pm.position ASC
            ";

            using var selectCommand = new SqliteCommand(selectModsQuery, db);
            selectCommand.Parameters.AddWithValue("@PlaysetId", playsetId);

            using var query = selectCommand.ExecuteReader();
            while (query.Read())
            {
                mods.Add(new Mod
                {
                    SteamId = query.IsDBNull(0) ? "" : query.GetString(0),
                    DisplayName = query.IsDBNull(1) ? "Mod sans nom" : query.GetString(1),
                    Version = query.IsDBNull(2) ? "N/A" : query.GetString(2),
                    IsEnabled = !query.IsDBNull(3) && query.GetBoolean(3),
                    Position = query.IsDBNull(4) ? null : query.GetInt32(4)
                });
            }

            Log.Debug("Récupération de {Count} mods pour le playset ID: {PlaysetId}", mods.Count, playsetId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors de la récupération des mods du playset {PlaysetId}", playsetId);
        }

        return mods;
    }
}