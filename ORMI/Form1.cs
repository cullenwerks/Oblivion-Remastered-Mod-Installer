using System;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using Aspose.Zip;
using Aspose.Zip.SevenZip;
using Aspose.Zip.Rar;
using System.Collections.Generic;
using System.Text.Json;
using System.Drawing;
using System.Windows.Forms.VisualStyles;
using System.Threading.Tasks;
using Microsoft.Win32;
using FluentNexus; // Hypothetical Nexus Mods API client
using FuzzySharp; // For fuzzy matching mod names
using System.Net.Http;
using System.Linq;

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
        private float buttonScale = 1.0f;
        private const string ConfigFilePath = "ORMIConfig.json";
        private const string LogFilePath = "ORMI.log";
        private const string AppVersion = "1.0.0";

        // Nexus Mods API integration
        private Button? btnConnectNexus;
        private TextBox? txtApiKey;
        private Label? lblNexusStatus;
        private Button? btnCheckUpdates;
        private Button? btnUpdateMod;
        private string? nexusApiKey;
        private NexusClient? nexusClient;
        private Dictionary<string, string> modVersions = new Dictionary<string, string>(); // Local mod versions
        private Dictionary<string, NexusModInfo> nexusModInfo = new Dictionary<string, NexusModInfo>(); // Nexus mod info

        private class AppConfig
        {
            public string? InstallDir { get; set; }
            public string? NexusApiKey { get; set; }
        }

        private class NexusModInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public string DownloadUrl { get; set; } = string.Empty;
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

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
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
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    return value is int lightTheme && lightTheme == 0;
                }
            }
            catch (Exception ex)
            {
                Log($"Error checking system theme: {ex.Message}");
                return false;
            }
        }

        private void SetupForm()
        {
            this.Text = $"Oblivion Remastered Mod Installer v{AppVersion}";
            this.MinimumSize = new Size(600, 500);
            this.Size = new Size(600, 500);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.Font = new Font("Segoe UI", 9);

            // Add About menu
            var menuStrip = new MenuStrip();
            var helpMenu = new ToolStripMenuItem("Help");
            var aboutItem = new ToolStripMenuItem("About");
#pragma warning disable CS8622
            aboutItem.Click += (s, e) => MessageBox.Show($"Oblivion Remastered Mod Installer v{AppVersion}\nA tool to manage mods for Oblivion Remastered.\nBuilt with love by Grok.", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
#pragma warning restore CS8622
            helpMenu.DropDownItems.Add(aboutItem);
            menuStrip.Items.Add(helpMenu);
            this.Controls.Add(menuStrip);

            animationTimer = new System.Windows.Forms.Timer { Interval = 10 };
#pragma warning disable CS8622
            animationTimer.Tick += AnimationTimer_Tick;
#pragma warning restore CS8622

            toolTip = new ToolTip { AutoPopDelay = 10000, InitialDelay = 500, ShowAlways = true };
            toolTip.OwnerDraw = true;
            toolTip.Draw += (s, e) =>
            {
                e.DrawBackground();
                e.DrawBorder();
                using (var brush = IsSystemDarkTheme() ? Brushes.White : Brushes.Black)
                {
                    e.Graphics.DrawString(e.ToolTipText, new Font("Segoe UI", 8), brush, e.Bounds, StringFormat.GenericDefault);
                }
            };

            ctxMenuMods = new ContextMenuStrip();
#pragma warning disable CS8622
            ctxMenuMods.Items.Add("Remove Mod", null, BtnRemoveMod_Click);
            ctxMenuMods.Items.Add("Refresh List", null, BtnRefreshModList_Click);
#pragma warning restore CS8622

            var layoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 12, // Increased for Nexus Mods controls
                Padding = new Padding(10),
                AutoSize = true
            };
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 0: lblInstallDir
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 1: txtInstallDir, btnBrowseInstall
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 2: lblModFile
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 3: txtModFile, btnBrowseMod
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 4: Nexus Mods controls
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 5: chkCustomInstall, btnCustomHelp
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 6: lblCustomInstructions
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 7: txtCustomInstructions
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 8: lblInstalledMods
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Row 9: lstInstalledMods
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 10: buttonsPanel
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Row 11: prgInstall

            lblInstallDir = new Label { Text = "Oblivion Remastered Installation Directory:", AutoSize = true };
            layoutPanel.Controls.Add(lblInstallDir, 0, 0);

            txtInstallDir = new TextBox { ReadOnly = true, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseInstall = new Button { Text = "Browse", FlatStyle = FlatStyle.Flat, Size = new Size(100, 23) };
#pragma warning disable CS8622
            btnBrowseInstall.MouseDown += Button_MouseDown;
            btnBrowseInstall.MouseUp += Button_MouseUp;
            btnBrowseInstall.Click += BtnBrowseInstall_Click;
#pragma warning restore CS8622
            layoutPanel.Controls.Add(txtInstallDir, 0, 1);
            layoutPanel.Controls.Add(btnBrowseInstall, 1, 1);

            lblModFile = new Label { Text = "Mod Archive File:", AutoSize = true };
            layoutPanel.Controls.Add(lblModFile, 0, 2);

            txtModFile = new TextBox { ReadOnly = true, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnBrowseMod = new Button { Text = "Browse", FlatStyle = FlatStyle.Flat, Size = new Size(100, 23) };
#pragma warning disable CS8622
            btnBrowseMod.MouseDown += Button_MouseDown;
            btnBrowseMod.MouseUp += Button_MouseUp;
            btnBrowseMod.Click += BtnBrowseMod_Click;
#pragma warning restore CS8622
            layoutPanel.Controls.Add(txtModFile, 0, 3);
            layoutPanel.Controls.Add(btnBrowseMod, 1, 3);

            // Nexus Mods controls
            lblNexusStatus = new Label { Text = "Nexus Mods: Not Connected", AutoSize = true };
            layoutPanel.Controls.Add(lblNexusStatus, 0, 4);

            txtApiKey = new TextBox { PlaceholderText = "Enter Nexus Mods API Key", Anchor = AnchorStyles.Left | AnchorStyles.Right };
            btnConnectNexus = new Button { Text = "Connect", FlatStyle = FlatStyle.Flat, Size = new Size(100, 23) };
#pragma warning disable CS8622
            btnConnectNexus.MouseDown += Button_MouseDown;
            btnConnectNexus.MouseUp += Button_MouseUp;
            btnConnectNexus.Click += BtnConnectNexus_Click;
#pragma warning restore CS8622
            layoutPanel.Controls.Add(txtApiKey, 0, 4);
            layoutPanel.Controls.Add(btnConnectNexus, 1, 4);

            chkCustomInstall = new CheckBox { Text = "Enable Custom Installation", AutoSize = true };
            toolTip.SetToolTip(chkCustomInstall, "Check to specify custom file placements or instructions.\nExample: 'Place in Content/LogicMods' or 'Edit mods.txt'.\nSee the question mark for details or use Vortex for supported mods.");
            layoutPanel.Controls.Add(chkCustomInstall, 0, 5);

            btnCustomHelp = new Button
            {
                Text = "?",
                FlatStyle = FlatStyle.Flat,
                Size = new Size(23, 23),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            toolTip.SetToolTip(btnCustomHelp, "Click for help on custom installation.\nEnter instructions like 'Place in Content/LogicMods' or 'Edit mods.txt' in the text box below when enabled.");
#pragma warning disable CS8622
            btnCustomHelp.Click += BtnCustomHelp_Click;
            btnCustomHelp.MouseDown += Button_MouseDown;
            btnCustomHelp.MouseUp += Button_MouseUp;
#pragma warning restore CS8622
            layoutPanel.Controls.Add(btnCustomHelp, 2, 5);

            lblCustomInstructions = new Label { Text = "Custom Installation Instructions:", AutoSize = true };
            layoutPanel.Controls.Add(lblCustomInstructions, 0, 6);
            layoutPanel.SetColumnSpan(lblCustomInstructions, 3);

            txtCustomInstructions = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Height = 60,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Enabled = false
            };
#pragma warning disable CS8622
            chkCustomInstall.CheckedChanged += (s, e) => txtCustomInstructions.Enabled = chkCustomInstall!.Checked;
#pragma warning restore CS8622
            layoutPanel.Controls.Add(txtCustomInstructions, 0, 7);
            layoutPanel.SetColumnSpan(txtCustomInstructions, 3);

            lblInstalledMods = new Label { Text = "Installed Mods:", AutoSize = true };
            layoutPanel.Controls.Add(lblInstalledMods, 0, 8);
            layoutPanel.SetColumnSpan(lblInstalledMods, 3);

            lstInstalledMods = new ListBox
            {
                SelectionMode = SelectionMode.One,
                ContextMenuStrip = ctxMenuMods,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            layoutPanel.Controls.Add(lstInstalledMods, 0, 9);
            layoutPanel.SetColumnSpan(lstInstalledMods, 3);

            var buttonsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Anchor = AnchorStyles.Left
            };

            btnInstall = new Button { Text = "Install Mod", FlatStyle = FlatStyle.Flat, Size = new Size(100, 30) };
#pragma warning disable CS8622
            btnInstall.MouseDown += Button_MouseDown;
            btnInstall.MouseUp += Button_MouseUp;
            btnInstall.Click += BtnInstall_Click;
#pragma warning restore CS8622
            buttonsPanel.Controls.Add(btnInstall);

            btnRemoveMod = new Button { Text = "Remove Selected Mod", FlatStyle = FlatStyle.Flat, Size = new Size(150, 30) };
#pragma warning disable CS8622
            btnRemoveMod.MouseDown += Button_MouseDown;
            btnRemoveMod.MouseUp += Button_MouseUp;
            btnRemoveMod.Click += BtnRemoveMod_Click;
#pragma warning restore CS8622
            buttonsPanel.Controls.Add(btnRemoveMod);

            btnRefreshModList = new Button { Text = "Refresh Mod List", FlatStyle = FlatStyle.Flat, Size = new Size(150, 30) };
#pragma warning disable CS8622
            btnRefreshModList.MouseDown += Button_MouseDown;
            btnRefreshModList.MouseUp += Button_MouseUp;
            btnRefreshModList.Click += BtnRefreshModList_Click;
#pragma warning restore CS8622
            buttonsPanel.Controls.Add(btnRefreshModList);

            btnCheckUpdates = new Button { Text = "Check for Updates", FlatStyle = FlatStyle.Flat, Size = new Size(150, 30) };
#pragma warning disable CS8622
            btnCheckUpdates.MouseDown += Button_MouseDown;
            btnCheckUpdates.MouseUp += Button_MouseUp;
            btnCheckUpdates.Click += BtnCheckUpdates_Click;
#pragma warning restore CS8622
            buttonsPanel.Controls.Add(btnCheckUpdates);

            btnUpdateMod = new Button { Text = "Update Selected Mod", FlatStyle = FlatStyle.Flat, Size = new Size(150, 30), Enabled = false };
#pragma warning disable CS8622
            btnUpdateMod.MouseDown += Button_MouseDown;
            btnUpdateMod.MouseUp += Button_MouseUp;
            btnUpdateMod.Click += BtnUpdateMod_Click;
#pragma warning restore CS8622
            buttonsPanel.Controls.Add(btnUpdateMod);

            layoutPanel.Controls.Add(buttonsPanel, 0, 10);
            layoutPanel.SetColumnSpan(buttonsPanel, 3);

            prgInstall = new ProgressBar
            {
                Visible = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Maximum = 100,
                Minimum = 0,
                Value = 0
            };
            layoutPanel.Controls.Add(prgInstall, 0, 11);
            layoutPanel.SetColumnSpan(prgInstall, 3);

            this.Controls.Add(layoutPanel);
        }

        private async void BtnConnectNexus_Click(object sender, EventArgs e)
        {
            try
            {
                nexusApiKey = txtApiKey?.Text?.Trim();
                if (string.IsNullOrEmpty(nexusApiKey))
                {
                    MessageBox.Show("Please enter a valid Nexus Mods API key. Contact Nexus Mods support to obtain one.");
                    Log("Nexus Mods connection failed: No API key provided.");
                    return;
                }

                // Initialize NexusClient (hypothetical FluentNexus client)
                nexusClient = new NexusClient(nexusApiKey);
                // Test connection by fetching a simple public endpoint
                var games = await nexusClient.GetGamesAsync();
                if (games.Any(g => g.DomainName == "oblivionremastered"))
                {
                    lblNexusStatus!.Text = "Nexus Mods: Connected";
                    Log("Successfully connected to Nexus Mods API.");
                    SaveConfig();
                }
                else
                {
                    lblNexusStatus!.Text = "Nexus Mods: Connection Failed";
                    Log("Nexus Mods connection failed: Unable to fetch game data.");
                }
            }
            catch (Exception ex)
            {
                lblNexusStatus!.Text = "Nexus Mods: Connection Failed";
                Log($"Nexus Mods connection failed: {ex.Message}");
                MessageBox.Show("Failed to connect to Nexus Mods: " + ex.Message);
            }
        }

        private async void BtnCheckUpdates_Click(object sender, EventArgs e)
        {
            if (nexusClient == null || string.IsNullOrEmpty(nexusApiKey))
            {
                MessageBox.Show("Please connect to Nexus Mods first.");
                Log("Check for updates failed: Not connected to Nexus Mods.");
                return;
            }

            if (string.IsNullOrEmpty(txtInstallDir?.Text))
            {
                MessageBox.Show("Please select an installation directory first.");
                Log("Check for updates failed: No installation directory selected.");
                return;
            }

            try
            {
                prgInstall!.Visible = true;
                prgInstall.Value = 0;
                Log("Checking for mod updates...");

                // Get local mod names from plugins.txt
                string dataDir = Path.Combine(txtInstallDir.Text, "Content", "Dev", "ObvData", "Data");
                string pluginsFile = Path.Combine(dataDir, "plugins.txt");
                if (!File.Exists(pluginsFile))
                {
                    MessageBox.Show("No installed mods found in plugins.txt.");
                    Log("Check for updates failed: plugins.txt not found.");
                    return;
                }

                var localMods = File.ReadAllLines(pluginsFile)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();
                if (!localMods.Any())
                {
                    MessageBox.Show("No installed mods found to check for updates.");
                    Log("Check for updates: No mods found in plugins.txt.");
                    return;
                }

                // Query Nexus Mods for Oblivion Remastered mods
                var nexusMods = await nexusClient.GetModsAsync("oblivionremastered");
                if (nexusMods == null || !nexusMods.Any())
                {
                    MessageBox.Show("No mods found on Nexus Mods for Oblivion Remastered.");
                    Log("Check for updates failed: No mods found on Nexus Mods.");
                    return;
                }

                modVersions.Clear();
                nexusModInfo.Clear();
                lstInstalledMods!.Items.Clear();

                int totalMods = localMods.Count;
                int processedMods = 0;

                foreach (var localMod in localMods)
                {
                    // Assume local version is "1.0.0" if unknown (could enhance by storing version in mod_files.json)
                    modVersions[localMod] = "1.0.0";

                    // Find matching mod on Nexus Mods (fuzzy matching)
                    var matchedMod = nexusMods.OrderByDescending(m => Fuzz.Ratio(m.Name.ToLower(), localMod.ToLower()))
                        .FirstOrDefault(m => Fuzz.Ratio(m.Name.ToLower(), localMod.ToLower()) > 80);

                    if (matchedMod != null)
                    {
                        var latestFile = await nexusClient.GetModFilesAsync("oblivionremastered", matchedMod.Id);
                        if (latestFile != null && latestFile.Any())
                        {
                            var latestVersion = latestFile.OrderByDescending(f => f.Version).First().Version;
                            var downloadUrl = latestFile.OrderByDescending(f => f.Version).First().DownloadUrl;
                            nexusModInfo[localMod] = new NexusModInfo { Name = matchedMod.Name, Version = latestVersion, DownloadUrl = downloadUrl };

                            if (CompareVersions(modVersions[localMod], latestVersion) < 0)
                            {
                                lstInstalledMods.Items.Add($"{localMod} (Update available: {latestVersion})");
                            }
                            else
                            {
                                lstInstalledMods.Items.Add($"{localMod} (Up to date)");
                            }
                        }
                        else
                        {
                            lstInstalledMods.Items.Add($"{localMod} (No files found on Nexus Mods)");
                        }
                    }
                    else
                    {
                        lstInstalledMods.Items.Add($"{localMod} (Not found on Nexus Mods)");
                    }

                    processedMods++;
                    prgInstall.Value = (int)(100.0 * processedMods / totalMods);
                }

                btnUpdateMod!.Enabled = nexusModInfo.Any();
                MessageBox.Show("Update check completed.");
                Log("Mod update check completed.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to check for updates: " + ex.Message);
                Log($"Check for updates failed: {ex.Message}");
            }
            finally
            {
                prgInstall!.Visible = false;
            }
        }

        private async void BtnUpdateMod_Click(object sender, EventArgs e)
        {
            if (lstInstalledMods?.SelectedItem == null)
            {
                MessageBox.Show("Please select a mod to update.");
                Log("Mod update failed: No mod selected.");
                return;
            }

            string selectedMod = lstInstalledMods.SelectedItem.ToString();
            string modName = selectedMod.Contains(" (") ? selectedMod.Substring(0, selectedMod.IndexOf(" (")) : selectedMod;

            if (!nexusModInfo.ContainsKey(modName) || string.IsNullOrEmpty(nexusModInfo[modName].DownloadUrl))
            {
                MessageBox.Show("No update available for this mod.");
                Log($"Mod update failed for {modName}: No update available.");
                return;
            }

            try
            {
                prgInstall!.Visible = true;
                prgInstall.Value = 0;
                Log($"Updating mod {modName}...");

                // Download the new mod file
                string tempFile = Path.Combine(Path.GetTempPath(), $"{modName}_update{Path.GetExtension(nexusModInfo[modName].DownloadUrl)}");
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(nexusModInfo[modName].DownloadUrl);
                    response.EnsureSuccessStatusCode();
                    using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }
                Log($"Downloaded updated mod file for {modName} to {tempFile}");

                // Remove old mod files
                if (modFiles.ContainsKey(modName))
                {
                    foreach (var file in modFiles[modName])
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                            Log($"Deleted old mod file: {file}");
                        }
                    }
                    modFiles.Remove(modName);
                }

                // Install new mod file (simplified: assuming same structure as manual install)
                string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                string extension = Path.GetExtension(tempFile).ToLower();
                if (extension == ".zip")
                {
                    ZipFile.ExtractToDirectory(tempFile, tempDir);
                }
                else if (extension == ".7z")
                {
                    using (var archive = new SevenZipArchive(tempFile))
                    {
                        archive.ExtractToDirectory(tempDir);
                    }
                }
                else if (extension == ".rar")
                {
                    using (var archive = new RarArchive(tempFile))
                    {
                        archive.ExtractToDirectory(tempDir);
                    }
                }
                else
                {
                    throw new NotSupportedException("Unsupported archive format: " + extension);
                }

                var files = Directory.EnumerateFiles(tempDir, "*.*", SearchOption.AllDirectories);
                int totalFiles = files.Count();
                int processedFiles = 0;

                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();
                    List<string> installedFiles = new List<string>();

                    if (ext == ".esp")
                    {
                        string destDir = Path.Combine(txtInstallDir!.Text, "Content", "Dev", "ObvData", "Data");
                        if (!Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        string destFile = Path.Combine(destDir, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                        installedFiles.Add(destFile);

                        string pluginsFile = Path.Combine(destDir, "plugins.txt");
                        var pluginList = new List<string>();
                        if (File.Exists(pluginsFile))
                        {
                            pluginList.AddRange(File.ReadAllLines(pluginsFile).Where(line => !string.IsNullOrWhiteSpace(line)));
                        }
                        if (!pluginList.Contains(modName, StringComparer.OrdinalIgnoreCase))
                        {
                            pluginList.Add(modName);
                            File.WriteAllLines(pluginsFile, pluginList);
                            Log($"Added updated .esp to plugins.txt: {modName}");
                        }
                    }
                    else if (ext == ".pak")
                    {
                        string destDir = Path.Combine(txtInstallDir!.Text, "Content", "Paks", "~mods");
                        if (!Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        string destFile = Path.Combine(destDir, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                        installedFiles.Add(destFile);
                        Log($"Copied updated .pak to {destFile}");
                    }
                    else if (ext == ".dll" || ext == ".exe")
                    {
                        string destDir = Path.Combine(txtInstallDir!.Text, "Binaries", "Win64");
                        if (!Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        string destFile = Path.Combine(destDir, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                        installedFiles.Add(destFile);
                        Log($"Copied updated .dll/.exe to {destFile}");
                    }

                    if (installedFiles.Count > 0)
                    {
                        if (!modFiles.ContainsKey(modName))
                        {
                            modFiles[modName] = new List<string>();
                        }
                        modFiles[modName].AddRange(installedFiles);
                    }

                    processedFiles++;
                    prgInstall.Value = (int)(100.0 * processedFiles / totalFiles);
                }

                SaveModFiles();
                UpdateModList();
                MessageBox.Show($"Mod '{modName}' updated successfully.");
                Log($"Mod '{modName}' updated successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update mod: " + ex.Message);
                Log($"Mod update failed for {modName}: {ex.Message}");
            }
            finally
            {
                prgInstall!.Visible = false;
            }
        }

        private int CompareVersions(string version1, string version2)
        {
            // Simple semantic version comparison
            var v1Parts = version1.Split('.').Select(int.Parse).ToArray();
            var v2Parts = version2.Split('.').Select(int.Parse).ToArray();
            for (int i = 0; i < Math.Min(v1Parts.Length, v2Parts.Length); i++)
            {
                if (v1Parts[i] < v2Parts[i]) return -1;
                if (v1Parts[i] > v2Parts[i]) return 1;
            }
            return 0;
        }

        private void ApplyTheme()
        {
            bool isDark = IsSystemDarkTheme();
            if (isDark)
            {
                this.BackColor = Color.FromArgb(30, 30, 30);
                lblInstallDir!.ForeColor = Color.White;
                lblModFile!.ForeColor = Color.White;
                lblCustomInstructions!.ForeColor = Color.White;
                lblInstalledMods!.ForeColor = Color.White;
                lblNexusStatus!.ForeColor = Color.White;
                txtInstallDir!.BackColor = Color.FromArgb(45, 45, 45);
                txtInstallDir.ForeColor = Color.White;
                txtModFile!.BackColor = Color.FromArgb(45, 45, 45);
                txtModFile.ForeColor = Color.White;
                txtApiKey!.BackColor = Color.FromArgb(45, 45, 45);
                txtApiKey.ForeColor = Color.White;
                txtCustomInstructions!.BackColor = Color.FromArgb(45, 45, 45);
                txtCustomInstructions.ForeColor = Color.White;
                lstInstalledMods!.BackColor = Color.FromArgb(45, 45, 45);
                lstInstalledMods.ForeColor = Color.White;
                btnInstall!.BackColor = Color.FromArgb(0, 120, 215);
                btnInstall.ForeColor = Color.White;
                btnBrowseInstall!.BackColor = Color.FromArgb(0, 120, 215);
                btnBrowseInstall.ForeColor = Color.White;
                btnBrowseMod!.BackColor = Color.FromArgb(0, 120, 215);
                btnBrowseMod.ForeColor = Color.White;
                btnRemoveMod!.BackColor = Color.FromArgb(0, 120, 215);
                btnRemoveMod.ForeColor = Color.White;
                btnRefreshModList!.BackColor = Color.FromArgb(0, 120, 215);
                btnRefreshModList.ForeColor = Color.White;
                btnConnectNexus!.BackColor = Color.FromArgb(0, 120, 215);
                btnConnectNexus.ForeColor = Color.White;
                btnCheckUpdates!.BackColor = Color.FromArgb(0, 120, 215);
                btnCheckUpdates.ForeColor = Color.White;
                btnUpdateMod!.BackColor = Color.FromArgb(0, 120, 215);
                btnUpdateMod.ForeColor = Color.White;
                btnCustomHelp!.BackColor = Color.FromArgb(0, 120, 215);
                btnCustomHelp.ForeColor = Color.White;
                prgInstall!.BackColor = Color.FromArgb(45, 45, 45);
                prgInstall.ForeColor = Color.FromArgb(0, 120, 215);
            }
            else
            {
                this.BackColor = SystemColors.Control;
                lblInstallDir!.ForeColor = Color.Black;
                lblModFile!.ForeColor = Color.Black;
                lblCustomInstructions!.ForeColor = Color.Black;
                lblInstalledMods!.ForeColor = Color.Black;
                lblNexusStatus!.ForeColor = Color.Black;
                txtInstallDir!.BackColor = Color.White;
                txtInstallDir.ForeColor = Color.Black;
                txtModFile!.BackColor = Color.White;
                txtModFile.ForeColor = Color.Black;
                txtApiKey!.BackColor = Color.White;
                txtApiKey.ForeColor = Color.Black;
                txtCustomInstructions!.BackColor = Color.White;
                txtCustomInstructions.ForeColor = Color.Black;
                lstInstalledMods!.BackColor = Color.White;
                lstInstalledMods.ForeColor = Color.Black;
                btnInstall!.BackColor = SystemColors.Control;
                btnInstall.ForeColor = Color.Black;
                btnBrowseInstall!.BackColor = SystemColors.Control;
                btnBrowseInstall.ForeColor = Color.Black;
                btnBrowseMod!.BackColor = SystemColors.Control;
                btnBrowseMod.ForeColor = Color.Black;
                btnRemoveMod!.BackColor = SystemColors.Control;
                btnRemoveMod.ForeColor = Color.Black;
                btnRefreshModList!.BackColor = SystemColors.Control;
                btnRefreshModList.ForeColor = Color.Black;
                btnConnectNexus!.BackColor = SystemColors.Control;
                btnConnectNexus.ForeColor = Color.Black;
                btnCheckUpdates!.BackColor = SystemColors.Control;
                btnCheckUpdates.ForeColor = Color.Black;
                btnUpdateMod!.BackColor = SystemColors.Control;
                btnUpdateMod.ForeColor = Color.Black;
                btnCustomHelp!.BackColor = SystemColors.Control;
                btnCustomHelp.ForeColor = Color.Black;
                prgInstall!.BackColor = SystemColors.Control;
                prgInstall.ForeColor = SystemColors.Highlight;
            }
        }

        private void BtnCustomHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Custom Installation Help:\n\n" +
                "Use this feature for mods requiring non-standard file placements or configuration edits, as specified in their Nexus Mods descriptions.\n\n" +
                "1. Check 'Enable Custom Installation' to activate the text box.\n" +
                "2. Enter instructions in the text box, one per line:\n" +
                "   - File placement: Use 'Place in <path>' or 'Copy to <path>' (e.g., 'Place in Content/LogicMods').\n" +
                "   - Configuration edits: Use 'Edit <file>' (e.g., 'Edit Binaries/Win64/ue4ss/Mods/mods.txt to add MyMod').\n" +
                "3. The program will copy files to specified paths or prompt you to manually edit configuration files.\n\n" +
                "Examples:\n" +
                "- Place in Content/LogicMods\n" +
                "- Copy to Binaries/Win64/ue4ss/Mods\n" +
                "- Edit Binaries/Win64/ue4ss/Mods/mods.txt to add MyMod\n\n" +
                "For mods supported by Vortex, consider using the Vortex mod manager from Nexus Mods.",
                "Custom Installation Help",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void Button_MouseDown(object sender, MouseEventArgs e)
        {
            buttonScale = 0.95f;
            animationTimer?.Start();
        }

        private void Button_MouseUp(object sender, MouseEventArgs e)
        {
            buttonScale = 1.0f;
            animationTimer?.Stop();
            ((Control)sender).Invalidate();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            btnInstall?.Invalidate();
            btnBrowseInstall?.Invalidate();
            btnBrowseMod?.Invalidate();
            btnRemoveMod?.Invalidate();
            btnRefreshModList?.Invalidate();
            btnConnectNexus?.Invalidate();
            btnCheckUpdates?.Invalidate();
            btnUpdateMod?.Invalidate();
            btnCustomHelp?.Invalidate();
        }

        private void BtnBrowseInstall_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtInstallDir!.Text = dialog.SelectedPath;
                    SaveConfig();
                    UpdateModList();
                    Log($"Installation directory set to: {txtInstallDir.Text}");
                }
            }
        }

        private void BtnBrowseMod_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Archive files (*.zip;*.7z;*.rar)|*.zip;*.7z;*.rar";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtModFile!.Text = dialog.FileName;
                    Log($"Mod file selected: {txtModFile.Text}");
                }
            }
        }

        private async void BtnInstall_Click(object sender, EventArgs e)
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
                        using (var archive = new SevenZipArchive(txtModFile.Text))
                        {
                            archive.ExtractToDirectory(tempDir);
                        }
                        prgInstall.Value = 20;
                        Log("Extracted .7z archive successfully.");
                    }
                    catch (System.IO.InvalidDataException)
                    {
                        string? password = PromptForPassword();
                        if (string.IsNullOrEmpty(password))
                        {
                            throw new Exception("Password required for encrypted .7z archive.");
                        }
                        using (var archive = new SevenZipArchive(txtModFile.Text, password))
                        {
                            archive.ExtractToDirectory(tempDir);
                        }
                        prgInstall.Value = 20;
                        Log("Extracted password-protected .7z archive successfully.");
                    }
                }
                else if (extension == ".rar")
                {
                    try
                    {
                        using (var archive = new RarArchive(txtModFile.Text))
                        {
                            archive.ExtractToDirectory(tempDir);
                        }
                        prgInstall.Value = 20;
                        Log("Extracted .rar archive successfully.");
                    }
                    catch (System.IO.InvalidDataException)
                    {
                        string? password = PromptForPassword();
                        if (string.IsNullOrEmpty(password))
                        {
                            throw new Exception("Password required for encrypted .rar archive.");
                        }
                        using (var archive = new RarArchive(txtModFile.Text))
                        {
                            foreach (var entry in archive.Entries)
                            {
                                string entryPath = Path.Combine(tempDir, entry.Name.Replace('/', Path.DirectorySeparatorChar));
                                string entryDir = Path.GetDirectoryName(entryPath);
                                if (!string.IsNullOrEmpty(entryDir))
                                {
                                    Directory.CreateDirectory(entryDir);
                                }
                                using (var entryStream = entry.Open(password))
                                using (var fileStream = File.Create(entryPath))
                                {
                                    entryStream.CopyTo(fileStream);
                                }
                            }
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

                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();
                    string pluginName = null;
                    List<string> installedFiles = new List<string>();

                    if (chkCustomInstall?.Checked == true && !string.IsNullOrEmpty(txtCustomInstructions?.Text))
                    {
                        string[] instructions = txtCustomInstructions.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var instruction in instructions)
                        {
                            string trimmed = instruction.Trim();
                            if (trimmed.StartsWith("Place in ", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("Copy to ", StringComparison.OrdinalIgnoreCase))
                            {
                                string destPath = trimmed.Substring(trimmed.IndexOf(" ", StringComparison.OrdinalIgnoreCase) + 1).Trim();
                                string fullDestDir = Path.Combine(txtInstallDir.Text, destPath);
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
                                MessageBox.Show($"Please manually perform the following instruction: {trimmed}\nRefer to the mod's Nexus Mods description for details.", "Manual Configuration Required");
                                Log($"Prompted user for manual edit: {trimmed}");
                            }
                        }
                    }

                    if (ext == ".esp" && (pluginName == null || installedFiles.Count == 0))
                    {
                        string destDir = Path.Combine(txtInstallDir.Text, "Content", "Dev", "ObvData", "Data");
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
                        string destDir = Path.Combine(txtInstallDir.Text, "Content", "Paks", "~mods");
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
                        string destDir = Path.Combine(txtInstallDir.Text, "Binaries", "Win64");
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
                            modFiles[pluginName] = new List<string>();
                        }
                        modFiles[pluginName].AddRange(installedFiles);
                    }

                    processedFiles++;
                    prgInstall.Value = (int)(20 + (80.0 * processedFiles / totalFiles));
                    await Task.Delay(10);
                }

                SaveModFiles();
                UpdateModList();
                MessageBox.Show("Mod installed successfully. For mods requiring Vortex, consider using the Vortex mod manager from Nexus Mods.");
                Log("Mod installation completed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
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

        private void BtnRemoveMod_Click(object sender, EventArgs e)
        {
            if (lstInstalledMods?.SelectedItem == null)
            {
                MessageBox.Show("Please select a mod to remove.");
                Log("Mod removal failed: No mod selected.");
                return;
            }

            string selectedMod = lstInstalledMods.SelectedItem.ToString();
            string pluginName = selectedMod.Contains(" (") ? selectedMod.Substring(0, selectedMod.IndexOf(" (")) : selectedMod;

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
                MessageBox.Show("An error occurred while removing the mod: " + ex.Message);
                Log($"Mod removal failed: {ex.Message}");
            }
        }

        private void BtnRefreshModList_Click(object sender, EventArgs e)
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
                MessageBox.Show("An error occurred while refreshing the mod list: " + ex.Message);
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
                    : new List<string>();

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
                        modFiles[esp] = new List<string> { Path.Combine(dataDir, esp) };
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
                    if (nexusModInfo.ContainsKey(plugin) && CompareVersions(modVersions.GetValueOrDefault(plugin, "1.0.0"), nexusModInfo[plugin].Version) < 0)
                    {
                        lstInstalledMods.Items.Add($"{plugin} (Update available: {nexusModInfo[plugin].Version})");
                    }
                    else if (nexusModInfo.ContainsKey(plugin))
                    {
                        lstInstalledMods.Items.Add($"{plugin} (Up to date)");
                    }
                    else
                    {
                        lstInstalledMods.Items.Add(plugin);
                    }
                }
                Log($"Mod list updated with {pluginList.Count} mods.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update mod list: " + ex.Message);
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
                    modFiles = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? new Dictionary<string, List<string>>();
                    Log($"Loaded mod files from {modFilesPath}");
                }
                catch (Exception ex)
                {
                    modFiles = new Dictionary<string, List<string>>();
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
                MessageBox.Show("Failed to save mod tracking data: " + ex.Message);
                Log($"SaveModFiles failed: {ex.Message}");
            }
        }

        private void SaveConfig()
        {
            try
            {
                var config = new AppConfig { InstallDir = txtInstallDir?.Text, NexusApiKey = nexusApiKey };
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
                    if (!string.IsNullOrEmpty(config?.NexusApiKey))
                    {
                        txtApiKey!.Text = config.NexusApiKey;
                        BtnConnectNexus_Click(this, EventArgs.Empty);
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
            using (var form = new Form())
            {
                form.Text = "Enter Password";
                form.Width = 300;
                form.Height = 150;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.BackColor = IsSystemDarkTheme() ? Color.FromArgb(30, 30, 30) : SystemColors.Control;

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
        }
    }
}