using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using CEngine;
namespace MagiConvert
{
	public partial class MainWindow : Form
	{
        private readonly HashSet<string> supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public MainWindow()
		{
			InitializeComponent();
            InitializeSupportedExtensions();
            btnConvertFolder.Enabled = false;
		}

        private void ButtonBrowse_Click(object sender, EventArgs e)
        {
            labelDone.Text = "";
            openFileDialog = new OpenFileDialog
            {

                Filter = File.ReadAllText("Filter.flt")

            };
            if(openFileDialog.ShowDialog() == DialogResult.OK ) 
            {
                InputPath.Text = openFileDialog.FileName;
            }
            string outputString = InputPath.Text;
            if (outputString != "")
                outputString = outputString.Remove(outputString.LastIndexOf('.'));
            outputString += format;
            OutputPath.Text = outputString;

        }
        private void ButtonConvert_Click(object sender, EventArgs e)
        {
            ConvertEngine.Convert(InputPath.Text, OutputPath.Text, format, checkBoxTransparent.Checked);
            labelDone.ForeColor= Color.Green;
            labelDone.Text = "\u2713" + "Done !";
        }

        private void RadioButtonBMP_CheckedChanged(object sender, EventArgs e)
        {
            labelDone.Text = "";
            checkBoxTransparent.Checked = false;
            checkBoxTransparent.Enabled = false;
            format = ".bmp";
            GetFormat();
        }

        private void RadioButtonDIB_CheckedChanged(object sender, EventArgs e)
        {
            labelDone.Text = "";
            checkBoxTransparent.Checked = false;
            checkBoxTransparent.Enabled = false;
            format = ".dib";
            GetFormat();
        }

        private void RadioButtonICO_CheckedChanged(object sender, EventArgs e)
        {
            labelDone.Text = "";
            checkBoxTransparent.Enabled = true;
            format = ".ico";
            GetFormat();
        }

        private void RadioButtonJPE_CheckedChanged(object sender, EventArgs e)
        {
            labelDone.Text = "";
            checkBoxTransparent.Checked = false;
            checkBoxTransparent.Enabled = false;
            format = ".jpe";
            GetFormat();
        }

        private void RadioButtonJPEG_CheckedChanged(object sender, EventArgs e)
        {
            labelDone.Text = "";
            checkBoxTransparent.Checked = false;
            checkBoxTransparent.Enabled = false;
            format = ".jpeg";
            GetFormat();
        }

        private void RadioButtonJPG_CheckedChanged(object sender, EventArgs e)
        {
            labelDone.Text = "";
            checkBoxTransparent.Checked = false;
            checkBoxTransparent.Enabled = false;
            format = ".jpg";
            GetFormat();
        }

        private void RadioButtonTIF_CheckedChanged(object sender, EventArgs e)
        {
            labelDone.Text = "";
            checkBoxTransparent.Checked = false;
            checkBoxTransparent.Enabled = false;
            format = ".tif";
            GetFormat();
        }
        private void RadioButtonPNG_CheckedChanged(object sender, EventArgs e)
        {
            labelDone.Text = "";
            checkBoxTransparent.Enabled = true;
            format = ".png";
            GetFormat();
        }
        private void RadioButtonTIFF_CheckedChanged(object sender, EventArgs e)
        {
            labelDone.Text = "";
            checkBoxTransparent.Checked = false;
            checkBoxTransparent.Enabled = false;
            format = ".tiff";
            GetFormat();
        }

        private void RadioButtonWEBP_CheckedChanged(object sender, EventArgs e)
        {
            labelDone.Text = "";
            checkBoxTransparent.Checked = false;
            checkBoxTransparent.Enabled = false;
            format = ".webp";
            GetFormat();
        }
        private void GetFormat()
        {
            string outputString = InputPath.Text;
            if (outputString != "")
                outputString = outputString.Remove(outputString.LastIndexOf('.'));
            outputString += format;
            OutputPath.Text = outputString;
        }

        private void InitializeSupportedExtensions()
        {
            if (!File.Exists("Filter.flt"))
                return;

            string filterText = File.ReadAllText("Filter.flt");
            foreach (Match match in Regex.Matches(filterText, @"\*\.([a-zA-Z0-9]+)|\*([a-zA-Z0-9]+)"))
            {
                string extension = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                if (string.IsNullOrWhiteSpace(extension))
                    continue;
                supportedExtensions.Add("." + extension.TrimStart('.'));
            }
        }

