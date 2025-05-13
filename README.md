# Oblivion Remastered Mod Installer (ORMI)

Welcome to the **Oblivion Remastered Mod Installer (ORMI)**, a powerful and user-friendly tool designed to simplify mod management for *The Elder Scrolls IV: Oblivion Remastered*. Whether you're a seasoned modder or new to the Tamriel modding scene, ORMI makes installing, managing, and removing mods a breeze—no Nexus API required! Built with love for the Reddit modding community, ORMI is ready for you to test and enhance your Oblivion experience.

# DISCLAIMER
This is very Alpha at the moment, it's lacking features mentioned from Reddit and those will be added in over the next week. Please backup your Oblivion Installation if you'd like to test this for me. Tons of things subject to change including the UI and additonal OS's to boot this program from. -CPG

## 🚀 Features

ORMI is packed with features to streamline your modding workflow:

- **Local Mod Installation**:
  - Install mods from `.zip`, `.7z`, or `.rar` archives with a single click.
  - Supports password-protected archives with a user-friendly password prompt.
  - Automatically places mod files (`.esp`, `.pak`, `.dll`, `.exe`) in the correct directories:
    - `.esp` files → `Content/Dev/ObvData/Data`
    - `.pak` files → `Content/Paks/~mods`
    - `.dll`/`.exe` files → `Binaries/Win64`

- **Custom Installation Instructions**:
  - Enable custom file placements or configuration edits via a dedicated text box.
  - Use simple instructions like:
    - `Place in Content/LogicMods`
    - `Copy to Binaries/Win64/ue4ss/Mods`
    - `Edit Binaries/Win64/ue4ss/Mods/mods.txt to add MyMod`
  - ORMI copies files to specified paths or prompts for manual edits, perfect for complex mods.

- **Mod Management**:
  - View and manage installed mods in a clean list, updated from `plugins.txt`.
  - Remove mods with one click, deleting associated files and updating `plugins.txt`.
  - Refresh the mod list to reflect changes in the game directory.

- **Batch Installer Support**:
  - Run a custom batch script (`Oblivion Remastered Mod Installer.bat`) after installation to handle additional setup tasks.
  - Configurable via `batchScriptPath` in the code.

- **Dynamic Theme Support**:
  - Automatically adapts to your system’s light or dark theme for a seamless UI experience.
  - Modern, intuitive interface with tooltips and a help menu for guidance.

- **Robust Logging**:
  - Logs all actions (installs, removals, errors) to `ORMI.log` for easy debugging.
  - Timestamped entries help track modding activities.

- **Configuration Persistence**:
  - Saves your installation directory to `ORMIConfig.json` for quick reuse.
  - Loads settings on startup to minimize setup time.

## 📋 Requirements

