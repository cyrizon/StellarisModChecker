using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StellarisModChecker.Models;
using StellarisModChecker.Services.Detection;

namespace StellarisModChecker.ViewModels;

public partial class PlaysetTabViewModel : ViewModelBase
{
    private readonly PlaysetDetectionService _service;
    
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
        if (IsChecking) return; // Anti-spam manuel

        IsChecking = true;

        // TODO: Implémenter votre vraie logique de vérification
        // Exemple factice pour tester le comportement :
        await Task.Delay(500); // Simule un traitement rapide sans bloquer l'UI

        MissingMods.Clear();
        foreach (var mod in Mods)
        {
            // Exemple de logique : si le mod n'a pas de version ou n'est pas activé
            if (!mod.IsEnabled || string.IsNullOrEmpty(mod.Version))
            {
                MissingMods.Add(mod);
            }
        }

        IsChecking = false;
    }
}