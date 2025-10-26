using GroupDocs.Metadata;
using GroupDocs.Metadata.Options;
using System.IO;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace PrivacyMetadataCleaner.Services;

public class MetadataCleaningService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".docx", ".pdf", ".jpg", ".jpeg", ".png"
    };

    public IEnumerable<string> GetSupportedFiles(IEnumerable<string> droppedPaths)
    {
        foreach (var path in droppedPaths)
        {
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    if (IsSupported(file))
                    {
                        yield return file;
                    }
                }
            }
            else if (File.Exists(path) && IsSupported(path))
            {
                yield return path;
            }
        }
    }

    public async Task<FileProcessResult> ProcessFileAsync(string filePath, int imageQuality, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(filePath);
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var cleanedDirectory = Path.Combine(directory, "Cleaned");
        Directory.CreateDirectory(cleanedDirectory);
        var cleanedPath = Path.Combine(cleanedDirectory, Path.GetFileName(filePath));
        var tempPath = Path.Combine(cleanedDirectory, $"tmp_{Guid.NewGuid():N}{extension}");

        long originalSize = new FileInfo(filePath).Length;

        try
        {
            if (IsDocument(extension))
            {
                await Task.Run(() => SanitizeWithGroupDocs(filePath, cleanedPath), cancellationToken);
            }
            else if (IsImage(extension))
            {
                await Task.Run(() => SanitizeWithGroupDocs(filePath, tempPath), cancellationToken);
                await ProcessImageAsync(tempPath, cleanedPath, imageQuality, cancellationToken);
            }
            else
            {
                throw new NotSupportedException($"暂不支持的文件类型: {extension}");
            }

            long cleanedSize = new FileInfo(cleanedPath).Length;
            return FileProcessResult.Success(filePath, cleanedPath, originalSize, cleanedSize);
        }
        catch (Exception ex)
        {
            return FileProcessResult.Failure(filePath, ex.Message, originalSize);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static void SanitizeWithGroupDocs(string sourcePath, string destinationPath)
    {
        using var metadata = new Metadata(sourcePath);
        metadata.Sanitize();
        metadata.Save(destinationPath, new MetadataSaveOptions());
    }

    private static async Task ProcessImageAsync(string sourcePath, string destinationPath, int quality, CancellationToken cancellationToken)
    {
        await using var sourceStream = File.OpenRead(sourcePath);
        using var image = await Image.LoadAsync(sourceStream, cancellationToken);

        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;

        await using var destinationStream = File.Create(destinationPath);
        if (Path.GetExtension(destinationPath).Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            var encoder = new PngEncoder
            {
                CompressionLevel = quality switch
                {
                    <= 40 => PngCompressionLevel.BestCompression,
                    <= 60 => PngCompressionLevel.Level6,
                    <= 80 => PngCompressionLevel.Level3,
                    _ => PngCompressionLevel.Default
                },
                ColorType = PngColorType.Rgb
            };
            await image.SaveAsync(destinationStream, encoder, cancellationToken);
        }
        else
        {
            var encoder = new JpegEncoder
            {
                Quality = Math.Clamp(quality, 10, 100)
            };
            await image.SaveAsync(destinationStream, encoder, cancellationToken);
        }
    }

    private static bool IsSupported(string path) => SupportedExtensions.Contains(Path.GetExtension(path));

    private static bool IsDocument(string? extension) => extension is not null && (extension.Equals(".docx", StringComparison.OrdinalIgnoreCase) || extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase));

    private static bool IsImage(string? extension) => extension is not null && (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".png", StringComparison.OrdinalIgnoreCase));
}

public record FileProcessResult(string FilePath, string? CleanedPath, bool Success, string Message, long OriginalSize, long? CleanedSize)
{
    public static FileProcessResult Success(string filePath, string cleanedPath, long originalSize, long cleanedSize) =>
        new(filePath, cleanedPath, true, "清理完成", originalSize, cleanedSize);

    public static FileProcessResult Failure(string filePath, string message, long originalSize) =>
        new(filePath, null, false, message, originalSize, null);
}
