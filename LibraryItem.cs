using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace ZModLauncher;

public abstract class LibraryItem
{
    public string ExecutablePath;
    public string[] Files;
    public BitmapImage Image;
    public string ImageUri;
    public bool IsInstalled;
    public string LocalPath;
    public string Name;
    public Version Version;

    public void SetImageFromStream(Stream stream)
    {
        Image = new BitmapImage();
        Image.BeginInit();
        Image.StreamSource = stream;
        Image.CacheOption = BitmapCacheOption.None;
        Image.EndInit();
    }

    public void SetFiles()
    {
        Files ??= Directory.GetFiles(LocalPath, "*.*", SearchOption.AllDirectories);
    }
}