using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PrivacyMetadataCleaner
{
    internal sealed class CompressionOptionsForm : Form
    {
        public CompressionOptionsForm()
        {
            InitializeComponent();
        }

        public CompressionQuality SelectedQuality { get; private set; } = CompressionQuality.Medium;

        public CompressionOutputFormat SelectedFormat { get; private set; } = CompressionOutputFormat.Docx;

        private void InitializeComponent()
        {
            cmbQuality = new ComboBox();
            cmbFormat = new ComboBox();
            lblQuality = new Label();
            lblFormat = new Label();
            btnOk = new Button();
            btnCancel = new Button();

            SuspendLayout();

            Text = "选择压缩参数";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new System.Drawing.Size(360, 180);

            lblQuality.AutoSize = true;
            lblQuality.Location = new System.Drawing.Point(24, 28);
            lblQuality.Text = "压缩级别：";

            cmbQuality.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbQuality.FormattingEnabled = true;
            cmbQuality.Location = new System.Drawing.Point(130, 24);
            cmbQuality.Width = 200;
            cmbQuality.DataSource = new List<ComboItem<CompressionQuality>>
            {
                new ComboItem<CompressionQuality>("低（尺寸优先）", CompressionQuality.Low),
                new ComboItem<CompressionQuality>("中（平衡）", CompressionQuality.Medium),
                new ComboItem<CompressionQuality>("高（质量优先）", CompressionQuality.High)
            };
            if (cmbQuality.Items.Count > 1)
            {
                cmbQuality.SelectedIndex = 1;
            }

            lblFormat.AutoSize = true;
            lblFormat.Location = new System.Drawing.Point(24, 78);
            lblFormat.Text = "输出格式：";

            cmbFormat.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFormat.FormattingEnabled = true;
            cmbFormat.Location = new System.Drawing.Point(130, 74);
            cmbFormat.Width = 200;
            cmbFormat.DataSource = new List<ComboItem<CompressionOutputFormat>>
            {
                new ComboItem<CompressionOutputFormat>("Word 文档 (*.docx)", CompressionOutputFormat.Docx),
                new ComboItem<CompressionOutputFormat>("PDF 文档 (*.pdf)", CompressionOutputFormat.Pdf)
            };
            if (cmbFormat.Items.Count > 0)
            {
                cmbFormat.SelectedIndex = 0;
            }

            btnOk.Text = "确定";
            btnOk.Location = new System.Drawing.Point(170, 124);
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Click += btnOk_Click;

            btnCancel.Text = "取消";
            btnCancel.Location = new System.Drawing.Point(260, 124);
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Click += btnCancel_Click;

            Controls.Add(lblQuality);
            Controls.Add(cmbQuality);
            Controls.Add(lblFormat);
            Controls.Add(cmbFormat);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            ResumeLayout(false);
            PerformLayout();
        }

        private void btnOk_Click(object? sender, EventArgs e)
        {
            if (cmbQuality.SelectedItem is ComboItem<CompressionQuality> qualityItem)
            {
                SelectedQuality = qualityItem.Value;
            }

            if (cmbFormat.SelectedItem is ComboItem<CompressionOutputFormat> formatItem)
            {
                SelectedFormat = formatItem.Value;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private ComboBox cmbQuality = null!;
        private ComboBox cmbFormat = null!;
        private Label lblQuality = null!;
        private Label lblFormat = null!;
        private Button btnOk = null!;
        private Button btnCancel = null!;

        private sealed class ComboItem<T>
        {
            public ComboItem(string text, T value)
            {
                Text = text;
                Value = value;
            }

            public string Text { get; }

            public T Value { get; }

            public override string ToString()
            {
                return Text;
            }
        }
    }
}
