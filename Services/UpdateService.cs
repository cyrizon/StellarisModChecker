using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace StellarisModChecker.Services;

public class UpdateService
{
    private readonly UpdateManager _updateManager;

    public UpdateService()
    {
        var source = new GithubSource("https://github.com/cyrizon/StellarisModChecker", null, false);
        _updateManager = new UpdateManager(source);
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            return await _updateManager.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erreur lors de la vérification des mises à jour : {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DownloadAndInstallAsync(UpdateInfo updateInfo)
    {
        try
        {
            await _updateManager.DownloadUpdatesAsync(updateInfo);
            _updateManager.ApplyUpdatesAndRestart(updateInfo);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erreur lors du téléchargement et de l'installation des mises à jour : {ex.Message}");
            return false;
        }
    }

    public string CurrentVersion => _updateManager.CurrentVersion?.ToString() ?? "0.0.0";
}