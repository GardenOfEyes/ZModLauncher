using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using static ZModLauncher.UIHelper;
using static ZModLauncher.GlobalStringConstants;

namespace ZModLauncher;

public class LauncherConfigManager
{
    private static readonly string _launcherConfigName = LauncherConfigName;
    public JObject LauncherConfig;

    public LauncherConfigManager()
    {
        Read();
    }

    private static void ShowLauncherConfigError()
    {
        ShowErrorDialog(LauncherConfigError);
        Environment.Exit(0);
    }

    public void Read()
    {
        var launcherAssembly = Assembly.GetExecutingAssembly();
        Stream stream = launcherAssembly.GetManifestResourceStream(_launcherConfigName);
        if (stream == null)
        {
            ShowLauncherConfigError();
            return;
        }
        var reader = new StreamReader(stream);
        try
        {
            LauncherConfig = JObject.Parse(reader.ReadToEnd());
        }
        catch
        {
            ShowLauncherConfigError();
        }
    }
}