using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using StellarisModChecker.Models;
using StellarisModChecker.Services;

namespace StellarisModChecker.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly UpdateService _updateService;
    private PlaysetDetectionService? _playsetDetectionService;

    [ObservableProperty]
    private string _welcomeMessage = "Welcome to Stellaris Mod Checker!";

    [ObservableProperty]
    private string _currentVersion;

    [ObservableProperty]
    private bool _updateAvailable;

    [ObservableProperty]
    private string _updateMessage = string.Empty;

    [ObservableProperty] 
    private ObservableCollection<Playset> _playsets = new();
    
    [ObservableProperty]
    private Playset? _selectedPlayset;

    [ObservableProperty]
    private ObservableCollection<PlaysetTabViewModel> _tabs = new();
    
    [ObservableProperty]
    private PlaysetTabViewModel? _selectedTab;

    public MainWindowViewModel()
    {
        Log.Information("Initialisation du MainWindowViewModel (Version app: {Version})", _updateService?.CurrentVersion ?? "Inconnue");
        
        _updateService = new UpdateService();
        _currentVersion = _updateService.CurrentVersion;
        
        _ = CheckForUpdatesAsync();

        _ = InitializeAsync();
    }
    
    private async Task InitializeAsync()
    {
        try
        {
            Log.Information("Initialisation asynchrone des services...");

            var dbUpdater = new DatabaseUpdaterService();
            await dbUpdater.CheckAndDownloadDatabaseAsync();

            _playsetDetectionService = new PlaysetDetectionService();

            LoadPlaysets();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Échec critique lors de l'initialisation de l'application dans MainWindowViewModel");
            WelcomeMessage = "Erreur lors de l'initialisation : vérifiez les fichiers de log.";
        }
    }

    private void LoadPlaysets()
    {
        if (_playsetDetectionService == null)
        {
            Log.Warning("Tentative de chargement des playsets alors que PlaysetDetectionService est null.");
            return;
        }
        
        try
        {
            _playsetDetectionService.LoadPlaysets();
            Dictionary<string, string> playsetData = _playsetDetectionService.GetPlaysets();
            
            Playsets.Clear();

            foreach (var kvp in playsetData)
            {
                Playsets.Add(new Playset
                {
                    Id = kvp.Key, 
                    Name = kvp.Value
                });
            }

            Log.Information("{Count} playset(s) chargé(s) dans l'interface", Playsets.Count);
            SelectedPlayset = Playsets.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors du chargement des playsets dans le ViewModel");
        }
    }

    partial void OnSelectedPlaysetChanged(Playset? value)
    {
        if (value != null)
        {
            Log.Debug("Playset sélectionné dans l'UI : '{PlaysetName}' (ID: {PlaysetId})", value.Name, value.Id);
            WelcomeMessage = $"Selected playset : {value.Name}";
        }
    }

    [RelayCommand]
    private void LoadMods()
    {
        if (SelectedPlayset == null)
        {
            Log.Warning("Clic sur 'Load Mods' mais aucun playset n'est sélectionné.");
            return;
        }

        if (_playsetDetectionService == null)
        {
            Log.Error("Impossible de charger les mods : PlaysetDetectionService n'est pas initialisé.");
            return;
        }

        var existingTab = Tabs.FirstOrDefault(t => t.Id == SelectedPlayset.Id);

        if (existingTab != null)
        {
            Log.Information("Bascule vers l'onglet existant pour le playset '{PlaysetName}'", SelectedPlayset.Name);
            SelectedTab = existingTab;
        }
        else
        {
            Log.Information("Création d'un nouvel onglet pour le playset '{PlaysetName}' (ID: {PlaysetId})", SelectedPlayset.Name, SelectedPlayset.Id);
            var newTab = new PlaysetTabViewModel(SelectedPlayset, _playsetDetectionService);
            Tabs.Add(newTab);
            SelectedTab = newTab;
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        UpdateMessage = "Vérification des mises à jour...";
        var updateInfo = await _updateService.CheckForUpdatesAsync();

        if (updateInfo != null)
        {
            UpdateAvailable = true;
            UpdateMessage = $"Mise à jour {updateInfo.TargetFullRelease.Version} disponible !";
            Log.Information("Notification de mise à jour affichée pour la version {Version}", updateInfo.TargetFullRelease.Version);
        }
        else
        {
            UpdateMessage = "Vous avez la dernière version.";
        }
    }

    [RelayCommand]
    private async Task InstallUpdateAsync()
    {
        Log.Information("Lancement du processus d'installation de la mise à jour...");
        UpdateMessage = "Téléchargement de la mise à jour...";
        
        var updateInfo = await _updateService.CheckForUpdatesAsync();
        if (updateInfo != null)
        {
            bool success = await _updateService.DownloadAndInstallAsync(updateInfo);
            if (!success)
            {
                UpdateMessage = "Erreur lors du téléchargement de la mise à jour.";
            }
        }
    }
}
