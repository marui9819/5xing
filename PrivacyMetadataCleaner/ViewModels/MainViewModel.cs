using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using PrivacyMetadataCleaner.Models;
using PrivacyMetadataCleaner.Services;

namespace PrivacyMetadataCleaner.ViewModels;

public class MainViewModel : ViewModelBase
{
    private bool _isProcessing;
    private double _progressValue;
    private string _statusMessage = "准备就绪";

    public MainViewModel()
    {
        Files = new ObservableCollection<FileItem>();
        LogEntries = new ObservableCollection<string>();

        AddFilesCommand = new RelayCommand(_ => AddFiles(), _ => !IsProcessing);
        CleanMetadataCommand = new RelayCommand(async _ => await CleanMetadataAsync(), _ => !IsProcessing && Files.Count > 0);
        ExportLogCommand = new RelayCommand(_ => ExportLog(), _ => !IsProcessing && LogEntries.Count > 0);

        Files.CollectionChanged += OnFilesCollectionChanged;
        LogEntries.CollectionChanged += OnLogEntriesCollectionChanged;
    }

    public ObservableCollection<FileItem> Files { get; }

    public ObservableCollection<string> LogEntries { get; }

    public ICommand AddFilesCommand { get; }

    public ICommand CleanMetadataCommand { get; }

    public ICommand ExportLogCommand { get; }

    public bool IsProcessing
    {
        get => _isProcessing;
        private set
        {
            if (_isProcessing != value)
            {
                _isProcessing = value;
                OnPropertyChanged();
                (AddFilesCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (CleanMetadataCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (ExportLogCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set
        {
            if (Math.Abs(_progressValue - value) > 0.001)
            {
                _progressValue = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    private void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择需要清理的文件",
            Filter = "支持的文件|*.docx;*.pdf;*.jpg;*.jpeg;*.png|Word 文档 (*.docx)|*.docx|PDF 文件 (*.pdf)|*.pdf|图片文件 (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true)
        {
            StatusMessage = "未选择任何文件";
            return;
        }

        int addedCount = 0;
        foreach (var filePath in dialog.FileNames)
        {
            if (Files.Any(f => string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
            {
                AppendLog($"跳过重复文件：{Path.GetFileName(filePath)}");
                continue;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                Files.Add(new FileItem
                {
                    FileName = fileInfo.Name,
                    FileType = fileInfo.Extension.TrimStart('.').ToUpperInvariant(),
                    FileSize = FormatFileSize(fileInfo.Length),
                    FilePath = fileInfo.FullName,
                    Status = "待处理"
                });
                addedCount++;
            }
            catch (Exception ex)
            {
                AppendLog($"添加文件失败：{filePath}，原因：{ex.Message}");
            }
        }

        if (addedCount > 0)
        {
            AppendLog($"成功添加 {addedCount} 个文件。");
            StatusMessage = $"已添加 {addedCount} 个文件";
        }
        else
        {
            StatusMessage = "未添加新的文件";
        }
    }

    private async Task CleanMetadataAsync()
    {
        if (Files.Count == 0)
        {
            StatusMessage = "请先添加需要清理的文件";
            return;
        }

        IsProcessing = true;
        ProgressValue = 0;

        var total = Files.Count;
        StatusMessage = total == 1 ? "正在清理 1 个文件" : $"正在清理 {total} 个文件";
        AppendLog($"开始清理，共 {total} 个文件。");

        try
        {
            int index = 0;
            foreach (var file in Files)
            {
                file.Status = "处理中";
                AppendLog($"[{file.FileName}] 清理开始。");

                try
                {
                    await Task.Run(() => MetadataCleaner.CleanMetadata(file.FilePath));
                    file.Status = "已完成";
                    AppendLog($"[{file.FileName}] 清理完成。");
                }
                catch (Exception ex)
                {
                    file.Status = "失败";
                    AppendLog($"[{file.FileName}] 清理失败：{ex.Message}");
                }

                index++;
                ProgressValue = Math.Round(index * 100d / total, 2);
            }

            StatusMessage = "清理完成";
            AppendLog("全部文件清理完成。");
            ProgressValue = 100;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void ExportLog()
    {
        if (LogEntries.Count == 0)
        {
            StatusMessage = "没有可导出的日志";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "导出清理日志",
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            FileName = $"MetadataCleanerLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };

        if (dialog.ShowDialog() != true)
        {
            StatusMessage = "已取消日志导出";
            return;
        }

        try
        {
            File.WriteAllLines(dialog.FileName, LogEntries);
            AppendLog($"日志已导出：{dialog.FileName}");
            StatusMessage = "日志导出成功";
        }
        catch (Exception ex)
        {
            AppendLog($"日志导出失败：{ex.Message}");
            StatusMessage = "日志导出失败";
        }
    }

    private void AppendLog(string message)
    {
        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }

    private void OnFilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        (CleanMetadataCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnLogEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        (ExportLogCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }
}
