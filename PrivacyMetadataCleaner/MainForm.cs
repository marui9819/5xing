using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.ExtendedProperties;
using iText.Kernel.Pdf;
using ImageMagick;

namespace PrivacyMetadataCleaner
{
    public partial class MainForm : Form
    {
        private readonly HashSet<string> _pendingFiles = new(StringComparer.OrdinalIgnoreCase);
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
            btnSaveLog = new Button();
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
            // btnSaveLog
            // 
            btnSaveLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSaveLog.Location = new System.Drawing.Point(542, 12);
            btnSaveLog.Name = "btnSaveLog";
            btnSaveLog.Size = new System.Drawing.Size(120, 34);
            btnSaveLog.TabIndex = 3;
            btnSaveLog.Text = "保存日志";
            btnSaveLog.UseVisualStyleBackColor = true;
            btnSaveLog.Click += btnSaveLog_Click;
            // 
            // lvFiles
            // 
            lvFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lvFiles.Columns.AddRange(new[] { columnHeaderFile, columnHeaderStatus, columnHeaderMetadata });
            lvFiles.FullRowSelect = true;
            lvFiles.GridLines = true;
            lvFiles.Location = new System.Drawing.Point(12, 56);
            lvFiles.Name = "lvFiles";
            lvFiles.Size = new System.Drawing.Size(776, 280);
            lvFiles.TabIndex = 4;
            lvFiles.UseCompatibleStateImageBehavior = false;
            lvFiles.View = View.Details;
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
            Controls.Add(btnSaveLog);
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

        private async void btnStart_Click(object? sender, EventArgs e)
        {
            await StartProcessingAsync();
        }

        private void btnSaveLog_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "保存日志",
                Filter = "文本文件|*.txt|所有文件|*.*",
                FileName = $"PrivacyMetadataCleaner_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                File.WriteAllText(dialog.FileName, txtLog.Text);
            }
        }

        private async Task StartProcessingAsync()
        {
            if (_pendingFiles.Count == 0)
            {
                MessageBox.Show(this, "请先添加要处理的文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var filesToProcess = _pendingFiles.ToList();

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

                var result = await Task.Run(() => ProcessFile(filePath));
                processed++;
                _pendingFiles.Remove(filePath);

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
            btnSaveLog.Enabled = enabled;
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

                if (_pendingFiles.Contains(file))
                {
                    continue;
                }

                _pendingFiles.Add(file);

                if (_listViewItems.TryGetValue(file, out var existingItem))
                {
                    existingItem.SubItems[1].Text = "等待";
                    existingItem.SubItems[2].Text = string.Empty;
                    added++;
                    continue;
                }

                var item = new ListViewItem(file)
                {
                    Tag = file
                };
                item.SubItems.Add("等待");
                item.SubItems.Add(string.Empty);
                lvFiles.Items.Add(item);
                _listViewItems[file] = item;
                added++;
            }

            if (added > 0)
            {
                AppendLog($"已添加 {added} 个文件。当前总数: {_pendingFiles.Count}");
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

                    var infoDictionary = info.GetPdfObject();
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
        private Button btnSaveLog = null!;
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
}
