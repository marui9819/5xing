using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.IO;
using Microsoft.Maui.ApplicationModel;
using System.Threading;
using PrivacyMetadataCleaner.Services;

namespace PrivacyMetadataCleaner.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly MetadataCleaningService _service;
    private QualityOption _selectedQuality;
    private double _progress;
    private string _progressMessage = "等待拖放文件...";
    private bool _isProcessing;

    public ObservableCollection<ProcessingResultItem> Results { get; } = new();

    public IReadOnlyList<QualityOption> QualityOptions { get; } = new List<QualityOption>
    {
        new("30%", 30),
        new("50%", 50),
        new("70%", 70),
        new("90%", 90)
    };

    public QualityOption SelectedQuality
    {
        get => _selectedQuality;
        set
        {
            if (_selectedQuality != value)
            {
                _selectedQuality = value;
                OnPropertyChanged();
            }
        }
    }

    public double Progress
    {
        get => _progress;
        private set
        {
            if (Math.Abs(_progress - value) > double.Epsilon)
            {
                _progress = value;
                OnPropertyChanged();
            }
        }
    }

    public string ProgressMessage
    {
        get => _progressMessage;
        private set
        {
            if (_progressMessage != value)
            {
                _progressMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(MetadataCleaningService? service = null)
    {
        _service = service ?? new MetadataCleaningService();
        _selectedQuality = QualityOptions.First();
    }

    public async Task ProcessDroppedItemsAsync(IEnumerable<string> droppedPaths)
    {
        if (_isProcessing)
        {
            ProgressMessage = "正在处理中，请稍候...";
            return;
        }

        _isProcessing = true;
        try
        {
            var files = _service.GetSupportedFiles(droppedPaths).Distinct().ToList();
            if (files.Count == 0)
            {
                ProgressMessage = "未找到支持的文件类型。";
                return;
            }

            Results.Clear();
            double processed = 0;
            Progress = 0;
            ProgressMessage = $"准备处理 {files.Count} 个文件...";

            foreach (var file in files)
            {
                var result = await _service.ProcessFileAsync(file, SelectedQuality.Quality, CancellationToken.None);
                processed++;
                Progress = processed / files.Count;
                ProgressMessage = $"已处理 {processed} / {files.Count} 个文件";

                var item = new ProcessingResultItem(result);
                MainThread.BeginInvokeOnMainThread(() => Results.Add(item));
            }

            ProgressMessage = "全部文件处理完成。";
        }
        finally
        {
            _isProcessing = false;
            if (Progress < 1)
            {
                Progress = 1;
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public record QualityOption(string Label, int Quality)
{
    public override string ToString() => Label;
}

public class ProcessingResultItem
{
    public ProcessingResultItem(FileProcessResult result)
    {
        FileName = Path.GetFileName(result.FilePath);
        StatusText = result.Success ? result.Message : $"失败：{result.Message}";
        SizeDifference = BuildSizeText(result);
    }

    public string FileName { get; }

    public string StatusText { get; }

    public string SizeDifference { get; }

    private static string BuildSizeText(FileProcessResult result)
    {
        if (!result.Success || result.CleanedSize is null)
        {
            return $"原始大小：{FormatBytes(result.OriginalSize)}";
        }

        var delta = result.OriginalSize - result.CleanedSize.Value;
        var deltaText = delta == 0 ? "无变化" : $"减少 {FormatBytes(delta)}";
        return $"原始大小：{FormatBytes(result.OriginalSize)}，清理后：{FormatBytes(result.CleanedSize.Value)}（{deltaText}）";
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}