using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StellarisModChecker.Services;
using System.Threading.Tasks;
using StellarisModChecker.Models;

namespace StellarisModChecker.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly UpdateService _updateService;
    private readonly PlaysetDetectionService _playsetDetectionService;

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

    public MainWindowViewModel()
    {
        _updateService = new UpdateService();
        _playsetDetectionService = new PlaysetDetectionService();
        
        _currentVersion = _updateService.CurrentVersion;
        _ = CheckForUpdatesAsync();

        LoadPlaysets();
    }

    private void LoadPlaysets()
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

        SelectedPlayset = Playsets.FirstOrDefault();
    }

    partial void OnSelectedPlaysetChanged(Playset? value)
    {
        if (value != null)
        {
            _playsetDetectionService.LoadPlaysetContents(value.Id);
            WelcomeMessage = $"Selected playset : {value.Name}";
        }
    }

    [RelayCommand]
    private void LoadMods()
    {
        // TODO: Implémenter la logique de chargement des mods
        if (SelectedPlayset != null)
        {
            WelcomeMessage = $"Loading mods for : {SelectedPlayset.Name}...";
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
        }
        else
        {
            UpdateMessage = "Vous avez la dernière version.";
        }
    }

    [RelayCommand]
    private async Task InstallUpdateAsync()
    {
        UpdateMessage = "Téléchargement de la mise à jour...";
        var updateInfo = await _updateService.CheckForUpdatesAsync();
        
        if (updateInfo != null)
        {
            await _updateService.DownloadAndInstallAsync(updateInfo);
        }
    }
}
