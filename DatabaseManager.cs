using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dropbox.Api.Files;
using Newtonsoft.Json.Linq;

namespace ZModLauncher;

public class DatabaseManager
{
    public JObject Database;
    public DropboxFileManager FileManager;

    public async Task ReadDatabase(string databaseName)
    {
        foreach (Metadata folder in FileManager.Files.Where(i => i.IsFolder))
        {
            List<Metadata> files = FileManager.GetFolderFiles(folder, 2, out _);
            Metadata database = files?.FirstOrDefault(i => i.Name == databaseName);
            if (database == null) return;
            try
            {
                Database = JObject.Parse(await (await FileManager.DownloadFile(database.PathDisplay)).GetContentAsStringAsync());
            }
            catch
            {
                break;
            }
        }
    }
}