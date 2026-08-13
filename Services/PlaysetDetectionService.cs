using System;
using System.Collections.Generic;
using System.IO;
using Serilog;
using StellarisModChecker.Data;
using StellarisModChecker.Models;

namespace StellarisModChecker.Services;

public class PlaysetDetectionService
{
    private readonly IPlaysetRepository _playsetRepository;

    public string DetectedOS { get; set; } = String.Empty;
    
    private const string PlaysetFileName = "launcher-v2.sqlite";

    private string _playsetPath;

    public PlaysetDetectionService()
    {
        DetectOS();
        SearchPlayset();
        if (string.IsNullOrEmpty(_playsetPath))
        {
            var ex = new FileNotFoundException($"Le fichier {PlaysetFileName} n'a pas été trouvé pour l'OS {DetectedOS}.");
            Log.Error(ex, "Échec de l'initialisation de PlaysetDetectionService.");
            throw ex;
        }
        _playsetRepository = new PlaysetRepository(_playsetPath);
    }

    
    private void DetectOS()
    {
        if (OperatingSystem.IsWindows())
        {
            DetectedOS = "Windows";
        }
        else if (OperatingSystem.IsLinux())
        {
            DetectedOS = "Linux";
        }
        else if (OperatingSystem.IsMacOS())
        {
            DetectedOS = "macOS";
        }
        else
        {
            var ex = new NotSupportedException("L'système d'exploitation détecté n'est pas supporté.");
            Log.Fatal(ex, "OS non supporté.");
            throw ex;
        }
        Log.Information("OS détecté : {DetectedOS}", DetectedOS);
    }

    private void SearchPlayset()
    {
        DirectoryInfo playsetDirectory;
        switch (DetectedOS)
        {
            case "Windows":
                playsetDirectory = new DirectoryInfo(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Paradox Interactive", "Stellaris"));
                break;
            case "Linux":
                playsetDirectory = new DirectoryInfo(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local", "share", "Paradox Interactive", "Stellaris"));
                break;
            case "macOS":
                playsetDirectory = new DirectoryInfo(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    "Library", "Application Support", "Paradox Interactive", "Stellaris"));
                break;
            default:
                var ex = new NotSupportedException("OS non supporté pour la recherche de playset.");
                Log.Error(ex, "Recherche impossible pour OS: {OS}", DetectedOS);
                throw ex;
        }
        
        Log.Information("Recherche du fichier {FileName} dans {DirectoryPath}", PlaysetFileName, playsetDirectory.FullName);

        if (playsetDirectory.Exists)
        {
            FileInfo[] playsetFiles = playsetDirectory.GetFiles(PlaysetFileName, SearchOption.TopDirectoryOnly);
            foreach (FileInfo playsetFile in playsetFiles)
            {
                _playsetPath = playsetFile.FullName;
                Log.Information("Fichier de playset trouvé : {Path}", _playsetPath);
            }
        }
        else
        {
            Log.Warning("Le dossier du playset n'existe pas à l'emplacement : {DirectoryPath}", playsetDirectory.FullName);
        }
    }

    public void LoadPlaysets()
    {
        _playsetRepository.LoadPlaysets();
    }

    public List<string> GetPlaysetsID()
    {
        return new List<string>(_playsetRepository.GetPlaysetsID());
    }
    
    public Dictionary<string, string> GetPlaysets()
    {
        return _playsetRepository.GetPlaysets();
    }

    public List<Mod> GetModsForPlayset(string playsetId)
    {
        return _playsetRepository.GetModsForPlayset(playsetId);
    }
}