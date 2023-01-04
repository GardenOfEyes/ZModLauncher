using System;
using System.IO;
using System.Linq;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Newtonsoft.Json.Linq;
using static ZModLauncher.StringHelper;
using static ZModLauncher.GlobalStringConstants;

namespace ZModLauncher;

public class SteamManifest : Manifest
{
    private static VToken GetChildValue(VToken token, int index)
    {
        return ((VProperty)((VProperty)token).Value.Children().ElementAt(index)).Value;
    }

    public override void ReadGame(Game game)
    {
        VProperty baseManifest = VdfConvert.Deserialize(File.ReadAllText(FilePath));
        foreach (VToken libraryFolder in baseManifest.Value.Children())
        {
            try
            {
                var folderPath = $"{GetChildValue(libraryFolder, 0)}\\steamapps";
                string[] manifestFilePaths = Directory.GetFiles(folderPath, "*.acf");
                foreach (string filePath in manifestFilePaths)
                {
                    VProperty manifest = VdfConvert.Deserialize(File.ReadAllText(filePath));
                    var name = GetChildValue(manifest, 3).ToString();
                    if (!IsMatching(name, game.Name)) continue;
                    if (GamesDatabase == null) return;
                    JToken gameEntry = GamesDatabase.GetValue(game.Name, StringComparison.OrdinalIgnoreCase);
                    ManifestManager.ConfigureGameFromDatabase(game, $"{folderPath}\\common\\{name}\\{gameEntry?[GamesDatabaseLocalPathKey]}", game.Name);
                    return;
                }
            }
            catch { }
        }
    }
}