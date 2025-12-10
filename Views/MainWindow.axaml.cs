using System;
using Avalonia.Controls;
using StellarisModChecker.ViewModels;

namespace StellarisModChecker.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        PlaysetDetectionService playsetDetectionService = new PlaysetDetectionService();
        playsetDetectionService.LoadPlaysets();
        playsetDetectionService.LoadPlaysetContents(playsetDetectionService.GetPlaysetsID()[0]);
    }
}
