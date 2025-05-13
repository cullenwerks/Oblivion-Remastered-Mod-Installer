using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Aspose.Zip;
using Aspose.Zip.SevenZip;
using Aspose.Zip.Rar;
using Microsoft.Win32;

namespace ORMI
{
    public partial class Form1 : Form
    {
        private TextBox? txtInstallDir;
        private Button? btnBrowseInstall;
        private TextBox? txtModFile;
        private Button? btnBrowseMod;
        private Button? btnInstall;
        private ListBox? lstInstalledMods;
        private Button? btnRemoveMod;
        private Label? lblInstallDir;
        private Label? lblModFile;
        private Button? btnRefreshModList;
        private CheckBox? chkCustomInstall;
        private Button? btnCustomHelp;
        private TextBox? txtCustomInstructions;
        private Label? lblCustomInstructions;
        private Label? lblInstalledMods;
        private ProgressBar? prgInstall;
        private ToolTip? toolTip;
        private ContextMenuStrip? ctxMenuMods;
        private Dictionary<string, List<string>> modFiles;
        private System.Windows.Forms.Timer? animationTimer;
        private const string ConfigFilePath = "ORMIConfig.json";
        private const string LogFilePath = "ORMI.log";
        private const string AppVersion = "1.0.0";
        private readonly string batchScriptPath = @"..\Oblivion Remastered Mod Installer.bat";

        private class AppConfig
        {
            public string? InstallDir { get; set; }
        }

        public Form1()
        {
            InitializeComponent();
            modFiles = new Dictionary<string, List<string>>();
            SetupForm();
            LoadConfig();
            LoadModFiles();
            UpdateModList();
            ApplyTheme();
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }

