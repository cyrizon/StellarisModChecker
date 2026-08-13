using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace StellarisModChecker.Services;

public class SteamWorkshopService
{
    private readonly HttpClient _httpClient;
    private readonly ModCacheRepository _cacheRepository;
    private readonly int _delayMs;

    // delayMs = 400ms par défaut (environ 2.5 requêtes / sec), un rythme très doux pour Steam
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
        // 1. VÉRIFICATION DANS LA BDD LOCALE (0 ms)
        if (_cacheRepository.IsModCached(modSteamId))
        {
            Console.WriteLine($"[Cache HIT] Dépendances lues en BDD pour le mod {modSteamId}");
            return _cacheRepository.GetCachedDependencies(modSteamId);
        }

        // 2. SINON : APPEL À STEAM (Cache MISS)
        Console.WriteLine($"[Cache MISS] Scraping de Steam pour le mod {modSteamId}...");
        var requiredIds = new List<string>();
        string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={modSteamId}";

        try
        {
            await Task.Delay(_delayMs); // Politesse vis-à-vis de Steam

            using var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                int randomDelay = Random.Shared.Next(10000, 15001);
                Console.WriteLine($"[Rate Limit 429] Pause forcée de {randomDelay / 1000} secondes pour l'IP...");
                await Task.Delay(randomDelay);
                return requiredIds; // On évite de crash, le mod sera retenté plus tard
            }

            if (!response.IsSuccessStatusCode) return requiredIds;

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

            // 3. SAUVEGARDE DANS LA BDD LOCALE POUR LES PROCHAINES FOIS
            _cacheRepository.SaveDependencies(modSteamId, requiredIds);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du scraping du mod {modSteamId} : {ex.Message}");
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

            if (!response.IsSuccessStatusCode) return result;

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

                    // Extraction éventuelle de la version de Stellaris dans les tags (ex: "4.4.*")
                    string versionTag = "Non installé";
                    if (item.TryGetProperty("tags", out var tags))
                    {
                        foreach (var tagObj in tags.EnumerateArray())
                        {
                            if (tagObj.TryGetProperty("tag", out var tagVal))
                            {
                                string tagStr = tagVal.GetString() ?? "";
                                // Détection si le tag ressemble à une version (ex: 3.12, 4.4.*, etc.)
                                if (System.Text.RegularExpressions.Regex.IsMatch(tagStr, @"^\d+\.\d+"))
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
            Console.WriteLine($"Erreur lors de la récupération des titres Steam : {ex.Message}");
        }

        return result;
    }
}