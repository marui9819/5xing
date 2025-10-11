using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using ImageMagick;
using PdfLayoutDocument = iText.Layout.Document;
using PdfParagraph = iText.Layout.Element.Paragraph;
using PdfImageElement = iText.Layout.Element.Image;
using WordParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;

namespace PrivacyMetadataCleaner
{
    public partial class MainForm : Form
    {
        private readonly Dictionary<string, ListViewItem> _listViewItems = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _logSyncRoot = new();

        private static readonly string[] SupportedExtensions =
        {
            ".docx", ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif", ".bmp"
        };

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            btnAddFiles = new Button();
            btnAddFolder = new Button();
            btnStart = new Button();
            btnCompress = new Button();
            lvFiles = new ListView();
            columnHeaderFile = new ColumnHeader();
            columnHeaderStatus = new ColumnHeader();
            columnHeaderMetadata = new ColumnHeader();
            progressBar = new ProgressBar();
            txtLog = new TextBox();
            SuspendLayout();
            // 
            // btnAddFiles
            // 
            btnAddFiles.Location = new System.Drawing.Point(12, 12);
            btnAddFiles.Name = "btnAddFiles";
            btnAddFiles.Size = new System.Drawing.Size(120, 34);
            btnAddFiles.TabIndex = 0;
            btnAddFiles.Text = "选择文件";
            btnAddFiles.UseVisualStyleBackColor = true;
            btnAddFiles.Click += btnAddFiles_Click;
            // 
            // btnAddFolder
            // 
            btnAddFolder.Location = new System.Drawing.Point(138, 12);
            btnAddFolder.Name = "btnAddFolder";
            btnAddFolder.Size = new System.Drawing.Size(120, 34);
            btnAddFolder.TabIndex = 1;
            btnAddFolder.Text = "选择文件夹";
            btnAddFolder.UseVisualStyleBackColor = true;
            btnAddFolder.Click += btnAddFolder_Click;
            // 
            // btnStart
            // 
            btnStart.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnStart.Location = new System.Drawing.Point(668, 12);
            btnStart.Name = "btnStart";
            btnStart.Size = new System.Drawing.Size(120, 34);
            btnStart.TabIndex = 2;
            btnStart.Text = "开始处理";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            //
            // btnCompress
            //
            btnCompress.Location = new System.Drawing.Point(264, 12);
            btnCompress.Name = "btnCompress";
            btnCompress.Size = new System.Drawing.Size(120, 34);
            btnCompress.TabIndex = 3;
            btnCompress.Text = "文档压缩";
            btnCompress.UseVisualStyleBackColor = true;
            btnCompress.Click += btnCompress_Click;
            //
            // lvFiles
            //
            lvFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lvFiles.Columns.AddRange(new[] { columnHeaderFile, columnHeaderStatus, columnHeaderMetadata });
            lvFiles.CheckBoxes = true;
            lvFiles.FullRowSelect = true;
            lvFiles.GridLines = true;
            lvFiles.Location = new System.Drawing.Point(12, 56);
            lvFiles.Name = "lvFiles";
            lvFiles.Size = new System.Drawing.Size(776, 280);
            lvFiles.TabIndex = 4;
            lvFiles.UseCompatibleStateImageBehavior = false;
            lvFiles.View = View.Details;
            lvFiles.ItemCheck += lvFiles_ItemCheck;
            //
            // columnHeaderFile
            //
            columnHeaderFile.Text = "文件";
            columnHeaderFile.Width = 360;
            // 
            // columnHeaderStatus
            // 
            columnHeaderStatus.Text = "状态";
            columnHeaderStatus.Width = 120;
            // 
            // columnHeaderMetadata
            // 
            columnHeaderMetadata.Text = "清理的元数据";
            columnHeaderMetadata.Width = 260;
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new System.Drawing.Point(12, 344);
            progressBar.Name = "progressBar";
            progressBar.Size = new System.Drawing.Size(776, 23);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.TabIndex = 5;
            // 
            // txtLog
            // 
            txtLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.Location = new System.Drawing.Point(12, 373);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new System.Drawing.Size(776, 165);
            txtLog.TabIndex = 6;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 550);
            Controls.Add(txtLog);
            Controls.Add(progressBar);
            Controls.Add(lvFiles);
            Controls.Add(btnCompress);
            Controls.Add(btnStart);
            Controls.Add(btnAddFolder);
            Controls.Add(btnAddFiles);
            MinimumSize = new System.Drawing.Size(820, 600);
            Name = "MainForm";
            Text = "Privacy Metadata Cleaner";
            ResumeLayout(false);
            PerformLayout();
        }

        private void btnAddFiles_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = "选择要清理的文件",
                Filter = "支持的文件|*.docx;*.pdf;*.jpg;*.jpeg;*.png;*.tiff;*.tif;*.bmp|所有文件|*.*"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                AddFiles(dialog.FileNames);
            }
        }

        private void btnAddFolder_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择包含要清理文件的文件夹"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                var files = Directory.EnumerateFiles(dialog.SelectedPath, "*", SearchOption.AllDirectories)
                    .Where(IsSupportedExtension);
                AddFiles(files);
            }
        }

        private void lvFiles_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lvFiles.Items.Count)
            {
                return;
            }

            var item = lvFiles.Items[e.Index];
            if (e.NewValue == CheckState.Unchecked && item.SubItems.Count > 1)
            {
                item.SubItems[1].Text = "未选中";
            }
            else if (e.NewValue == CheckState.Checked && item.SubItems.Count > 1)
            {
                if (string.Equals(item.SubItems[1].Text, "未选中", StringComparison.OrdinalIgnoreCase))
                {
                    item.SubItems[1].Text = "等待";
                }
            }
        }

        private async void btnStart_Click(object? sender, EventArgs e)
        {
            await StartProcessingAsync();
        }

        private async void btnCompress_Click(object? sender, EventArgs e)
        {
            var docxFiles = lvFiles.Items
                .Cast<ListViewItem>()
                .Where(item => item.Checked)
                .Select(item => item.Tag as string)
                .Where(path => !string.IsNullOrEmpty(path) && string.Equals(Path.GetExtension(path), ".docx", StringComparison.OrdinalIgnoreCase))
                .Select(path => path!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (docxFiles.Count == 0)
            {
                MessageBox.Show(this, "请先勾选需要压缩的 Word 文档。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var optionsDialog = new CompressionOptionsForm();
            if (optionsDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            ToggleUi(false);
            progressBar.Value = 0;
            progressBar.Maximum = docxFiles.Count;
            AppendLog($"开始压缩 {docxFiles.Count} 个文档...");

            int processed = 0;
            int success = 0;
            int failed = 0;

            foreach (var filePath in docxFiles)
            {
                AppendLog($"压缩: {filePath}");

                if (_listViewItems.TryGetValue(filePath, out var item))
                {
                    item.SubItems[1].Text = "压缩中";
                }

                var result = await Task.Run(() => CompressDocxDocument(filePath, optionsDialog.SelectedQuality, optionsDialog.SelectedFormat));
                processed++;

                if (result.Success)
                {
                    success++;
                    AppendLog($"压缩成功: {result.OutputPath}");

                    if (_listViewItems.TryGetValue(filePath, out var successItem))
                    {
                        successItem.SubItems[1].Text = "压缩成功";
                        successItem.SubItems[2].Text = string.Join(", ", result.Details);
                    }
                }
                else
                {
                    failed++;
                    AppendLog($"压缩失败: {result.ErrorMessage}");

                    if (_listViewItems.TryGetValue(filePath, out var failedItem))
                    {
                        failedItem.SubItems[1].Text = "压缩失败";
                        failedItem.SubItems[2].Text = result.ErrorMessage;
                    }
                }

                progressBar.Value = Math.Min(progressBar.Maximum, processed);
            }

            ToggleUi(true);

            AppendLog($"压缩完成。成功: {success}，失败: {failed}");
            MessageBox.Show(this,
                $"压缩完成！\n总数: {processed}\n成功: {success}\n失败: {failed}",
                "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task StartProcessingAsync()
        {
            var filesToProcess = lvFiles.Items
                .Cast<ListViewItem>()
                .Where(item => item.Checked)
                .Select(item => (string)item.Tag)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (filesToProcess.Count == 0)
            {
                MessageBox.Show(this, "请先选择要处理的文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ToggleUi(false);
            progressBar.Value = 0;
            progressBar.Maximum = filesToProcess.Count;
            AppendLog($"开始处理 {filesToProcess.Count} 个文件...");

            int processed = 0;
            int success = 0;
            int failed = 0;
            int skipped = 0;

            foreach (var filePath in filesToProcess)
            {
                var currentIndex = processed + 1;
                AppendLog($"[{currentIndex}/{filesToProcess.Count}] 处理: {filePath}");

                if (_listViewItems.TryGetValue(filePath, out var pendingItem))
                {
                    pendingItem.SubItems[1].Text = "处理中";
                }

                var result = await Task.Run(() => ProcessFile(filePath));
                processed++;

                switch (result.Status)
                {
                    case FileProcessStatus.Success:
                        success++;
                        break;
                    case FileProcessStatus.Failed:
                        failed++;
                        break;
                    case FileProcessStatus.Skipped:
                        skipped++;
                        break;
                }

                UpdateListView(result);
                AppendResultLog(result);

                progressBar.Value = Math.Min(progressBar.Maximum, processed);
            }

            AppendLog($"处理完成。成功: {success}，失败: {failed}，跳过: {skipped}");
            ToggleUi(true);
            MessageBox.Show(this,
                $"处理完成！\n总数: {processed}\n成功: {success}\n失败: {failed}\n跳过: {skipped}",
                "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ToggleUi(bool enabled)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(ToggleUi), enabled);
                return;
            }

            btnAddFiles.Enabled = enabled;
            btnAddFolder.Enabled = enabled;
            btnStart.Enabled = enabled;
            btnCompress.Enabled = enabled;
        }

        private void AddFiles(IEnumerable<string> files)
        {
            var added = 0;
            foreach (var file in files)
            {
                if (!File.Exists(file))
                {
                    continue;
                }

                if (!IsSupportedExtension(file))
                {
                    AppendLog($"跳过不支持的文件类型: {file}");
                    continue;
                }

                if (_listViewItems.ContainsKey(file))
                {
                    var existingItem = _listViewItems[file];
                    existingItem.Checked = true;
                    existingItem.SubItems[1].Text = "等待";
                    existingItem.SubItems[2].Text = string.Empty;
                    added++;
                    continue;
                }

                var item = new ListViewItem(file)
                {
                    Tag = file,
                    Checked = true
                };
                item.SubItems.Add("等待");
                item.SubItems.Add(string.Empty);
                lvFiles.Items.Add(item);
                _listViewItems[file] = item;
                added++;
            }

            if (added > 0)
            {
                AppendLog($"已添加 {added} 个文件。当前列表总数: {lvFiles.Items.Count}");
            }
        }

        private bool IsSupportedExtension(string path)
        {
            var extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        private FileProcessResult ProcessFile(string filePath)
        {
            try
            {
                var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
                return extension switch
                {
                    ".docx" => CleanDocx(filePath),
                    ".pdf" => CleanPdf(filePath),
                    ".jpg" or ".jpeg" or ".png" or ".tiff" or ".tif" or ".bmp" => CleanImage(filePath),
                    _ => new FileProcessResult(filePath, FileProcessStatus.Skipped, new List<string>(), "不支持的文件类型")
                };
            }
            catch (Exception ex)
            {
                return new FileProcessResult(filePath, FileProcessStatus.Failed, new List<string>(), ex.Message);
            }
        }

        private CompressionResult CompressDocxDocument(string sourcePath, CompressionQuality quality, CompressionOutputFormat format)
        {
            var tempDocxPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.docx");

            try
            {
                File.Copy(sourcePath, tempDocxPath, true);

                var metrics = CompressDocxImages(tempDocxPath, quality);
                var details = BuildCompressionDetails(metrics);

                if (format == CompressionOutputFormat.Docx)
                {
                    var targetPath = BuildOutputPath(sourcePath, ".docx");
                    File.Copy(tempDocxPath, targetPath, true);
                    return new CompressionResult(true, targetPath, details, string.Empty);
                }

                var pdfTarget = BuildOutputPath(sourcePath, ".pdf");
                ConvertDocxToPdf(tempDocxPath, pdfTarget);
                details.Add("输出格式: PDF");
                return new CompressionResult(true, pdfTarget, details, string.Empty);
            }
            catch (Exception ex)
            {
                return new CompressionResult(false, string.Empty, new List<string>(), ex.Message);
            }
            finally
            {
                if (File.Exists(tempDocxPath))
                {
                    File.Delete(tempDocxPath);
                }
            }
        }

        private static CompressionMetrics CompressDocxImages(string docxPath, CompressionQuality quality)
        {
            if (!File.Exists(docxPath))
            {
                return new CompressionMetrics(0, 0, 0);
            }

            using var document = WordprocessingDocument.Open(docxPath, true);
            var mainPart = document.MainDocumentPart;
            if (mainPart == null)
            {
                return new CompressionMetrics(0, 0, 0);
            }

            var totalImages = 0;
            var compressedImages = 0;
            long savedBytes = 0;

            var qualityValue = GetQualityValue(quality);
            var pngCompressionLevel = GetPngCompressionLevel(quality);

            foreach (var imagePart in mainPart.ImageParts)
            {
                totalImages++;

                using var sourceStream = imagePart.GetStream(FileMode.Open, FileAccess.Read);
                using var sourceBuffer = new MemoryStream();
                sourceStream.CopyTo(sourceBuffer);
                var originalLength = sourceBuffer.Length;
                sourceBuffer.Position = 0;

                using var image = new MagickImage(sourceBuffer);
                image.Strip();
                image.Quality = qualityValue;

                if (image.Format == MagickFormat.Png)
                {
                    image.SetDefine(MagickFormat.Png, "compression-level", pngCompressionLevel);
                }
                else if (image.Format == MagickFormat.Jpeg || image.Format == MagickFormat.Jpg)
                {
                    image.Interlace = Interlace.No;
                }

                using var compressedBuffer = new MemoryStream();
                image.Write(compressedBuffer, image.Format);

                if (compressedBuffer.Length < originalLength)
                {
                    compressedBuffer.Position = 0;
                    using var targetStream = imagePart.GetStream(FileMode.Create, FileAccess.Write);
                    compressedBuffer.CopyTo(targetStream);
                    compressedImages++;
                    savedBytes += originalLength - compressedBuffer.Length;
                }
            }

            return new CompressionMetrics(totalImages, compressedImages, savedBytes);
        }

        private void ConvertDocxToPdf(string sourceDocxPath, string targetPdfPath)
        {
            using var pdfWriter = new PdfWriter(targetPdfPath);
            using var pdfDocument = new PdfDocument(pdfWriter);
            using var pdf = new PdfLayoutDocument(pdfDocument);

            using var wordDocument = WordprocessingDocument.Open(sourceDocxPath, false);
            var mainPart = wordDocument.MainDocumentPart;
            if (mainPart?.Document?.Body == null)
            {
                return;
            }

            foreach (var element in mainPart.Document.Body.Elements())
            {
                switch (element)
                {
                    case WordParagraph paragraph:
                        AddParagraphToPdf(paragraph, mainPart, pdf);
                        break;
                    case Table table:
                        var tableText = table.InnerText;
                        if (!string.IsNullOrWhiteSpace(tableText))
                        {
                            pdf.Add(new PdfParagraph(tableText));
                        }
                        break;
                }
            }
        }

        private void AddParagraphToPdf(WordParagraph paragraph, MainDocumentPart mainPart, PdfLayoutDocument pdf)
        {
            var pdfParagraph = new PdfParagraph();
            var hasContent = false;

            foreach (var run in paragraph.Elements<Run>())
            {
                foreach (var text in run.Elements<Text>())
                {
                    if (!string.IsNullOrEmpty(text.Text))
                    {
                        pdfParagraph.Add(text.Text);
                        hasContent = true;
                    }
                }

                foreach (var br in run.Elements<Break>())
                {
                    pdfParagraph.Add(Environment.NewLine);
                    hasContent = true;
                }

                foreach (var drawing in run.Elements<Drawing>())
                {
                    var imageElement = CreatePdfImageFromDrawing(drawing, mainPart);
                    if (imageElement != null)
                    {
                        pdfParagraph.Add(imageElement);
                        hasContent = true;
                    }
                }
            }

            if (!hasContent)
            {
                pdfParagraph.Add(string.Empty);
            }

            pdf.Add(pdfParagraph);
        }

        private PdfImageElement? CreatePdfImageFromDrawing(Drawing drawing, MainDocumentPart mainPart)
        {
            var blip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
            if (blip?.Embed == null)
            {
                return null;
            }

            var relationshipId = blip.Embed.Value;
            if (string.IsNullOrEmpty(relationshipId))
            {
                return null;
            }

            if (mainPart.GetPartById(relationshipId) is not ImagePart imagePart)
            {
                return null;
            }

            using var stream = imagePart.GetStream(FileMode.Open, FileAccess.Read);
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            var imageData = ImageDataFactory.Create(buffer.ToArray());
            var image = new PdfImageElement(imageData);
            image.SetAutoScale(true);
            return image;
        }

        private static List<string> BuildCompressionDetails(CompressionMetrics metrics)
        {
            var details = new List<string>
            {
                $"图片数量: {metrics.TotalImages}",
                $"压缩生效: {metrics.CompressedImages}",
                $"节省空间: {FormatBytes(metrics.SavedBytes)}"
            };

            return details;
        }

        private static string BuildOutputPath(string sourcePath, string newExtension)
        {
            var directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
            var fileName = Path.GetFileNameWithoutExtension(sourcePath);
            var extension = newExtension.StartsWith('.') ? newExtension : $".{newExtension}";
            var candidate = Path.Combine(directory, $"{fileName}_s{extension}");

            if (!File.Exists(candidate))
            {
                return candidate;
            }

            var index = 1;
            while (true)
            {
                var nextCandidate = Path.Combine(directory, $"{fileName}_s({index}){extension}");
                if (!File.Exists(nextCandidate))
                {
                    return nextCandidate;
                }

                index++;
            }
        }

        private static int GetQualityValue(CompressionQuality quality)
        {
            return quality switch
            {
                CompressionQuality.Low => 40,
                CompressionQuality.Medium => 60,
                CompressionQuality.High => 80,
                _ => 60
            };
        }

        private static int GetPngCompressionLevel(CompressionQuality quality)
        {
            return quality switch
            {
                CompressionQuality.Low => 9,
                CompressionQuality.Medium => 6,
                CompressionQuality.High => 3,
                _ => 6
            };
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0)
            {
                return "0 B";
            }

            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            var unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{Math.Round(size, 2)} {units[unitIndex]}";
        }

        private FileProcessResult CleanDocx(string filePath)
        {
            var clearedFields = new List<string>();

            using (var document = WordprocessingDocument.Open(filePath, true))
            {
                var props = document.PackageProperties;

                ClearStringProperty(props.Creator, value => props.Creator = value, "作者", clearedFields);
                ClearStringProperty(props.Title, value => props.Title = value, "标题", clearedFields);
                ClearStringProperty(props.Subject, value => props.Subject = value, "主题", clearedFields);
                ClearStringProperty(props.Keywords, value => props.Keywords = value, "关键字", clearedFields);
                ClearStringProperty(props.Category, value => props.Category = value, "分类", clearedFields);
                ClearStringProperty(props.ContentStatus, value => props.ContentStatus = value, "状态", clearedFields);
                ClearStringProperty(props.Description, value => props.Description = value, "描述", clearedFields);
                ClearStringProperty(props.Identifier, value => props.Identifier = value, "标识", clearedFields);
                ClearStringProperty(props.Language, value => props.Language = value, "语言", clearedFields);
                ClearStringProperty(props.Version, value => props.Version = value, "版本", clearedFields);

                if (props.Created != null)
                {
                    props.Created = null;
                    clearedFields.Add("创建时间");
                }

                if (props.Modified != null)
                {
                    props.Modified = null;
                    clearedFields.Add("修改时间");
                }

                if (props.LastPrinted != null)
                {
                    props.LastPrinted = null;
                    clearedFields.Add("最后打印时间");
                }

                if (document.ExtendedFilePropertiesPart?.Properties != null)
                {
                    var extProps = document.ExtendedFilePropertiesPart.Properties;

                    if (extProps.Company != null && !string.IsNullOrEmpty(extProps.Company.Text))
                    {
                        extProps.Company.Text = string.Empty;
                        clearedFields.Add("公司");
                    }

                    if (extProps.Manager != null && !string.IsNullOrEmpty(extProps.Manager.Text))
                    {
                        extProps.Manager.Text = string.Empty;
                        clearedFields.Add("经理");
                    }
                }

                if (document.CustomFilePropertiesPart?.Properties != null)
                {
                    var customProperties = document.CustomFilePropertiesPart.Properties;
                    if (customProperties.Any())
                    {
                        customProperties.RemoveAllChildren();
                        clearedFields.Add("自定义属性");
                    }
                }
            }

            return clearedFields.Count > 0
                ? new FileProcessResult(filePath, FileProcessStatus.Success, clearedFields, string.Empty)
                : new FileProcessResult(filePath, FileProcessStatus.Skipped, clearedFields, "未检测到可清理的元数据");
        }

        private FileProcessResult CleanPdf(string filePath)
        {
            var clearedFields = new List<string>();
            var tempFile = Path.GetTempFileName();

            try
            {
                var writerProperties = new WriterProperties().SetFullCompressionMode(true);
                using (var pdfDoc = new PdfDocument(new PdfReader(filePath), new PdfWriter(tempFile, writerProperties)))
                {
                    var info = pdfDoc.GetDocumentInfo();

                    ClearPdfInfo(info.GetAuthor(), value => info.SetAuthor(value), "作者", clearedFields);
                    ClearPdfInfo(info.GetCreator(), value => info.SetCreator(value), "创建者", clearedFields);
                    ClearPdfInfo(info.GetProducer(), value => info.SetProducer(value), "生产者", clearedFields);
                    ClearPdfInfo(info.GetKeywords(), value => info.SetKeywords(value), "关键字", clearedFields);
                    ClearPdfInfo(info.GetSubject(), value => info.SetSubject(value), "主题", clearedFields);
                    ClearPdfInfo(info.GetTitle(), value => info.SetTitle(value), "标题", clearedFields);

                    var infoDictionary = pdfDoc.GetTrailer()?.GetAsDictionary(PdfName.Info);
                    if (infoDictionary != null)
                    {
                        var standardKeys = new HashSet<PdfName>
                        {
                            PdfName.Author,
                            PdfName.Creator,
                            PdfName.Producer,
                            PdfName.Keywords,
                            PdfName.Subject,
                            PdfName.Title
                        };

                        var keysToRemove = infoDictionary.KeySet()
                            .Where(key => !standardKeys.Contains(key))
                            .ToList();

                        foreach (var key in keysToRemove)
                        {
                            var value = infoDictionary.GetAsString(key);
                            if (value != null && !string.IsNullOrEmpty(value.ToString()))
                            {
                                infoDictionary.Remove(key);
                                clearedFields.Add(key.GetValue() ?? key.ToString());
                            }
                        }
                    }

                    var catalogObject = pdfDoc.GetCatalog().GetPdfObject();
                    if (catalogObject != null && catalogObject.ContainsKey(PdfName.Metadata))
                    {
                        catalogObject.Remove(PdfName.Metadata);
                        clearedFields.Add("XMP 元数据");
                    }
                }

                ReplaceFile(filePath, tempFile);
            }
            catch
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
                throw;
            }

            return clearedFields.Count > 0
                ? new FileProcessResult(filePath, FileProcessStatus.Success, clearedFields, string.Empty)
                : new FileProcessResult(filePath, FileProcessStatus.Skipped, clearedFields, "未检测到可清理的元数据");
        }

        private FileProcessResult CleanImage(string filePath)
        {
            var clearedFields = new List<string>();
            var tempFile = Path.GetTempFileName();

            try
            {
                using (var image = new MagickImage(filePath))
                {
                    bool changed = false;
                    var clearedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    void AddCleared(string value)
                    {
                        if (clearedSet.Add(value))
                        {
                            clearedFields.Add(value);
                        }
                    }

                    var originalProfiles = image.ProfileNames ?? Array.Empty<string>();
                    var iccProfile = image.GetProfile("icc");
                    var hadComment = !string.IsNullOrEmpty(image.Comment);

                    if (image.HasProfile("exif"))
                    {
                        image.RemoveProfile("exif");
                        AddCleared("EXIF");
                        changed = true;
                    }

                    if (image.HasProfile("iptc"))
                    {
                        image.RemoveProfile("iptc");
                        AddCleared("IPTC");
                        changed = true;
                    }

                    if (image.HasProfile("xmp"))
                    {
                        image.RemoveProfile("xmp");
                        AddCleared("XMP");
                        changed = true;
                    }

                    var hasOtherProfiles = originalProfiles.Any(profile =>
                        !profile.Equals("icc", StringComparison.OrdinalIgnoreCase) &&
                        !profile.Equals("icm", StringComparison.OrdinalIgnoreCase) &&
                        !profile.Equals("exif", StringComparison.OrdinalIgnoreCase) &&
                        !profile.Equals("iptc", StringComparison.OrdinalIgnoreCase) &&
                        !profile.Equals("xmp", StringComparison.OrdinalIgnoreCase));

                    image.Strip();

                    if (iccProfile != null)
                    {
                        image.SetProfile(iccProfile);
                    }

                    if (hasOtherProfiles || hadComment || changed)
                    {
                        AddCleared("通用元数据");
                        changed = true;
                    }

                    if (!changed)
                    {
                        return new FileProcessResult(
                            filePath,
                            FileProcessStatus.Skipped,
                            clearedFields,
                            "未检测到可清理的元数据");
                    }

                    image.Write(tempFile);
                }

                ReplaceFile(filePath, tempFile);
            }
            catch
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                throw;
            }

            return new FileProcessResult(filePath, FileProcessStatus.Success, clearedFields, string.Empty);
        }

        private void ReplaceFile(string destinationPath, string tempFile)
        {
            if (File.Exists(tempFile))
            {
#if NET8_0_OR_GREATER
                File.Copy(tempFile, destinationPath, overwrite: true);
                File.Delete(tempFile);
#else
                File.Delete(destinationPath);
                File.Move(tempFile, destinationPath);
#endif
            }
        }

        private static void ClearStringProperty(
            string? currentValue,
            Action<string?> setter,
            string propertyName,
            ICollection<string> clearedFields)
        {
            if (!string.IsNullOrEmpty(currentValue))
            {
                setter(null);
                clearedFields.Add(propertyName);
            }
        }

        private static void ClearPdfInfo(
            string? currentValue,
            Action<string?> setter,
            string propertyName,
            ICollection<string> clearedFields)
        {
            if (!string.IsNullOrEmpty(currentValue))
            {
                setter(string.Empty);
                clearedFields.Add(propertyName);
            }
        }

        private void UpdateListView(FileProcessResult result)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<FileProcessResult>(UpdateListView), result);
                return;
            }

            if (_listViewItems.TryGetValue(result.FilePath, out var item))
            {
                item.SubItems[1].Text = GetStatusText(result.Status);
                item.SubItems[2].Text = result.ClearedMetadata.Count > 0
                    ? string.Join(", ", result.ClearedMetadata)
                    : result.Status == FileProcessStatus.Success
                        ? "无"
                        : result.ErrorMessage;
            }
        }

        private void AppendResultLog(FileProcessResult result)
        {
            switch (result.Status)
            {
                case FileProcessStatus.Success:
                    AppendLog($"成功清理 {Path.GetFileName(result.FilePath)}: {string.Join(", ", result.ClearedMetadata)}");
                    break;
                case FileProcessStatus.Skipped:
                    AppendLog($"跳过 {Path.GetFileName(result.FilePath)}: {result.ErrorMessage}");
                    break;
                case FileProcessStatus.Failed:
                    AppendLog($"失败 {Path.GetFileName(result.FilePath)}: {result.ErrorMessage}");
                    break;
            }
        }

        private void AppendLog(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AppendLog), message);
                return;
            }

            lock (_logSyncRoot)
            {
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            }
        }

        private static string GetStatusText(FileProcessStatus status)
        {
            return status switch
            {
                FileProcessStatus.Success => "成功",
                FileProcessStatus.Skipped => "跳过",
                FileProcessStatus.Failed => "失败",
                _ => status.ToString()
            };
        }

        private Button btnAddFiles = null!;
        private Button btnAddFolder = null!;
        private Button btnStart = null!;
        private Button btnCompress = null!;
        private ListView lvFiles = null!;
        private ColumnHeader columnHeaderFile = null!;
        private ColumnHeader columnHeaderStatus = null!;
        private ColumnHeader columnHeaderMetadata = null!;
        private ProgressBar progressBar = null!;
        private TextBox txtLog = null!;
    }

    internal enum FileProcessStatus
    {
        Success,
        Failed,
        Skipped
    }

    internal sealed class FileProcessResult
    {
        public FileProcessResult(string filePath, FileProcessStatus status, List<string> clearedMetadata, string errorMessage)
        {
            FilePath = filePath;
            Status = status;
            ClearedMetadata = clearedMetadata;
            ErrorMessage = errorMessage;
        }

        public string FilePath { get; }

        public FileProcessStatus Status { get; }

        public List<string> ClearedMetadata { get; }

        public string ErrorMessage { get; }
    }

    internal enum CompressionQuality
    {
        Low,
        Medium,
        High
    }

    internal enum CompressionOutputFormat
    {
        Docx,
        Pdf
    }

    internal sealed class CompressionMetrics
    {
        public CompressionMetrics(int totalImages, int compressedImages, long savedBytes)
        {
            TotalImages = totalImages;
            CompressedImages = compressedImages;
            SavedBytes = savedBytes;
        }

        public int TotalImages { get; }

        public int CompressedImages { get; }

        public long SavedBytes { get; }
    }

    internal sealed class CompressionResult
    {
        public CompressionResult(bool success, string outputPath, List<string> details, string errorMessage)
        {
            Success = success;
            OutputPath = outputPath;
            Details = details;
            ErrorMessage = errorMessage;
        }

        public bool Success { get; }

        public string OutputPath { get; }

        public List<string> Details { get; }

        public string ErrorMessage { get; }
    }
}
