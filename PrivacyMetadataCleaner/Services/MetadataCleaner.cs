using System;
using System.IO;

namespace PrivacyMetadataCleaner.Services;

public static class MetadataCleaner
{
    /// <summary>
    /// 根据文件扩展名调用对应的清理方法，支持 .docx、.pdf、.jpg、.jpeg、.png。
    /// </summary>
    /// <param name="filePath">需要清理的文件路径。</param>
    /// <exception cref="ArgumentException">当路径为空或仅包含空白字符时抛出。</exception>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出。</exception>
    /// <exception cref="NotSupportedException">当文件类型不受支持时抛出。</exception>
    public static void CleanMetadata(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        switch (extension)
        {
            case ".docx":
                WordMetadataCleaner.CleanWordMetadata(filePath);
                break;
            case ".pdf":
                PdfMetadataCleaner.CleanPdfMetadata(filePath);
                break;
            case ".jpg":
            case ".jpeg":
            case ".png":
                ImageMetadataCleaner.CleanImageMetadata(filePath);
                break;
            default:
                throw new NotSupportedException($"File type '{extension}' is not supported for metadata cleaning.");
        }
    }
}
