using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using static ZModLauncher.GlobalStringConstants;
using static ZModLauncher.UIHelper;

namespace ZModLauncher;

public static class IOHelper
{
    public static async Task<bool> WriteStreamToFile(Stream stream, string filePath)
    {
        try
        {
            FileStream fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream);
            fileStream.Close();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void CreateZipFromFile(string filePath)
    {
        try
        {
            using var fileStream = new FileStream($"{filePath}.zip", FileMode.CreateNew);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true);
            archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
        }
        catch { }
    }

    public static void ExtractAndDeleteZip(string zipPath)
    {
        if (!File.Exists(zipPath)) return;
        try
        {
            using var fileStream = new FileStream(zipPath, FileMode.Open);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);
            archive.ExtractToDirectory(Path.GetDirectoryName(zipPath));
            fileStream.Close();
            File.Delete(zipPath);
        }
        catch { }
    }

    public static bool ExtractToDirectory(ZipArchive archive, string destinationDirectoryName, bool overwrite)
    {
        if (!overwrite)
        {
            archive.ExtractToDirectory(destinationDirectoryName);
            return true;
        }
        DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
        string destinationDirectoryFullPath = di.FullName;
        foreach (ZipArchiveEntry file in archive.Entries)
        {
            string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, file.FullName));
            if (file.Name == "")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(completeFileName)!);
                continue;
            }
            try
            {
                file.ExtractToFile(completeFileName, true);
            }
            catch (InvalidDataException)
            {
                ShowErrorDialog(ZipFormatError);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }
        return true;
    }
}