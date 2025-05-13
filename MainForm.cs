using System;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace OblivionModInstaller
{
    public partial class MainForm : Form
    {
        // Designer-generated fields for UI components
        private TextBox txtInstallDir;
        private Button btnBrowseInstall;
        private TextBox txtModFile;
        private Button btnBrowseMod;
        private Button btnInstall;

        public MainForm()
        {
            InitializeComponent();
            SetupForm();
        }

        private void SetupForm()
        {
            // Form properties
            this.Text = "Oblivion Remastered Mod Installer";
            this.Width = 500;
            this.Height = 200;

            // Installation directory selection
            txtInstallDir = new TextBox { Left = 20, Top = 20, Width = 300, ReadOnly = true };
            btnBrowseInstall = new Button { Text = "Browse", Left = 330, Top = 20, Width = 100 };
            btnBrowseInstall.Click += BtnBrowseInstall_Click;

            // Mod file selection
            txtModFile = new TextBox { Left = 20, Top = 60, Width = 300, ReadOnly = true };
            btnBrowseMod = new Button { Text = "Browse", Left = 330, Top = 60, Width = 100 };
            btnBrowseMod.Click += BtnBrowseMod_Click;

            // Install button
            btnInstall = new Button { Text = "Install Mod", Left = 20, Top = 100, Width = 100 };
            btnInstall.Click += BtnInstall_Click;

            // Add controls to form
            this.Controls.Add(txtInstallDir);
            this.Controls.Add(btnBrowseInstall);
            this.Controls.Add(txtModFile);
            this.Controls.Add(btnBrowseMod);
            this.Controls.Add(btnInstall);
        }

        private void BtnBrowseInstall_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtInstallDir.Text = dialog.SelectedPath;
                }
            }
        }

        private void BtnBrowseMod_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Zip files (*.zip)|*.zip";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtModFile.Text = dialog.FileName;
                }
            }
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtInstallDir.Text) || string.IsNullOrEmpty(txtModFile.Text))
            {
                MessageBox.Show("Please select both the installation directory and the mod file.");
                return;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                // Extract the zip file to a temporary directory
                Directory.CreateDirectory(tempDir);
                ZipFile.ExtractToDirectory(txtModFile.Text, tempDir);

                // Scan for mod files
                var files = Directory.EnumerateFiles(tempDir, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();

                    if (ext == ".esp")
                    {
                        // Handle .esp files
                        string destDir = Path.Combine(txtInstallDir.Text, "Content", "Dev", "ObvData", "Data");
                        if (!Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        string destFile = Path.Combine(destDir, Path.GetFileName(file));
                        File.Copy(file, destFile, true);

                        // Update plugins.txt
                        string pluginsFile = Path.Combine(destDir, "plugins.txt");
                        string pluginName = Path.GetFileName(file);
                        if (File.Exists(pluginsFile))
                        {
                            string[] existingPlugins = File.ReadAllLines(pluginsFile);
                            if (!Array.Exists(existingPlugins, p => p.Trim() == pluginName))
                            {
                                File.AppendAllText(pluginsFile, pluginName + Environment.NewLine);
                            }
                        }
                        else
                        {
                            File.WriteAllText(pluginsFile, pluginName + Environment.NewLine);
                        }
                    }
                    else if (ext == ".pak")
                    {
                        // Handle .pak files
                        string destDir = Path.Combine(txtInstallDir.Text, "Content", "Paks", "~mods");
                        if (!Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        string destFile = Path.Combine(destDir, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                    }
                    else if (ext == ".dll" || ext == ".exe")
                    {
                        // Handle .dll and .exe files
                        string destDir = Path.Combine(txtInstallDir.Text, "Binaries", "Win64");
                        if (!Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        string destFile = Path.Combine(destDir, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                    }
                }
                MessageBox.Show("Mod installed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}