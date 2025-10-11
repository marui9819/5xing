using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.CustomProperties;

namespace PrivacyMetadataCleaner.Services;

public static class WordMetadataCleaner
{
    /// <summary>
    /// 清理 Word 文档的敏感元数据（作者、公司、时间戳等）。
    /// </summary>
    /// <param name="filePath">目标 .docx 文件路径。</param>
    /// <exception cref="ArgumentException">当路径为空或仅包含空白字符时抛出。</exception>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出。</exception>
    public static void CleanWordMetadata(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Word document not found.", filePath);
        }

        using var wordDocument = WordprocessingDocument.Open(filePath, true);

        ClearCoreProperties(wordDocument);
        ClearExtendedProperties(wordDocument);
        RemoveCustomProperties(wordDocument);
    }

    private static void ClearCoreProperties(WordprocessingDocument document)
    {
        var properties = document.PackageProperties;

        properties.Creator = null;
        properties.LastModifiedBy = null;
        properties.Revision = null;
        properties.Description = null;
        properties.Subject = null;
        properties.Title = null;
        properties.Category = null;
        properties.ContentStatus = null;
        properties.Keywords = null;
        properties.Language = null;
        properties.Identifier = null;
        properties.Version = null;
        properties.ContentType = null;
        properties.Created = null;
        properties.Modified = null;
        properties.LastPrinted = null;
    }

    private static void ClearExtendedProperties(WordprocessingDocument document)
    {
        var extendedProps = document.ExtendedFilePropertiesPart?.Properties;
        if (extendedProps is null)
        {
            return;
        }

        if (extendedProps.Company is not null)
        {
            extendedProps.Company.Text = string.Empty;
        }

        if (extendedProps.Manager is not null)
        {
            extendedProps.Manager.Text = string.Empty;
        }

        if (extendedProps.Lines is not null)
        {
            extendedProps.Lines.Text = string.Empty;
        }

        if (extendedProps.Notes is not null)
        {
            extendedProps.Notes.Text = string.Empty;
        }

        if (extendedProps.Template is not null)
        {
            extendedProps.Template.Text = string.Empty;
        }

        if (extendedProps.Application is not null)
        {
            extendedProps.Application.Text = string.Empty;
        }
    }

    private static void RemoveCustomProperties(WordprocessingDocument document)
    {
        var customPropertiesPart = document.CustomFilePropertiesPart;
        if (customPropertiesPart is null)
        {
            return;
        }

        var properties = customPropertiesPart.Properties;
        if (properties is not null)
        {
            var allCustomProperties = properties.Elements<CustomDocumentProperty>().ToList();
            foreach (var property in allCustomProperties)
            {
                property.Remove();
            }
        }

        document.DeletePart(customPropertiesPart);
    }
}
