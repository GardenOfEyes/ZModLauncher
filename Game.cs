using System;
using System.Diagnostics;

namespace ZModLauncher;

public class Game : LibraryItem
{
    public bool HasRunIntegrityCheck = true;
    public string IntegrityCheckerUri;
    public string SharedToggleMacroOnlineHash;
    public string SharedToggleMacroPath;
    public string SharedToggleMacroUri;

    public void SetVersionFromExecutable()
    {
        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(ExecutablePath);
        var version = new Version(versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart, versionInfo.FilePrivatePart);
        Version = version;
    }
}