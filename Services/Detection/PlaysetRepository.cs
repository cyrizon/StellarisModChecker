using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
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
        using (SqliteConnection db = new SqliteConnection($"Filename={_playsetPath}"))
        {
            db.Open();

            var playsets = new Dictionary<string, string>();

            string selectPlaysetsQuery = "SELECT id, name FROM playsets";
            using (SqliteCommand selectCommand = new SqliteCommand(selectPlaysetsQuery, db))
            {
                using (SqliteDataReader query = selectCommand.ExecuteReader())
                {
                    while (query.Read())
                    {
                        string id = query.GetString(0);
                        string playsetName = query.GetString(1);
                        playsets.Add(id, playsetName);
                    }
                }
            }
            Console.WriteLine($"Loaded {playsets.Count} playsets from the database.");
            this.playsets = playsets;
            db.Close();
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

        using (SqliteConnection db = new SqliteConnection($"Filename={_playsetPath}"))
        {
            db.Open();

            string selectModsQuery = @"
                SELECT m.steamId, m.displayName, m.version, pm.enabled, pm.position
                FROM playsets_mods pm
                JOIN mods m ON pm.modId = m.id
                WHERE pm.playsetId = @PlaysetId
                ORDER BY pm.position ASC
            ";

            using (SqliteCommand selectCommand = new SqliteCommand(selectModsQuery, db))
            {
                selectCommand.Parameters.AddWithValue("@PlaysetId", playsetId);

                using (SqliteDataReader query = selectCommand.ExecuteReader())
                {
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
                }
            }
            db.Close();
        }

        return mods;
    }
}