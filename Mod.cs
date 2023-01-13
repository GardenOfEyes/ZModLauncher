using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using static ZModLauncher.IOHelper;
using static ZModLauncher.CommandLineHelper;
using static ZModLauncher.StringHelper;
using static ZModLauncher.GlobalStringConstants;

namespace ZModLauncher;

public class Mod : LibraryItem
{
    public Uri DirectDownloadUri;
    public Game Game;
    public string GameName;
    public int InstalledUpdates;
    public bool IsBusy;
    public bool IsEnabled;
    public bool IsExtracted;
    public bool IsExtracting;
    public bool IsLaunchable;
    public bool IsQueuing;
    public bool IsReconnecting;
    public bool IsToggling;
    public bool IsUpdated = true;
    public bool IsUsingSharedToggleMacro = true;
    public bool IsWaiting;
    public Uri ModInfoUri;
    public string NativeToggleMacroPath;
    public int Progress;
    public List<string> UpdateFiles = new();
    public string Uri;

    private void UpdateEnabledStatus()
    {
        JToken modStatus = NativeManifest.Manifest[ManifestModsKey]?[Name]?[ManifestStatusKey];
        if (modStatus != null) IsEnabled = bool.Parse(modStatus.ToString());
    }

    public Version GetBaseModFileVersion(string baseModFileName)
    {
        string[] tokens = AssertExtractPathTokens(baseModFileName, 2, '_');
        if (tokens == null) return null;
        Version.TryParse(Path.GetFileNameWithoutExtension(tokens[1]), out Version version);
        return version;
    }

    public Version[] GetUpdateFileVersionInfo(string updateFileName)
    {
        var info = new List<Version>();
        string[] tokens = AssertExtractPathTokens(updateFileName, 4, '_');
        if (tokens == null) return null;
        Version.TryParse(tokens[1], out Version gameVersion);
        Version.TryParse(Path.GetFileNameWithoutExtension(tokens[3]), out Version modVersion);
        if (gameVersion == null || modVersion == null) return null;
        info.AddRange(new[] { gameVersion, modVersion });
        return info.ToArray();
    }

    private string GetModManifestVersion()
    {
        return NativeManifest.Manifest[ManifestModsKey]?[Name]?[ManifestVersionKey]?.ToString();
    }

    public string GetModFileDirWithoutVersion(string modFileName)
    {
        return modFileName.Substring(0, modFileName.IndexOf('_'));
    }

    public void SetModVersion(string baseModFileName)
    {
        Version baseModFileVersion = GetBaseModFileVersion(baseModFileName);
        if (baseModFileVersion == null) return;
        string manifestVersion = GetModManifestVersion();
        Version = manifestVersion == null ? baseModFileVersion : Version.Parse(manifestVersion);
    }

    public List<string> FilterValidUpdateFiles(List<string> updateFiles)
    {
        for (int i = UpdateFiles.Count - 1; i >= 0; --i)
        {
            string updateFilePath = UpdateFiles[i];
            Version[] updateFileInfo = GetUpdateFileVersionInfo(updateFilePath);
            if (Game.Version < updateFileInfo[0] || Version >= updateFileInfo[1] && !File.Exists($"{LocalPath}\\{Path.GetFileName(updateFilePath)}"))
                updateFiles.RemoveAt(i);
        }
        return updateFiles;
    }

    public void CheckForUpdates()
    {
        List<string> filteredUpdateFiles = FilterValidUpdateFiles(UpdateFiles.ToList());
        IsUpdated = filteredUpdateFiles.Count == 0 || Version == null;
    }

    public void Configure(Game game)
    {
        Game = game;
        IsInstalled = Directory.Exists(LocalPath);
        if (!IsInstalled) return;
        NativeManifest.WriteMod(this, false);
        CheckForUpdates();
        if (IsLaunchable) return;
        Game.SetFiles();
        SetFiles();
        UpdateEnabledStatus();
    }

    private string GetFilePathRelativeToGame(string modFilePath)
    {
        return $"{Game.LocalPath}\\{modFilePath.Replace(LocalPath, "")}";
    }

    private void LaunchToggleMacro(string workingDir, string fileName)
    {
        string modFolderName = Path.GetFileNameWithoutExtension(LocalPath);
        LaunchExecutable(workingDir, fileName, $"\"{Game.LocalPath}\" \"{modFolderName}\"", true);
    }

    private void LaunchSharedToggleMacro()
    {
        LaunchToggleMacro(Game.LocalPath, Game.SharedToggleMacroPath);
    }

    private void LaunchNativeToggleMacro()
    {
        LaunchToggleMacro(LocalPath, NativeToggleMacroPath);
    }

    public void Toggle()
    {
        IsEnabled = !IsEnabled;
        NativeManifest.WriteMod(this);
        foreach (string modFilePath in Files)
        {
            string relativeGameModFilePath = GetFilePathRelativeToGame(modFilePath);
            string relativeGameModFileDir = Path.GetDirectoryName(relativeGameModFilePath);
            if (File.Exists(relativeGameModFilePath))
            {
                var modFileZipPath = $"{relativeGameModFilePath}.zip";
                switch (IsEnabled)
                {
                    case true:
                        CreateZipFromFile(relativeGameModFilePath);
                        break;
                    case false:
                        try
                        {
                            File.Delete(relativeGameModFilePath);
                        }
                        catch { }
                        ExtractAndDeleteZip(modFileZipPath);
                        break;
                }
            }
            if (!IsEnabled) continue;
            if (relativeGameModFileDir != null && !Directory.Exists(relativeGameModFileDir))
                Directory.CreateDirectory(relativeGameModFileDir);
            try
            {
                File.Copy(modFilePath, relativeGameModFilePath, true);
            }
            catch { }
        }
        if (Game.SharedToggleMacroPath != null && IsUsingSharedToggleMacro) LaunchSharedToggleMacro();
        if (NativeToggleMacroPath != null) LaunchNativeToggleMacro();
    }
}