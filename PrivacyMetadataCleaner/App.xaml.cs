using PrivacyMetadataCleaner.Pages;

namespace PrivacyMetadataCleaner;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }
}
