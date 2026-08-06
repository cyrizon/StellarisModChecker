using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StellarisModChecker.Models;
using StellarisModChecker.Services;
using StellarisModChecker.Services.Detection;

namespace StellarisModChecker.ViewModels;

public partial class PlaysetTabViewModel : ViewModelBase
{
    private readonly PlaysetDetectionService _service;
    private readonly SteamWorkshopService _steamService;

    public string Id { get; }

    [ObservableProperty]
    private string _header;

    [ObservableProperty]
    private ObservableCollection<Mod> _mods = new();

    [ObservableProperty]
    private ObservableCollection<Mod> _missingMods = new();

    [ObservableProperty]
    private bool _isChecking;

    public PlaysetTabViewModel(Playset playset, PlaysetDetectionService service)
    {
        Id = playset.Id;
        _header = playset.Name;
        _service = service;
        // délai aléatoire entre 5000 ms et 7000 ms entre chaque page Steam
        int randomDelay = Random.Shared.Next(5000, 7001);
        var cacheRepo = new ModCacheRepository();
        _steamService = new SteamWorkshopService(cacheRepo, delayMs: randomDelay);

        LoadMods();
        _ = CheckMissingModsAsync();
    }

    private void LoadMods()
    {
        var modData = _service.GetModsForPlayset(Id);
        Mods = new ObservableCollection<Mod>(modData);
    }

    [RelayCommand]
    private async Task CheckMissingModsAsync()
    {
        if (IsChecking) return;
        IsChecking = true;

        MissingMods.Clear();

        // HashSet pour vérifier si un ID est déjà dans notre playset (recherche en O(1))
        var installedModIds = new HashSet<string>(
            Mods.Where(m => !string.IsNullOrEmpty(m.SteamId)).Select(m => m.SteamId)
        );

        // File d'attente pour la descente en cascade
        var toCheckQueue = new Queue<string>(installedModIds);
        
        // Cache local des IDs déjà vérifiés sur Steam pendant cette session
        var scannedSteamIds = new HashSet<string>();

        while (toCheckQueue.Count > 0)
        {
            string currentId = toCheckQueue.Dequeue();

            // Si déjà scanné sur Steam, on passe
            if (scannedSteamIds.Contains(currentId)) continue;
            scannedSteamIds.Add(currentId);

            // Scraping avec temporisation auto (400ms)
            List<string> requiredIds = await _steamService.GetRequiredItemIdsAsync(currentId);

            foreach (var reqId in requiredIds)
            {
                // Si le mod requis n'est PAS dans le playset local
                if (!installedModIds.Contains(reqId))
                {
                    if (!MissingMods.Any(m => m.SteamId == reqId))
                    {
                        MissingMods.Add(new Mod
                        {
                            SteamId = reqId,
                            DisplayName = $"Mod Requis #{reqId}",
                            Version = "Manquant",
                            IsEnabled = false
                        });
                    }
                }

                // Pour la descente en cascade : on vérifie aussi les dépendances de ce mod requis
                if (!scannedSteamIds.Contains(reqId))
                {
                    toCheckQueue.Enqueue(reqId);
                }
            }
        }

        IsChecking = false;
    }
}