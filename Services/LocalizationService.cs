using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Serilog;

namespace StellarisModChecker.Services;

public record LanguageItem(string Code, string DisplayName);

public class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public List<LanguageItem> AvailableLanguages { get; } = new()
    {
        new LanguageItem("en", "English 🇬🇧"),
        new LanguageItem("fr", "Français 🇫🇷")
    };
    
    public string this[string key]
    {
        get
        {
            try
            {
                return Resources.Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
            }
            catch
            {
                return key;
            }
        }
    }

    public void ChangeLanguage(string cultureCode)
    {
        try
        {
            var culture = new CultureInfo(cultureCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            Log.Information("Langue appliquée : {Culture}", cultureCode);
            
            OnPropertyChanged(string.Empty);
            OnPropertyChanged("Item[]");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors du changement de langue vers {Culture}", cultureCode);
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}