using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

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

    public void LoadPlaysetContents(string playsetId)
    {
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
                        string modId = query.GetString(0);
                        string displayName = query.IsDBNull(1) ? "" : query.GetString(1);
                        string version = query.IsDBNull(2) ? "" : query.GetString(2);
                        bool enabled = !query.IsDBNull(3) && query.GetBoolean(3);
                        int? position = query.IsDBNull(4) ? null : query.GetInt32(4);
                        Console.WriteLine($"Mod in playset {playsetId}: id={modId}, displayName={displayName}, version={version}, enabled={enabled}, position={position}");
                    }
                }
            }
            db.Close();
        }
    }
}