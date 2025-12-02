using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StellarisModChecker.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _welcomeMessage = "Welcome to Stellaris Mod Checker!";

    [RelayCommand]
    private void LoadMods()
    {
        // TODO: Implémenter la logique de chargement des mods
        WelcomeMessage = "Loading mods...";
    }
}
