using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using PrivacyMetadataCleaner.Services;
using PrivacyMetadataCleaner.ViewModels;

namespace PrivacyMetadataCleaner.Pages;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = ResolveViewModel();
    }

    private MainViewModel ResolveViewModel()
    {
        var serviceProvider = App.Current?.Services;
        var service = serviceProvider?.GetService<MetadataCleaningService>();
        var viewModel = serviceProvider?.GetService<MainViewModel>();
        return viewModel ?? new MainViewModel(service);
    }

    private async void OnDrop(object? sender, DropEventArgs e)
    {
        if (BindingContext is not MainViewModel viewModel)
        {
            return;
        }

        if (!e.Data.Contains(DataFormats.Files))
        {
            return;
        }

        var dropped = await e.Data.GetFileSystemEntriesAsync();
        if (dropped is null || dropped.Count == 0)
        {
            return;
        }

        await viewModel.ProcessDroppedItemsAsync(dropped.Select(item => item.FullPath));
    }
}