        private void SystemEvents_UserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                ApplyTheme();
                Log("System theme changed, reapplied UI theme.");
            }
        }

        private bool IsSystemDarkTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int lightTheme && lightTheme == 0;
            }
            catch (Exception ex)
            {
                Log($"Error checking system theme: {ex.Message}");
                return false;
            }
        }

        private void SetupForm()
        {
            Text = $"Oblivion Remastered Mod Installer v{AppVersion}";
            MinimumSize = new Size(600, 500);
            Size = new Size(600, 500);
            AutoScaleMode = AutoScaleMode.Dpi;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            Font = new Font("Segoe UI", 9);

            var menuStrip = new MenuStrip();
            var helpMenu = new ToolStripMenuItem("Help");
            var aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (s, e) => MessageBox.Show($"Oblivion Remastered Mod Installer v{AppVersion}\nA tool to manage mods for Oblivion Remastered.\n", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            helpMenu.DropDownItems.Add(aboutItem);
            menuStrip.Items.Add(helpMenu);
            Controls.Add(menuStrip);

            animationTimer = new System.Windows.Forms.Timer { Interval = 10 };
            animationTimer.Tick += AnimationTimer_Tick;

            toolTip = new ToolTip { AutoPopDelay = 10000, InitialDelay = 500, ShowAlways = true };
            toolTip.OwnerDraw = true;
            toolTip.Draw += (s, e) =>
            {
                e.DrawBackground();
                e.DrawBorder();
                using var brush = IsSystemDarkTheme() ? Brushes.White : Brushes.Black;
                e.Graphics.DrawString(e.ToolTipText, new Font("Segoe UI", 8), brush, e.Bounds, StringFormat.GenericDefault);
            };

            ctxMenuMods = new ContextMenuStrip();
            ctxMenuMods.Items.Add("Remove Mod", null, BtnRemoveMod_Click);
            ctxMenuMods.Items.Add("Refresh List", null, BtnRefreshModList_Click);

            var layoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 10, // Reduced rows due to removed Nexus controls
                Padding = new Padding(10),
                AutoSize = true
            };
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            lblInstallDir = new Label { Text = "Oblivion Remastered Installation Directory:", AutoSize = true };
            layoutPanel.Controls.Add(lblInstallDir, 0, 0);

            txtInstallDir = new TextBox { ReadOnly = true, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseInstall = new Button { Text = "Browse", FlatStyle = FlatStyle.Flat, Size = new Size(100, 23) };
            btnBrowseInstall.MouseDown += Button_MouseDown;
            btnBrowseInstall.MouseUp += Button_MouseUp;
            btnBrowseInstall.Click += BtnBrowseInstall_Click;
            layoutPanel.Controls.Add(txtInstallDir, 0, 1);
            layoutPanel.Controls.Add(btnBrowseInstall, 1, 1);

            lblModFile = new Label { Text = "Mod Archive File:", AutoSize = true };
            layoutPanel.Controls.Add(lblModFile, 0, 2);

            txtModFile = new TextBox { ReadOnly = true, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseMod = new Button { Text = "Browse", FlatStyle = FlatStyle.Flat, Size = new Size(100, 23) };
            btnBrowseMod.MouseDown += Button_MouseDown;
            btnBrowseMod.MouseUp += Button_MouseUp;
            btnBrowseMod.Click += BtnBrowseMod_Click;
            layoutPanel.Controls.Add(txtModFile, 0, 3);
            layoutPanel.Controls.Add(btnBrowseMod, 1, 3);

            chkCustomInstall = new CheckBox { Text = "Enable Custom Installation", AutoSize = true };
            toolTip.SetToolTip(chkCustomInstall, "Check to specify custom file placements or instructions.\nExample: 'Place in Content/LogicMods' or 'Edit mods.txt'.\nSee the question mark for details or use Vortex for supported mods.");
            layoutPanel.Controls.Add(chkCustomInstall, 0, 4);

            btnCustomHelp = new Button
            {
                Text = "?",
                FlatStyle = FlatStyle.Flat,
                Size = new Size(23, 23),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            toolTip.SetToolTip(btnCustomHelp, "Click for help on custom installation.\nEnter instructions like 'Place in Content/LogicMods' or 'Edit mods.txt' in the text box below when enabled.");
            btnCustomHelp.Click += BtnCustomHelp_Click;
            btnCustomHelp.MouseDown += Button_MouseDown;
            btnCustomHelp.MouseUp += Button_MouseUp;
            layoutPanel.Controls.Add(btnCustomHelp, 2, 4);

            lblCustomInstructions = new Label { Text = "Custom Installation Instructions:", AutoSize = true };
            layoutPanel.Controls.Add(lblCustomInstructions, 0, 5);
            layoutPanel.SetColumnSpan(lblCustomInstructions, 3);

            txtCustomInstructions = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Height = 60,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Enabled = false
            };
            chkCustomInstall.CheckedChanged += (s, e) => txtCustomInstructions!.Enabled = chkCustomInstall!.Checked;
            layoutPanel.Controls.Add(txtCustomInstructions, 0, 6);
            layoutPanel.SetColumnSpan(txtCustomInstructions, 3);

            lblInstalledMods = new Label { Text = "Installed Mods:", AutoSize = true };
            layoutPanel.Controls.Add(lblInstalledMods, 0, 7);
            layoutPanel.SetColumnSpan(lblInstalledMods, 3);

            lstInstalledMods = new ListBox
            {
                SelectionMode = SelectionMode.One,
                ContextMenuStrip = ctxMenuMods,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            layoutPanel.Controls.Add(lstInstalledMods, 0, 8);
            layoutPanel.SetColumnSpan(lstInstalledMods, 3);

            var buttonsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Anchor = AnchorStyles.Left
            };

            btnInstall = new Button { Text = "Install Mod", FlatStyle = FlatStyle.Flat, Size = new Size(100, 30) };
            btnInstall.MouseDown += Button_MouseDown;
            btnInstall.MouseUp += Button_MouseUp;
            btnInstall.Click += BtnInstall_Click;
            buttonsPanel.Controls.Add(btnInstall);

            btnRemoveMod = new Button { Text = "Remove Selected Mod", FlatStyle = FlatStyle.Flat, Size = new Size(150, 30) };
            btnRemoveMod.MouseDown += Button_MouseDown;
            btnRemoveMod.MouseUp += Button_MouseUp;
            btnRemoveMod.Click += BtnRemoveMod_Click;
            buttonsPanel.Controls.Add(btnRemoveMod);

            btnRefreshModList = new Button { Text = "Refresh Mod List", FlatStyle = FlatStyle.Flat, Size = new Size(150, 30) };
            btnRefreshModList.MouseDown += Button_MouseDown;
            btnRefreshModList.MouseUp += Button_MouseUp;
            btnRefreshModList.Click += BtnRefreshModList_Click;
            buttonsPanel.Controls.Add(btnRefreshModList);

            layoutPanel.Controls.Add(buttonsPanel, 0, 9);
            layoutPanel.SetColumnSpan(buttonsPanel, 3);

            prgInstall = new ProgressBar
            {
                Visible = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Maximum = 100,
                Minimum = 0,
                Value = 0
            };
            layoutPanel.Controls.Add(prgInstall, 0, 10);
            layoutPanel.SetColumnSpan(prgInstall, 3);

            Controls.Add(layoutPanel);
        }

        private void ApplyTheme()
        {
            bool isDark = IsSystemDarkTheme();
            BackColor = isDark ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            lblInstallDir!.ForeColor = isDark ? Color.White : Color.Black;
            lblModFile!.ForeColor = isDark ? Color.White : Color.Black;
            lblCustomInstructions!.ForeColor = isDark ? Color.White : Color.Black;
            lblInstalledMods!.ForeColor = isDark ? Color.White : Color.Black;
            txtInstallDir!.BackColor = isDark ? Color.FromArgb(45, 45, 45) : Color.White;
            txtInstallDir.ForeColor = isDark ? Color.White : Color.Black;
            txtModFile!.BackColor = isDark ? Color.FromArgb(45, 45, 45) : Color.White;
            txtModFile.ForeColor = isDark ? Color.White : Color.Black;
            txtCustomInstructions!.BackColor = isDark ? Color.FromArgb(45, 45, 45) : Color.White;
            txtCustomInstructions.ForeColor = isDark ? Color.White : Color.Black;
            lstInstalledMods!.BackColor = isDark ? Color.FromArgb(45, 45, 45) : Color.White;
            lstInstalledMods.ForeColor = isDark ? Color.White : Color.Black;
            btnInstall!.BackColor = isDark ? Color.FromArgb(0, 120, 215) : SystemColors.Control;
            btnInstall.ForeColor = isDark ? Color.White : Color.Black;
            btnBrowseInstall!.BackColor = isDark ? Color.FromArgb(0, 120, 215) : SystemColors.Control;
            btnBrowseInstall.ForeColor = isDark ? Color.White : Color.Black;
            btnBrowseMod!.BackColor = isDark ? Color.FromArgb(0, 120, 215) : SystemColors.Control;
            btnBrowseMod.ForeColor = isDark ? Color.White : Color.Black;
            btnRemoveMod!.BackColor = isDark ? Color.FromArgb(0, 120, 215) : SystemColors.Control;
            btnRemoveMod.ForeColor = isDark ? Color.White : Color.Black;
            btnRefreshModList!.BackColor = isDark ? Color.FromArgb(0, 120, 215) : SystemColors.Control;
            btnRefreshModList.ForeColor = isDark ? Color.White : Color.Black;
            btnCustomHelp!.BackColor = isDark ? Color.FromArgb(0, 120, 215) : SystemColors.Control;
            btnCustomHelp.ForeColor = isDark ? Color.White : Color.Black;
            prgInstall!.BackColor = isDark ? Color.FromArgb(45, 45, 45) : SystemColors.Control;
            prgInstall.ForeColor = isDark ? Color.FromArgb(0, 120, 215) : SystemColors.Highlight;
        }

        private void BtnCustomHelp_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "Custom Installation Help:\n\n" +
                "Use this feature for mods requiring non-standard file placements or configuration edits, as specified in their mod descriptions.\n\n" +
                "1. Check 'Enable Custom Installation' to activate the text box.\n" +
                "2. Enter instructions in the text box, one per line:\n" +
                "   - File placement: Use 'Place in <path>' or 'Copy to <path>' (e.g., 'Place in Content/LogicMods').\n" +
                "   - Configuration edits: Use 'Edit <file>' (e.g., 'Edit Binaries/Win64/ue4ss/Mods/mods.txt to add MyMod').\n" +
                "3. The program will copy files to specified paths or prompt you to manually edit configuration files.\n\n" +
                "Examples:\n" +
                "- Place in Content/LogicMods\n" +
                "- Copy to Binaries/Win64/ue4ss/Mods\n" +
                "- Edit Binaries/Win64/ue4ss/Mods/mods.txt to add MyMod\n\n" +
                "For mods supported by Vortex, consider using the Vortex mod manager.",
                "Custom Installation Help",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void Button_MouseDown(object? sender, MouseEventArgs e)
        {
            animationTimer?.Start();
        }

        private void Button_MouseUp(object? sender, MouseEventArgs e)
        {
            animationTimer?.Stop();
            ((Control)sender).Invalidate();
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            btnInstall?.Invalidate();
            btnBrowseInstall?.Invalidate();
            btnBrowseMod?.Invalidate();
            btnRemoveMod?.Invalidate();
            btnRefreshModList?.Invalidate();
            btnCustomHelp?.Invalidate();
        }

        private void BtnBrowseInstall_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtInstallDir!.Text = dialog.SelectedPath;
                SaveConfig();
                UpdateModList();
                Log($"Installation directory set to: {txtInstallDir.Text}");
            }
        }

        private void BtnBrowseMod_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            dialog.Filter = "Archive files (*.zip;*.7z;*.rar)|*.zip;*.7z;*.rar";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtModFile!.Text = dialog.FileName;
                Log($"Mod file selected: {txtModFile.Text}");
            }
        }

        private async void BtnInstall_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtInstallDir?.Text) || string.IsNullOrEmpty(txtModFile?.Text))
            {
                MessageBox.Show("Please select both the installation directory and the mod file.");
                Log("Installation failed: Missing installation directory or mod file.");
                return;
            }

            if (!Directory.Exists(txtInstallDir.Text))
            {
                MessageBox.Show("The specified installation directory does not exist. Please select a valid directory.");
                Log($"Installation failed: Directory {txtInstallDir.Text} does not exist.");
                return;
            }

            if (!File.Exists(txtModFile.Text))
            {
                MessageBox.Show("The specified mod file does not exist. Please select a valid file.");
                Log($"Installation failed: Mod file {txtModFile.Text} does not exist.");
                return;
            }

            prgInstall!.Visible = true;
            prgInstall.Value = 0;
            Log($"Starting mod installation from {txtModFile.Text} to {txtInstallDir.Text}");

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                Directory.CreateDirectory(tempDir);

                string extension = Path.GetExtension(txtModFile.Text).ToLower();
                if (extension == ".zip")
                {
                    ZipFile.ExtractToDirectory(txtModFile.Text, tempDir);
                    prgInstall.Value = 20;
                    Log("Extracted .zip archive successfully.");
                }
                else if (extension == ".7z")
                {
                    try
                    {
                        using var archive = new SevenZipArchive(txtModFile.Text);
                        archive.ExtractToDirectory(tempDir);
                        prgInstall.Value = 20;
                        Log("Extracted .7z archive successfully.");
                    }
                    catch (InvalidDataException)
                    {
                        string? password = PromptForPassword();
                        if (string.IsNullOrEmpty(password))
                        {
                            throw new Exception("Password required for encrypted .7z archive.");
                        }
                        using var archive = new SevenZipArchive(txtModFile.Text, password);
                        archive.ExtractToDirectory(tempDir);
                        prgInstall.Value = 20;
                        Log("Extracted password-protected .7z archive successfully.");
                    }
                }
                else if (extension == ".rar")
                {
                    try
                    {
                        using var archive = new RarArchive(txtModFile.Text);
                        archive.ExtractToDirectory(tempDir);
                        prgInstall.Value = 20;
                        Log("Extracted .rar archive successfully.");
                    }
                    catch (InvalidDataException)
                    {
                        string? password = PromptForPassword();
                        if (string.IsNullOrEmpty(password))
                        {
                            throw new Exception("Password required for encrypted .rar archive.");
                        }
                        using var archive = new RarArchive(txtModFile.Text);
                        foreach (var entry in archive.Entries)
                        {
                            string entryPath = Path.Combine(tempDir, entry.Name.Replace('/', Path.DirectorySeparatorChar));
                            string entryDir = Path.GetDirectoryName(entryPath)!;
                            if (!string.IsNullOrEmpty(entryDir))
                            {
                                Directory.CreateDirectory(entryDir);
                            }
                            using var entryStream = entry.Open(password);
                            using var fileStream = File.Create(entryPath);
                            entryStream.CopyTo(fileStream);
                        }
                        prgInstall.Value = 20;
                        Log("Extracted password-protected .rar archive successfully.");
                    }
                }
                else
                {
                    throw new NotSupportedException("Unsupported archive format: " + extension);
                }

                var files = Directory.EnumerateFiles(tempDir, "*.*", SearchOption.AllDirectories);
                int totalFiles = files.Count();
                int processedFiles = 0;

                string? pluginName = null;
                List<string> installedFiles = [];

                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();

                    if (chkCustomInstall?.Checked == true && !string.IsNullOrEmpty(txtCustomInstructions?.Text))
                    {
                        string[] instructions = txtCustomInstructions.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var instruction in instructions)
                        {
                            string trimmed = instruction.Trim();
                            if (trimmed.StartsWith("Place in ", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("Copy to ", StringComparison.OrdinalIgnoreCase))
                            {
                                string destPath = trimmed[(trimmed.IndexOf(" ", StringComparison.OrdinalIgnoreCase) + 1)..].Trim();
                                string fullDestDir = Path.Combine(txtInstallDir!.Text, destPath);
                                if (!Directory.Exists(fullDestDir))
                                {
                                    Directory.CreateDirectory(fullDestDir);
                                }
                                string destFile = Path.Combine(fullDestDir, Path.GetFileName(file));
                                File.Copy(file, destFile, true);
                                installedFiles.Add(destFile);
                                if (ext == ".esp")
                                {
                                    pluginName = Path.GetFileName(file);
                                }
                                Log($"Copied file to custom path: {destFile}");
                            }
                            else if (trimmed.StartsWith("Edit ", StringComparison.OrdinalIgnoreCase))
                            {
                                MessageBox.Show($"Please manually perform the following instruction: {trimmed}\nRefer to the mod's description for details.", "Manual Configuration Required");
                                Log($"Prompted user for manual edit: {trimmed}");
                            }
                        }
                    }

                    if (ext == ".esp" && (pluginName == null || installedFiles.Count == 0))
                    {
                        string destDir = Path.Combine(txtInstallDir!.Text, "Content", "Dev", "ObvData", "Data");
                        if (!Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        string destFile = Path.Combine(destDir, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                        installedFiles.Add(destFile);
                        pluginName = Path.GetFileName(file);

                        string pluginsFile = Path.Combine(destDir, "plugins.txt");
                        var pluginList = new List<string>();
                        if (File.Exists(pluginsFile))
                        {
                            pluginList.AddRange(File.ReadAllLines(pluginsFile).Where(line => !string.IsNullOrWhiteSpace(line)));
                        }
                        if (!pluginList.Contains(pluginName, StringComparer.OrdinalIgnoreCase))
                        {
                            pluginList.Add(pluginName);
                            File.WriteAllLines(pluginsFile, pluginList);
                            Log($"Added .esp to plugins.txt: {pluginName}");
                        }
                    }
                    else if (ext == ".pak" && installedFiles.Count == 0)
                    {
                        string destDir = Path.Combine(txtInstallDir!.Text, "Content", "Paks", "~mods");
                        if (!Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        string destFile = Path.Combine(destDir, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                        installedFiles.Add(destFile);
                        Log($"Copied .pak to {destFile}");
                    }
                    else if ((ext == ".dll" || ext == ".exe") && installedFiles.Count == 0)
                    {
                        string destDir = Path.Combine(txtInstallDir!.Text, "Binaries", "Win64");
                        if (!Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        string destFile = Path.Combine(destDir, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                        installedFiles.Add(destFile);
                        Log($"Copied .dll/.exe to {destFile}");
                    }

                    if (pluginName != null && installedFiles.Count > 0)
                    {
                        if (!modFiles.ContainsKey(pluginName))
                        {
                            modFiles[pluginName] = [];
                        }
                        modFiles[pluginName].AddRange(installedFiles);
                    }

                    processedFiles++;
                    prgInstall!.Value = (int)(20 + (80.0 * processedFiles / totalFiles));
                    await Task.Delay(10);
                }

                SaveModFiles();
                UpdateModList();
                MessageBox.Show("Mod installed successfully. For mods requiring Vortex, consider using the Vortex mod manager.");
                Log("Mod installation completed successfully.");

                await RunBatchInstaller();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
                Log($"Installation failed: {ex.Message}");
            }
            finally
            {
                prgInstall!.Visible = false;
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                        Log($"Cleaned up temporary directory: {tempDir}");
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to clean up temporary directory {tempDir}: {ex.Message}");
                    }
                }
            }
        }

        private void BtnRemoveMod_Click(object? sender, EventArgs e)
        {
            if (lstInstalledMods?.SelectedItem == null)
            {
                MessageBox.Show("Please select a mod to remove.");
                Log("Mod removal failed: No mod selected.");
                return;
            }

            string selectedMod = lstInstalledMods.SelectedItem.ToString()!;
            string pluginName = selectedMod.Contains(" (") ? selectedMod[..selectedMod.IndexOf(" (")] : selectedMod;

            string dataDir = Path.Combine(txtInstallDir?.Text ?? string.Empty, "Content", "Dev", "ObvData", "Data");
            string pluginsFile = Path.Combine(dataDir, "plugins.txt");

            try
            {
                if (modFiles.ContainsKey(pluginName))
                {
                    foreach (var file in modFiles[pluginName])
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                            Log($"Deleted mod file: {file}");
                        }
                    }
                    modFiles.Remove(pluginName);
                    SaveModFiles();
                }

                if (File.Exists(pluginsFile))
                {
                    var pluginList = new List<string>(File.ReadAllLines(pluginsFile).Where(line => !string.IsNullOrWhiteSpace(line)));
                    pluginList = pluginList.Where(line => !line.Equals(pluginName, StringComparison.OrdinalIgnoreCase)).ToList();
                    File.WriteAllLines(pluginsFile, pluginList);
                    Log($"Removed {pluginName} from plugins.txt");
                }

                UpdateModList();
                MessageBox.Show($"Mod '{pluginName}' removed successfully.");
                Log($"Mod '{pluginName}' removed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while removing the mod: {ex.Message}");
                Log($"Mod removal failed: {ex.Message}");
            }
        }

        private void BtnRefreshModList_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtInstallDir?.Text))
            {
                MessageBox.Show("Please select an installation directory first.");
                Log("Mod list refresh failed: No installation directory selected.");
                return;
            }

            try
            {
                UpdateModList();
                MessageBox.Show("Mod list refreshed successfully.");
                Log("Mod list refreshed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while refreshing the mod list: {ex.Message}");
                Log($"Mod list refresh failed: {ex.Message}");
            }
        }

        private void UpdateModList()
        {
            if (lstInstalledMods == null || string.IsNullOrEmpty(txtInstallDir?.Text))
            {
                Log("UpdateModList skipped: No installation directory or mod list box.");
                return;
            }

            try
            {
                string dataDir = Path.Combine(txtInstallDir.Text, "Content", "Dev", "ObvData", "Data");
                string pluginsFile = Path.Combine(dataDir, "plugins.txt");
                string paksDir = Path.Combine(txtInstallDir.Text, "Content", "Paks", "~mods");
                string binariesDir = Path.Combine(txtInstallDir.Text, "Binaries", "Win64");

                var espFiles = Directory.Exists(dataDir)
                    ? Directory.EnumerateFiles(dataDir, "*.esp", SearchOption.TopDirectoryOnly)
                        .Select(Path.GetFileName)
                        .ToList()
                    : [];

                var pluginList = new List<string>();
                if (File.Exists(pluginsFile))
                {
                    pluginList.AddRange(File.ReadAllLines(pluginsFile).Where(line => !string.IsNullOrWhiteSpace(line)));
                }

                foreach (var esp in espFiles)
                {
                    if (esp != null && !pluginList.Contains(esp, StringComparer.OrdinalIgnoreCase))
                    {
                        pluginList.Add(esp);
                    }
                }

                pluginList = pluginList.Where(esp => espFiles.Contains(esp, StringComparer.OrdinalIgnoreCase)).ToList();

                if (pluginList.Count > 0 || File.Exists(pluginsFile))
                {
                    File.WriteAllLines(pluginsFile, pluginList);
                    Log("Updated plugins.txt with current .esp files.");
                }

                foreach (var esp in espFiles)
                {
                    if (esp != null && !modFiles.ContainsKey(esp))
                    {
                        modFiles[esp] = [Path.Combine(dataDir, esp)];
                    }
                }

                if (Directory.Exists(paksDir))
                {
                    var pakFiles = Directory.EnumerateFiles(paksDir, "*.pak", SearchOption.TopDirectoryOnly);
                    foreach (var esp in espFiles)
                    {
                        if (esp != null && modFiles.ContainsKey(esp))
                        {
                            modFiles[esp].AddRange(pakFiles.Where(f => !modFiles[esp].Contains(f)));
                        }
                    }
                }

                if (Directory.Exists(binariesDir))
                {
                    var binaryFiles = Directory.EnumerateFiles(binariesDir, "*.dll", SearchOption.TopDirectoryOnly)
                        .Concat(Directory.EnumerateFiles(binariesDir, "*.exe", SearchOption.TopDirectoryOnly));
                    foreach (var esp in espFiles)
                    {
                        if (esp != null && modFiles.ContainsKey(esp))
                        {
                            modFiles[esp].AddRange(binaryFiles.Where(f => !modFiles[esp].Contains(f)));
                        }
                    }
                }

                SaveModFiles();

                lstInstalledMods.Items.Clear();
                foreach (var plugin in pluginList)
                {
                    lstInstalledMods.Items.Add(plugin);
                }
                Log($"Mod list updated with {pluginList.Count} mods.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update mod list: {ex.Message}");
                Log($"UpdateModList failed: {ex.Message}");
            }
        }

        private void LoadModFiles()
        {
            if (string.IsNullOrEmpty(txtInstallDir?.Text))
            {
                Log("LoadModFiles skipped: No installation directory selected.");
                return;
            }

            string modFilesPath = Path.Combine(txtInstallDir.Text, "Content", "Dev", "ObvData", "Data", "mod_files.json");
            if (File.Exists(modFilesPath))
            {
                try
                {
                    string json = File.ReadAllText(modFilesPath);
                    modFiles = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? [];
                    Log($"Loaded mod files from {modFilesPath}");
                }
                catch (Exception ex)
                {
                    modFiles = [];
                    Log($"Failed to load mod files from {modFilesPath}: {ex.Message}");
                }
            }
            else
            {
                Log($"Mod files not found at {modFilesPath}, starting with empty mod list.");
            }
        }

        private void SaveModFiles()
        {
            if (string.IsNullOrEmpty(txtInstallDir?.Text))
            {
                Log("SaveModFiles skipped: No installation directory selected.");
                return;
            }

            string modFilesPath = Path.Combine(txtInstallDir.Text, "Content", "Dev", "ObvData", "Data", "mod_files.json");
            try
            {
                string json = JsonSerializer.Serialize(modFiles, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(modFilesPath, json);
                Log($"Saved mod files to {modFilesPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save mod tracking data: {ex.Message}");
                Log($"SaveModFiles failed: {ex.Message}");
            }
        }

        private void SaveConfig()
        {
            try
            {
                var config = new AppConfig { InstallDir = txtInstallDir?.Text };
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
                Log($"Saved configuration to {ConfigFilePath}");
            }
            catch (Exception ex)
            {
                Log($"Failed to save configuration to {ConfigFilePath}: {ex.Message}");
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    if (!string.IsNullOrEmpty(config?.InstallDir) && Directory.Exists(config.InstallDir))
                    {
                        txtInstallDir!.Text = config.InstallDir;
                        Log($"Loaded configuration from {ConfigFilePath}: InstallDir={config.InstallDir}");
                    }
                }
                else
                {
                    Log($"Configuration file {ConfigFilePath} not found.");
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to load configuration from {ConfigFilePath}: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";
                File.AppendAllText(LogFilePath, logEntry);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logging failed: {ex.Message}");
            }
        }

        private string? PromptForPassword()
        {
            using var form = new Form
            {
                Text = "Enter Password",
                Width = 300,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = IsSystemDarkTheme() ? Color.FromArgb(30, 30, 30) : SystemColors.Control
            };

            var label = new Label
            {
                Left = 20,
                Top = 20,
                Text = "Password:",
                ForeColor = IsSystemDarkTheme() ? Color.White : Color.Black
            };
            var textBox = new TextBox
            {
                Left = 20,
                Top = 40,
                Width = 240,
                PasswordChar = '*',
                BackColor = IsSystemDarkTheme() ? Color.FromArgb(45, 45, 45) : Color.White,
                ForeColor = IsSystemDarkTheme() ? Color.White : Color.Black
            };
            var okButton = new Button
            {
                Text = "OK",
                Left = 20,
                Top = 70,
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = IsSystemDarkTheme() ? Color.FromArgb(0, 120, 215) : SystemColors.Control,
                ForeColor = IsSystemDarkTheme() ? Color.White : Color.Black
            };
            var cancelButton = new Button
            {
                Text = "Cancel",
                Left = 100,
                Top = 70,
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                BackColor = IsSystemDarkTheme() ? Color.FromArgb(0, 120, 215) : SystemColors.Control,
                ForeColor = IsSystemDarkTheme() ? Color.White : Color.Black
            };

            form.Controls.Add(label);
            form.Controls.Add(textBox);
            form.Controls.Add(okButton);
            form.Controls.Add(cancelButton);
            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;

            if (form.ShowDialog() == DialogResult.OK)
            {
                Log("Password prompt: User entered password.");
                return textBox.Text;
            }

            Log("Password prompt: User canceled.");
            return null;
        }

        private int CompareVersions(string version1, string version2)
        {
            var v1Parts = version1.Split('.').Select(int.Parse).ToArray();
            var v2Parts = version2.Split('.').Select(int.Parse).ToArray();
            for (int i = 0; i < Math.Min(v1Parts.Length, v2Parts.Length); i++)
            {
                if (v1Parts[i] < v2Parts[i]) return -1;
                if (v1Parts[i] > v2Parts[i]) return 1;
            }
            return v1Parts.Length.CompareTo(v2Parts.Length);
        }

        private async Task RunBatchInstaller()
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = batchScriptPath,
                        WorkingDirectory = Path.GetDirectoryName(batchScriptPath) ?? string.Empty,
                        UseShellExecute = true
                    }
                };
                process.Start();
                await process.WaitForExitAsync();
                Log("Batch installer script completed.");
                UpdateModList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Batch Installer Error: {ex.Message}", "Error");
                Log($"Batch installer failed: {ex.Message}");
            }
        }
    }
}