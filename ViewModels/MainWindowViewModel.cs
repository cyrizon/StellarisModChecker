using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StellarisModChecker.Services;
using System.Threading.Tasks;

namespace StellarisModChecker.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly UpdateService _updateService;

    [ObservableProperty]
    private string _welcomeMessage = "Welcome to Stellaris Mod Checker!";

    [ObservableProperty]
    private string _currentVersion;

    [ObservableProperty]
    private bool _updateAvailable;

    [ObservableProperty]
    private string _updateMessage = string.Empty;

    public MainWindowViewModel()
    {
        _updateService = new UpdateService();
        _currentVersion = _updateService.CurrentVersion;
        _ = CheckForUpdatesAsync();
    }

    [RelayCommand]
    private void LoadMods()
    {
        // TODO: Implémenter la logique de chargement des mods
        WelcomeMessage = "Loading mods...";
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
