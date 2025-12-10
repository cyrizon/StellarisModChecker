using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

class PlaysetDetectionService
{
    private readonly IPlaysetRepository _playsetRepository;

    public string DetectedOS { get; private set; }

    private const string WindowsModsPath = @"\Paradox Interactive\Stellaris";
    private const string LinuxModsPath = @"/.local/share/Paradox Interactive/Stellaris";
    private const string MacOSModsPath = @"/Library/Application Support/Paradox Interactive/Stellaris";

    private const string PlaysetFileName = "launcher-v2.sqlite";

    private string _playsetPath;

    public PlaysetDetectionService()
    {
        DetectOS();
        SearchPlayset();
        if (string.IsNullOrEmpty(_playsetPath))
        {
            throw new FileNotFoundException("Playset file not found.");
        }
        _playsetRepository = new PlaysetRepository(_playsetPath);
    }

    
    private void DetectOS()
    {
        // Logic to detect the operating system
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
            throw new NotSupportedException("The detected operating system is not supported.");
        }
        Console.WriteLine($"Detected OS: {DetectedOS}");
    }

    private void SearchPlayset()
    {
        DirectoryInfo playsetDirectory;
        switch (DetectedOS)
        {
            case "Windows":
                playsetDirectory = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Paradox Interactive\Stellaris");
                break;
            case "Linux":
                playsetDirectory = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"/.local/share/Paradox Interactive/Stellaris");
                break;
            case "macOS":
                playsetDirectory = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"/Library/Application Support/Paradox Interactive/Stellaris");
                break;
            default:
                throw new NotSupportedException("The detected operating system is not supported for playset search.");
        }

        if (playsetDirectory.Exists)
        {
            FileInfo[] playsetFiles = playsetDirectory.GetFiles(PlaysetFileName, SearchOption.TopDirectoryOnly);
            foreach (FileInfo playsetFile in playsetFiles)
            {
                _playsetPath = playsetFile.FullName;
                Console.WriteLine($"Found playset file: {playsetFile.FullName}");
            }
        }
        else
        {
            Console.WriteLine("Playset directory does not exist.");
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

    public void LoadPlaysetContents(string playsetId)
    {
        if (_playsetRepository is PlaysetRepository repository)
        {
            repository.LoadPlaysetContents(playsetId);
        }
        else
        {
            throw new InvalidOperationException("Playset repository does not support loading playset contents.");
        }
    }
}