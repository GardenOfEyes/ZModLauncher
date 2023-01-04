using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static ZModLauncher.GlobalStringConstants;

namespace ZModLauncher;

public class NativeManifest : Manifest
{
    public static readonly string ActualAppName = "ZModLauncher";
    public static readonly string ExecutableAppName = Assembly.GetExecutingAssembly().GetName().Name;
    public static readonly string AppRootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    public new static string FilePath = $"{AppRootPath}\\{ManifestFileName}";
    public static JObject Manifest;

    public NativeManifest()
    {
        ReadJSON();
    }

    private static JObject GetDefaultManifest()
    {
        var manifest = new JObject
        {
            { ManifestGamesKey, new JObject() },
            { ManifestModsKey, new JObject() }
        };
        return manifest;
    }

    public static void ReadJSON()
    {
        try
        {
            Manifest = JObject.Parse(File.ReadAllText(FilePath));
        }
        catch
        {
            Manifest = GetDefaultManifest();
            WriteJSON();
        }
    }

    private static void WriteJSON()
    {
        File.WriteAllText(FilePath, JsonConvert.SerializeObject(Manifest, Formatting.Indented));
    }

    public override void ReadGame(Game game)
    {
        if (Manifest[ManifestGamesKey]?[game.Name] == null) return;
        var localPath = Manifest[ManifestGamesKey]?[game.Name]![ManifestLocalPathKey]?.ToString();
        if (localPath != null) ManifestManager.ConfigureGameFromDatabase(game, localPath, game.Name);
    }

    public static bool HasRunIntegrityCheck(Game game)
    {
        if (Manifest[ManifestGamesKey]![game.Name] == null) return false;
        return Manifest[ManifestGamesKey]![game.Name]![ManifestHasRunIntegrityCheckKey] != null
            && bool.Parse(Manifest[ManifestGamesKey]![game.Name]![ManifestHasRunIntegrityCheckKey]?.ToString()!);
    }

    public static void WriteGame(Game game = null, string localPath = "", bool hasRunIntegrityCheck = false)
    {
        if (game != null && localPath != "")
        {
            var modEntry = new JObject
            {
                [ManifestLocalPathKey] = localPath,
                [ManifestHasRunIntegrityCheckKey] = hasRunIntegrityCheck
            };
            Manifest[ManifestGamesKey]![game.Name] = modEntry;
        }
        WriteJSON();
    }

    public static void WriteMod(Mod mod, bool forceOverwrite = true)
    {
        if (Manifest[ManifestModsKey]![mod.Name] != null && !forceOverwrite) return;
        var modEntry = new JObject();
        if (!mod.IsLaunchable) modEntry[ManifestStatusKey] = mod.IsEnabled;
        if (mod.Version != null) modEntry[ManifestVersionKey] = mod.Version.ToString();
        Manifest[ManifestModsKey]![mod.Name] = modEntry;
        WriteJSON();
    }

    public static void DeleteMod(LibraryItem item)
    {
        if (Manifest[ManifestModsKey]![item.Name] == null) return;
        ((JObject)Manifest.SelectToken(ManifestModsKey))?.Property(item.Name)?.Remove();
        WriteJSON();
    }
}