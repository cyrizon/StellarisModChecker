using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Serilog;

namespace StellarisModChecker.Services;

public class SteamWorkshopService
{
    private readonly HttpClient _httpClient;
    private readonly ModCacheRepository _cacheRepository;
    private readonly int _delayMs;

    public SteamWorkshopService(ModCacheRepository cacheRepository, int delayMs = 800)
    {
        _cacheRepository = cacheRepository;
        _delayMs = delayMs;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    /// <summary>
    /// Récupère les Steam IDs des "Required items" avec un Rate Limiter pour éviter l'erreur 429.
    /// </summary>
    public async Task<List<string>> GetRequiredItemIdsAsync(string modSteamId)
    {
        if (_cacheRepository.IsModCached(modSteamId))
        {
            Log.Debug("[Cache HIT] Dépendances lues en BDD locale pour le mod {ModId}", modSteamId);
            return _cacheRepository.GetCachedDependencies(modSteamId);
        }
        
        Log.Information("[Cache MISS] Scraping du Workshop Steam pour le mod {ModId}...", modSteamId);
        var requiredIds = new List<string>();
        string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={modSteamId}";

        try
        {
            await Task.Delay(_delayMs);

            using var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                int randomDelay = Random.Shared.Next(10000, 15001);
                Log.Warning("[Steam Scraping] Rate Limit 429 détecté pour {ModId}. Pause de sécurité de {Delay}ms...", modSteamId, randomDelay);
                await Task.Delay(randomDelay);
                return requiredIds; // On évite de crash, le mod sera retenté plus tard
            }

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("[Steam Scraping] Réponse HTTP {StatusCode} pour le mod {ModId}", response.StatusCode, modSteamId);
                return requiredIds;
            }

            string html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var requiredContainer = doc.DocumentNode.SelectSingleNode("//div[@id='RequiredItems']");

            if (requiredContainer != null)
            {
                var links = requiredContainer.SelectNodes(".//a[@href]");
                if (links != null)
                {
                    foreach (var link in links)
                    {
                        string href = link.GetAttributeValue("href", "");
                        var match = Regex.Match(href, @"id=(\d+)");
                        if (match.Success)
                        {
                            requiredIds.Add(match.Groups[1].Value);
                        }
                    }
                }
            }

            _cacheRepository.SaveDependencies(modSteamId, requiredIds);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors du scraping du mod {ModId} sur Steam", modSteamId);
        }

        return requiredIds;
    }
    
    /// <summary>
    /// Récupère les titres et détails de plusieurs mods via l'API officielle de Steam (1 seule requête).
    /// </summary>
    public async Task<Dictionary<string, (string Title, string VersionTag)>> GetModDetailsBatchAsync(IEnumerable<string> modIds)
    {
        var result = new Dictionary<string, (string Title, string VersionTag)>();
        var idsList = modIds.Distinct().ToList();

        if (idsList.Count == 0) return result;

        try
        {
            Log.Information("Appel API Batch Steam pour récupérer les détails de {Count} mod(s)...", idsList.Count);
            
            var formData = new List<KeyValuePair<string, string>>
            {
                new("itemcount", idsList.Count.ToString())
            };

            for (int i = 0; i < idsList.Count; i++)
            {
                formData.Add(new KeyValuePair<string, string>($"publishedfileids[{i}]", idsList[i]));
            }

            var content = new FormUrlEncodedContent(formData);
            using var response = await _httpClient.PostAsync("https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/", content);

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("Erreur HTTP {StatusCode} lors de l'appel API Batch Steam", response.StatusCode);
                return result;
            }

            string jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);

            if (doc.RootElement.TryGetProperty("response", out var resp) &&
                resp.TryGetProperty("publishedfiledetails", out var details))
            {
                foreach (var item in details.EnumerateArray())
                {
                    string id = item.TryGetProperty("publishedfileid", out var idProp) ? idProp.GetString() ?? "" : "";
                    string title = item.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";

                    if (string.IsNullOrEmpty(id)) continue;

                    string versionTag = "Non installé";
                    if (item.TryGetProperty("tags", out var tags))
                    {
                        foreach (var tagObj in tags.EnumerateArray())
                        {
                            if (tagObj.TryGetProperty("tag", out var tagVal))
                            {
                                string tagStr = tagVal.GetString() ?? "";
                                if (Regex.IsMatch(tagStr, @"^\d+\.\d+"))
                                {
                                    versionTag = tagStr;
                                    break;
                                }
                            }
                        }
                    }

                    result[id] = (string.IsNullOrEmpty(title) ? $"Mod #{id}" : title, versionTag);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors de la récupération des détails des mods via l'API Batch Steam");
        }

        return result;
    }
}