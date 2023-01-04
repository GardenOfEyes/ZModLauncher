using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using static ZModLauncher.GlobalStringConstants;

namespace ZModLauncher;

public class ManifestManager
{
    private static string _fileExtension;
    private static string _folderPath;
    private static readonly DatabaseManager _gamesDbManager = new();

    public ManifestManager(DropboxFileManager fileManager)
    {
        _gamesDbManager.FileManager = fileManager;
    }

    public static bool ConfigureGameFromDatabase(Game game, string localPath, string manifestGameName)
    {
        if (_gamesDbManager.Database == null) return false;
        JToken gameEntry = _gamesDbManager.Database.GetValue(game.Name, StringComparison.OrdinalIgnoreCase);
        if (localPath.EndsWith("\\")) localPath = localPath.Substring(0, localPath.LastIndexOf('\\'));
        if (!Directory.Exists(localPath)) localPath = localPath.Replace(manifestGameName, manifestGameName.Replace(" ", ""));
        game.LocalPath = localPath;
        var execPath = gameEntry?[DatabaseExecutableKey]?.ToString();
        game.ExecutablePath = $"{game.LocalPath}\\{execPath}";
        game.IsInstalled = File.Exists(game.ExecutablePath);
        if (game.IsInstalled) game.SetVersionFromExecutable();
        return game.IsInstalled;
    }

    public void ReadManifestFiles<T>(Game game) where T : Manifest, new()
    {
        if (game.LocalPath != null) return;
        if (typeof(T) == typeof(SteamManifest))
        {
            _folderPath = $"{Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Valve\\Steam", "InstallPath", "")}\\steamapps";
            _fileExtension = "*.vdf";
        }
        else if (typeof(T) == typeof(EpicGamesManifest))
        {
            _folderPath = $"{Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Epic Games\\EpicGamesLauncher", "AppDataPath", "")}Manifests";
            _fileExtension = "*.item";
        }
        if (typeof(T) != typeof(NativeManifest))
        {
            string[] manifestFilePaths = null;
            try
            {
                manifestFilePaths = Directory.GetFiles(_folderPath, _fileExtension);
            }
            catch { }
            if (manifestFilePaths == null) return;
            foreach (string filePath in manifestFilePaths)
            {
                try
                {
                    new T { FilePath = filePath, GamesDatabase = _gamesDbManager.Database }.ReadGame(game);
                }
                catch { }
            }
        }
        else if (File.Exists(NativeManifest.FilePath))
            new NativeManifest { GamesDatabase = _gamesDbManager.Database }.ReadGame(game);
        else NativeManifest.WriteGame();
    }

    public async Task ReadAllManifests(Game game)
    {
        if (_gamesDbManager.Database == null) await _gamesDbManager.ReadDatabase(GamesDbFileName);
        ReadManifestFiles<NativeManifest>(game);
        ReadManifestFiles<SteamManifest>(game);
        ReadManifestFiles<EpicGamesManifest>(game);
    }
}