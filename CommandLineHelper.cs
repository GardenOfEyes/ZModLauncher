using System.Diagnostics;
using static ZModLauncher.UIHelper;
using static ZModLauncher.GlobalStringConstants;

namespace ZModLauncher;

public static class CommandLineHelper
{
    public static void LaunchExecutable(string workingDir, string fileName, string args = "", bool isSilent = false)
    {
        var process = new Process();
        var processInfo = new ProcessStartInfo
        {
            UseShellExecute = isSilent,
            WorkingDirectory = workingDir,
            FileName = fileName,
            Arguments = args,
            WindowStyle = isSilent ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
        };
        process.StartInfo = processInfo;
        try
        {
            process.Start();
            process.WaitForExit();
        }
        catch
        {
            ShowErrorDialog(ModExecutableError);
        }
    }
}