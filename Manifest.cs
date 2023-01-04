using Newtonsoft.Json.Linq;

namespace ZModLauncher;

public abstract class Manifest
{
    public string FilePath;
    public JObject GamesDatabase;

    public abstract void ReadGame(Game game);
}