        private void BtnSelectInputFolder_Click(object sender, EventArgs e)
        {
            if (TrySelectFolder(txtInputFolder))
            {
                var validation = ValidateInputFolder(txtInputFolder.Text);
                if (!validation.ok)
                    MessageBox.Show(validation.message, "Invalid Input Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            UpdateBatchControls();
        }

        private void BtnSelectOutputFolder_Click(object sender, EventArgs e)
        {
            if (TrySelectFolder(txtOutputFolder))
            {
                var validation = ValidateOutputFolder(txtOutputFolder.Text);
                if (!validation.ok)
                    MessageBox.Show(validation.message, "Invalid Output Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            UpdateBatchControls();
        }

        private async void BtnConvertFolder_Click(object sender, EventArgs e)
        {
            txtBatchLog.Clear();
            lblBatchStatus.Text = "";
            progressBarBatch.Value = 0;

            var inputValidation = ValidateInputFolder(txtInputFolder.Text);
            if (!inputValidation.ok)
            {
                MessageBox.Show(inputValidation.message, "Invalid Input Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdateBatchControls();
                return;
            }

            var outputValidation = ValidateOutputFolder(txtOutputFolder.Text);
            if (!outputValidation.ok)
            {
                MessageBox.Show(outputValidation.message, "Invalid Output Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdateBatchControls();
                return;
            }

            SetBatchUiEnabled(false);
            var progress = new Progress<ProgressInfo>(info =>
            {
                progressBarBatch.Maximum = info.Total;
                progressBarBatch.Value = info.Current;
                lblBatchStatus.Text = $"{info.Current}/{info.Total} - {info.FileName}";
            });

            BatchResult result = await Task.Run(() => ConvertBatch(txtInputFolder.Text, txtOutputFolder.Text, progress));

            progressBarBatch.Value = progressBarBatch.Maximum;
            lblBatchStatus.Text = $"Completed {result.SuccessCount}/{result.TotalCount}";
            if (result.Errors.Count > 0)
            {
                txtBatchLog.Text = string.Join(Environment.NewLine, result.Errors);
            }

            string summary = $"Completed batch conversion.\nSuccess: {result.SuccessCount}\nFailed: {result.Errors.Count}";
            if (result.Errors.Count > 0)
            {
                int displayCount = Math.Min(result.Errors.Count, 5);
                summary += "\n\nErrors:\n" + string.Join("\n", result.Errors.GetRange(0, displayCount));
            }

            MessageBox.Show(summary, "Batch Conversion", MessageBoxButtons.OK, result.Errors.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            SetBatchUiEnabled(true);
            UpdateBatchControls();
        }

        private void UpdateBatchControls()
        {
            var inputValidation = ValidateInputFolder(txtInputFolder.Text);
            var outputValidation = ValidateOutputFolder(txtOutputFolder.Text);
            btnConvertFolder.Enabled = inputValidation.ok && outputValidation.ok;
        }

        private void SetBatchUiEnabled(bool enabled)
        {
            btnSelectInputFolder.Enabled = enabled;
            btnSelectOutputFolder.Enabled = enabled;
            btnConvertFolder.Enabled = enabled;
            buttonBrowse.Enabled = enabled;
            buttonConvert.Enabled = enabled;
            groupBoxBMP.Enabled = enabled;
        }

        private bool TrySelectFolder(TextBox targetTextBox)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = Directory.Exists(targetTextBox.Text) ? targetTextBox.Text : "";
                folderDialog.ShowNewFolderButton = true;
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    targetTextBox.Text = folderDialog.SelectedPath;
                    return true;
                }
            }

            return false;
        }

        private (bool ok, string message) ValidateInputFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return (false, "Please select an input folder.");

            if (!Directory.Exists(path))
                return (false, "Input folder does not exist.");

            List<string> images = GetInputImages(path);
            if (images.Count == 0)
                return (false, "Input folder does not contain any supported image files.");

            return (true, string.Empty);
        }

        private (bool ok, string message) ValidateOutputFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return (false, "Please select an output folder.");

            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                string testFile = Path.Combine(path, Path.GetRandomFileName());
                using (File.Create(testFile))
                {
                }
                File.Delete(testFile);
            }
            catch (Exception ex)
            {
                return (false, $"Output folder is not writable. {ex.Message}");
            }

            return (true, string.Empty);
        }

        private List<string> GetInputImages(string inputDir)
        {
            var results = new List<string>();
            if (supportedExtensions.Count == 0)
                return results;

            foreach (string file in Directory.EnumerateFiles(inputDir))
            {
                string extension = Path.GetExtension(file);
                if (supportedExtensions.Contains(extension))
                    results.Add(file);
            }

            return results;
        }

        private string GetUniqueOutputPath(string outputDir, string baseName, string ext)
        {
            string candidate = Path.Combine(outputDir, baseName + ext);
            int counter = 1;
            while (File.Exists(candidate))
            {
                candidate = Path.Combine(outputDir, $"{baseName}_{counter}{ext}");
                counter++;
            }

            return candidate;
        }

        private void ConvertSingleImage(string inputPath, string outputPath)
        {
            ConvertEngine.Convert(inputPath, outputPath, format, checkBoxTransparent.Checked);
        }

        private BatchResult ConvertBatch(string inputDir, string outputDir, IProgress<ProgressInfo> progress)
        {
            List<string> images = GetInputImages(inputDir);
            var result = new BatchResult { TotalCount = images.Count };

            for (int index = 0; index < images.Count; index++)
            {
                string inputPath = images[index];
                string baseName = Path.GetFileNameWithoutExtension(inputPath);
                string outputPath = GetUniqueOutputPath(outputDir, baseName, format);

                try
                {
                    ConvertSingleImage(inputPath, outputPath);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{Path.GetFileName(inputPath)}: {ex.Message}");
                }

                progress?.Report(new ProgressInfo(index + 1, images.Count, Path.GetFileName(inputPath)));
            }

            return result;
        }

        private sealed class BatchResult
        {
            public int TotalCount { get; set; }
            public int SuccessCount { get; set; }
            public List<string> Errors { get; } = new List<string>();
        }

        private readonly struct ProgressInfo
        {
            public ProgressInfo(int current, int total, string fileName)
            {
                Current = current;
                Total = total;
                FileName = fileName;
            }

            public int Current { get; }
            public int Total { get; }
            public string FileName { get; }
        }
    }
}
