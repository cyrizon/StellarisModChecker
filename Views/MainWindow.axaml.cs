using Avalonia.Controls;
using StellarisModChecker.ViewModels;

namespace StellarisModChecker.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
