using System.Windows;
using PrivacyMetadataCleaner.ViewModels;

namespace PrivacyMetadataCleaner;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow
        {
            DataContext = new MainViewModel()
        };

        mainWindow.Show();
    }
}