To run ORMI, you’ll need:
- **Operating System**: Windows (tested on Windows 10/11)
- **.NET Runtime**: .NET 8.0 or later ([Download](https://dotnet.microsoft.com/en-us/download/dotnet/8.0))
- **Oblivion Remastered**: Installed with a valid game directory
- **Mod Archives**: `.zip`, `.7z`, or `.rar` files containing `.esp`, `.pak`, `.dll`, or `.exe` mod files
- **Optional**: A batch script (`Oblivion Remastered Mod Installer.bat`) for post-install tasks, placed one directory above the ORMI executable

## 🛠️ Installation

1. **Download ORMI**:
   - Grab the latest release from the [Releases](https://github.com/cullenwerks/Oblivion-Remastered-Mod-Installer/releases) page or clone this repository:
     ```bash
     git clone https://github.com/yourusername/ORMI.git
     ```
   - (Replace `yourusername` with your GitHub username or fork URL.)

2. **Install Dependencies**:
   - Ensure .NET 8.0 is installed (see Requirements).
   - ORMI uses `Aspose.Zip` for archive handling, included via NuGet. Restore dependencies:
     ```powershell
     cd ORMI
     dotnet restore
     ```

3. **Build ORMI**:
   - Build the project using .NET CLI:
     ```powershell
     dotnet build
     ```
   - Alternatively, open `ORMIInstaller.sln` in Visual Studio and build (Debug/Release).

4. **Run ORMI**:
   - Run the executable:
     ```powershell
     dotnet run
     ```
   - Or launch `ORMI.exe` from the build output (`bin/Debug/net8.0-windows/`).

## 🎮 Usage

1. **Launch ORMI**:
   - Start the application. The UI adapts to your system’s theme (light/dark).

2. **Select Game Directory**:
   - Click **Browse** next to "Oblivion Remastered Installation Directory" to select your game folder (e.g., `C:\Games\Oblivion Remastered`).
   - ORMI saves this path for future sessions.

3. **Install a Mod**:
   - Click **Browse** next to "Mod Archive File" to select a `.zip`, `.7z`, or `.rar` mod archive.
   - For custom installations:
     - Check **Enable Custom Installation**.
     - Enter instructions in the text box (e.g., `Place in Content/LogicMods`).
     - Click the **?** button for help.
   - Click **Install Mod** to extract and place files. Enter a password if prompted for protected archives.

4. **Manage Mods**:
   - View installed mods in the **Installed Mods** list.
   - Select a mod and click **Remove Selected Mod** to delete its files and update `plugins.txt`.
   - Click **Refresh Mod List** to sync the list with the game directory.

5. **Run Batch Script**:
   - ORMI runs `Oblivion Remastered Mod Installer.bat` (if present) after each installation to handle additional setup.

6. **Check Logs**:
   - Open `ORMI.log` in the ORMI directory to review actions or troubleshoot issues.

## 🐛 Troubleshooting

- **Build Errors**:
  - Ensure .NET 8.0 is installed and dependencies are restored (`dotnet restore`).
  - Verify `Aspose.Zip` and `System.Text.Json` are listed in `dotnet list package`.

- **Mod Installation Fails**:
  - Check that the game directory and mod archive paths are valid.
  - For password-protected archives, ensure the correct password is entered.
  - Review `ORMI.log` for error details.

- **UI Issues**:
  - If controls don’t render, ensure `SetupForm` is correctly initializing the UI.
  - Report issues to the [Issues]((https://github.com/cullenwerks/Oblivion-Remastered-Mod-Installer/issues)) page.

- **Batch Script Errors**:
  - Ensure `Oblivion Remastered Mod Installer.bat` exists one directory above ORMI’s executable.
  - Verify the script’s permissions and content.

## 🤝 Contributing

We ❤️ contributions from the Reddit modding community! To contribute:
1. Fork the repository.
2. Create a branch: `git checkout -b feature/your-feature`.
3. Commit changes: `git commit -m "Add your feature"`.
4. Push to your fork: `git push origin feature/your-feature`.
5. Open a Pull Request on GitHub.

Ideas for contributions:
- Enhance the UI with Visual Studio’s Forms Designer.
- Add support for additional archive formats.
- Improve error handling or logging.
- Share modding tips for Oblivion Remastered!

## 📣 Community Testing

Hey Redditors! We’re excited to have thousands of you testing ORMI. Here’s how you can help:
- **Test Mod Installation**: Try installing various `.esp`, `.pak`, and `.dll` mods, with and without custom instructions.
- **Report Bugs**: Post issues to the [Issues](https://github.com/cullenwerks/Oblivion-Remastered-Mod-Installer/issues) page or comment on our Reddit thread.
- **Share Feedback**: Let us know what works, what doesn’t, and what features you’d love to see (e.g., mod load order management).
- **Showcase Mods**: Share screenshots or videos of your modded Oblivion setups using ORMI!

Join the discussion on [r/oblivionmods](https://www.reddit.com/r/oblivionmods/) or our dedicated thread (link TBD).

## 📜 License

ORMI is licensed under the [MIT License](LICENSE). Feel free to use, modify, and share, but please give credit to the original project.

## 🙌 Acknowledgments

- Built for the passionate Oblivion modding community on Reddit.
- Powered by .NET 8.0, Aspose.Zip, and the love for Tamriel.
- Special thanks to all testers and contributors!

---

Happy modding, and may your adventures in Oblivion Remastered be legendary! 🗡️✨
