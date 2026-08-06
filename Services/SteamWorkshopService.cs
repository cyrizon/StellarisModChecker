using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
}