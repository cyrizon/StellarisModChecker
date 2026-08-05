using CommunityToolkit.Mvvm.ComponentModel;
using StellarisModChecker.Models;

namespace StellarisModChecker.ViewModels;

public partial class PlaysetTabViewModel : ViewModelBase
{
    public string Id { get; }
    
    [ObservableProperty]
    private string _header;

    public PlaysetTabViewModel(Playset playset)
    {
        Id = playset.Id;
        _header = playset.Name;
    }
}