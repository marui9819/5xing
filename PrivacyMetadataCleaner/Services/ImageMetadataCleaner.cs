using System;
using System.IO;
using ImageMagick;

namespace PrivacyMetadataCleaner.Services;

public static class ImageMetadataCleaner
{
    /// <summary>
    /// 清除 JPG/PNG 图片的 EXIF 元数据（含 GPS、设备信息等）。
    /// </summary>
    /// <param name="filePath">目标图片文件路径。</param>
    public static void CleanImageMetadata(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Image file not found.", filePath);
        }

        using var image = new MagickImage(filePath);

        image.Strip();
        image.RemoveProfile("exif");
        image.RemoveProfile("iptc");
        image.RemoveProfile("xmp");

        image.Write(filePath);
    }
}
