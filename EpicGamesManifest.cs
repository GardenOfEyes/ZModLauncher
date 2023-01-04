using System.IO;
using Newtonsoft.Json.Linq;
using static ZModLauncher.StringHelper;
using static ZModLauncher.GlobalStringConstants;

namespace ZModLauncher;

public class EpicGamesManifest : Manifest
{
    public override void ReadGame(Game game)
    {
        JObject manifest = JObject.Parse(File.ReadAllText(FilePath));
        var displayName = manifest[EpicGamesGameNameKey]?.ToString();
        if (!IsMatching(displayName, game.Name)) return;
        ManifestManager.ConfigureGameFromDatabase(game, manifest[EpicGamesInstallLocKey]?.ToString(), game.Name);
    }
}