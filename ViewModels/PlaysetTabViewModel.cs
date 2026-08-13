using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using StellarisModChecker.Data;
using StellarisModChecker.Models;
using StellarisModChecker.Services;

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
        Log.Information("Chargement de {Count} mods pour le playset '{PlaysetName}' (ID: {PlaysetId})", Mods.Count, Header, Id);
    }

    [RelayCommand]
    private async Task CheckMissingModsAsync()
    {
        if (IsChecking) return;
        IsChecking = true;
        
        Log.Information("Début de la vérification des dépendances pour le playset '{PlaysetName}'", Header);

        try
        {
            LoadMods();
            MissingMods.Clear();

            // Dictionnaire rapide : SteamId -> Objet Mod du playset
            var installedModsDict = Mods
                .Where(m => !string.IsNullOrEmpty(m.SteamId))
                .GroupBy(m => m.SteamId)
                .ToDictionary(g => g.Key, g => g.First());

            var installedModIds = new HashSet<string>(installedModsDict.Keys);

            var toCheckQueue = new Queue<string>(installedModIds);
            var scannedSteamIds = new HashSet<string>();

            while (toCheckQueue.Count > 0)
            {
                string currentId = toCheckQueue.Dequeue();

                if (scannedSteamIds.Contains(currentId)) continue;
                scannedSteamIds.Add(currentId);

                // Nom du mod parent s'il est dans le playset local (pour un affichage clair)
                string parentModName = installedModsDict.TryGetValue(currentId, out var parentMod)
                    ? parentMod.DisplayName
                    : $"Mod #{currentId}";

                List<string> requiredIds = await _steamService.GetRequiredItemIdsAsync(currentId);

                foreach (var reqId in requiredIds)
                {
                    // Si le mod requis n'est PAS installé dans le playset
                    if (!installedModIds.Contains(reqId))
                    {
                        var existingMissingMod = MissingMods.FirstOrDefault(m => m.SteamId == reqId);

                        if (existingMissingMod == null)
                        {
                            // Création du mod manquant
                            var newMissingMod = new Mod
                            {
                                SteamId = reqId,
                                DisplayName = $"Chargement du nom... (#{reqId})",
                                Version = "Non installé",
                                IsEnabled = false
                            };

                            newMissingMod.RequiredByModIds.Add(currentId);
                            newMissingMod.RequiredByModNames.Add(parentModName);

                            MissingMods.Add(newMissingMod);
                            Log.Debug("Mod manquant détecté : {MissingId} (Requis par '{ParentName}')", reqId,
                                parentModName);
                        }
                        else
                        {
                            // S'il avait déjà été identifié comme manquant par un AUTRE mod, on ajoute ce nouveau parent !
                            if (!existingMissingMod.RequiredByModIds.Contains(currentId))
                            {
                                existingMissingMod.RequiredByModIds.Add(currentId);
                                existingMissingMod.RequiredByModNames.Add(parentModName);
                            }
                        }
                    }

                    // Poursuite de la cascade
                    if (!scannedSteamIds.Contains(reqId))
                    {
                        toCheckQueue.Enqueue(reqId);
                    }
                }
            }

            // Enrichissement final des noms via l'API Batch de Steam
            if (MissingMods.Count > 0)
            {
                Log.Information("Récupération des métadonnées Steam pour {Count} mods manquants...", MissingMods.Count);

                var missingIds = MissingMods.Select(m => m.SteamId);
                var detailsDict = await _steamService.GetModDetailsBatchAsync(missingIds);

                foreach (var missingMod in MissingMods)
                {
                    if (detailsDict.TryGetValue(missingMod.SteamId, out var info))
                    {
                        missingMod.DisplayName = info.Title;
                        missingMod.Version = info.VersionTag;
                    }
                }

                // Rafraîchir l'ObservableCollection
                MissingMods = new ObservableCollection<Mod>(MissingMods);
            }

            Log.Information("Vérification terminée pour '{PlaysetName}'. {Count} mod(s) manquant(s) trouvé(s).", Header,
                MissingMods.Count);
        }
        catch (Exception ex)
        { 
            Log.Error(ex, "Erreur lors de la vérification des mods manquants pour le playset '{PlaysetName}'", Header);
        }
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand]
    private void OpenInSteam(Mod? mod)
    {
        if (mod == null || string.IsNullOrEmpty(mod.SteamId)) return;

        string url = $"steam://openurl/https://steamcommunity.com/sharedfiles/filedetails/?id={mod.SteamId}";
        
        try
        {
            // Ouverture de l'URL via la commande système selon l'OS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors de l'ouverture du lien Steam pour le mod {ModId}", mod.SteamId);
        }
    }
}