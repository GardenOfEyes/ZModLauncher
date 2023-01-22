using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Dropbox.Api.Files;
using ZModLauncher.Pages;
using static ZModLauncher.StringHelper;
using static ZModLauncher.UIHelper;
using static ZModLauncher.GlobalStringConstants;
using static ZModLauncher.IOHelper;

namespace ZModLauncher;

public class LauncherUpdater
{
    private static readonly Version _currentLauncherVersion = Version.Parse(new SignInPage().LauncherVersion);
    private static DropboxFileManager _fileManager;

    public async Task CheckForUpdates()
    {
        var configManager = new LauncherConfigManager();
        bool.TryParse(configManager.LauncherConfig[IsLauncherOfflineForMaintenanceKey]?.ToString(), out bool isOffline);
        if (isOffline)
        {
            ShowInformationDialog(LauncherMaintenanceMessage);
            Environment.Exit(0);
        }
        while (true)
        {
            var backupLauncherExecutableDir = $"{NativeManifest.AppRootPath}\\launcher_backup";
            try
            {
                if (Directory.Exists(backupLauncherExecutableDir)) Directory.Delete(backupLauncherExecutableDir, true);
            }
            catch { }
            _fileManager = new DropboxFileManager();
            await _fileManager.GetAllFilesAndFolders();
            if (_fileManager.Files == null)
            {
                if (ShowYesNoErrorDialog(InternetConnectionError) == MessageBoxResult.Yes) continue;
                return;
            }
            Metadata updateFile = _fileManager.Files.FirstOrDefault(i => i.Name.Contains(NativeManifest.ExecutableAppName) && i.Name.EndsWith(".exe"));
            if (updateFile == null) return;
            string[] nameTokens = AssertExtractPathTokens(updateFile.Name, 2, '_');
            if (nameTokens == null) return;
            Version.TryParse(Path.GetFileNameWithoutExtension(nameTokens[1]), out Version updateFileVersion);
            if (updateFileVersion == null) return;
            if (_currentLauncherVersion < updateFileVersion)
            {
                Stream stream = await (await _fileManager.DownloadFile(updateFile.PathDisplay)).GetContentAsStreamAsync();
                string launcherName = Assembly.GetExecutingAssembly().GetName().Name;
                var launcherExecutablePath = $"{NativeManifest.AppRootPath}\\{launcherName}.exe";
                var backupLauncherExecutablePath = $"{backupLauncherExecutableDir}\\{launcherName}.exe";
                if (!Directory.Exists(backupLauncherExecutableDir)) Directory.CreateDirectory(backupLauncherExecutableDir);
                try
                {
                    if (File.Exists(backupLauncherExecutablePath)) File.Delete(backupLauncherExecutablePath);
                }
                catch { }
                File.Move(launcherExecutablePath, $"{backupLauncherExecutableDir}\\{launcherName}.exe");
                await WriteStreamToFile(stream, launcherExecutablePath);
                Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
            break;
        }
    }
}