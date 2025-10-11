using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrivacyMetadataCleaner.Models;

public class FileItem : INotifyPropertyChanged
{
    private string _status = "待处理";

    public string FileName { get; init; } = string.Empty;

    public string FileType { get; init; } = string.Empty;

    public string FileSize { get; init; } = string.Empty;

    public string FilePath { get; init; } = string.Empty;

    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
