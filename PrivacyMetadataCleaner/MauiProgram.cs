using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrivacyMetadataCleaner.Services;
using PrivacyMetadataCleaner.ViewModels;

namespace PrivacyMetadataCleaner;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<MetadataCleaningService>();
        builder.Services.AddTransient<MainViewModel>();

        return builder.Build();
    }
}
