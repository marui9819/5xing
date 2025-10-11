using System;
using System.IO;
using iText.Kernel.Pdf;

namespace PrivacyMetadataCleaner.Services;

public static class PdfMetadataCleaner
{
    /// <summary>
    /// 清理 PDF 文档的作者、标题、关键词、创建时间等元数据。
    /// </summary>
    /// <param name="filePath">目标 PDF 文件路径。</param>
    public static void CleanPdfMetadata(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("PDF document not found.", filePath);
        }

        var tempFile = Path.GetTempFileName();

        try
        {
            using var reader = new PdfReader(filePath);
            reader.SetUnethicalReading(true);
            using var writer = new PdfWriter(tempFile);
            using var pdfDocument = new PdfDocument(reader, writer);

            ClearDocumentInfo(pdfDocument);
            RemoveXmpMetadata(pdfDocument);

            pdfDocument.Close();

            File.Copy(tempFile, filePath, true);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    private static void ClearDocumentInfo(PdfDocument pdfDocument)
    {
        var info = pdfDocument.GetDocumentInfo();
        var infoDictionary = info.GetPdfObject();
        infoDictionary?.Clear();

        info.SetAuthor(string.Empty);
        info.SetTitle(string.Empty);
        info.SetSubject(string.Empty);
        info.SetKeywords(string.Empty);
        info.SetCreator(string.Empty);
        info.SetProducer(string.Empty);
    }

    private static void RemoveXmpMetadata(PdfDocument pdfDocument)
    {
        var catalog = pdfDocument.GetCatalog();
        catalog.GetPdfObject()?.Remove(PdfName.Metadata);

        pdfDocument.SetXmpMetadata((byte[])null);
    }
}
