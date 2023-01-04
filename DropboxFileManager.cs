using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Stone;
using static ZModLauncher.StringHelper;
using static ZModLauncher.GlobalStringConstants;

namespace ZModLauncher;

public class DropboxFileManager : OAuthConstants
{
    private static DropboxClient _client;
    public IList<Metadata> Files;

    public DropboxFileManager()
    {
        var configManager = new LauncherConfigManager();
        RefreshToken = configManager.LauncherConfig[DropboxRefreshTokenKey]?.ToString();
        ClientId = configManager.LauncherConfig[DropboxClientIdKey]?.ToString();
        ClientSecret = configManager.LauncherConfig[DropboxClientSecretKey]?.ToString();
        var httpClient = new HttpClient(new WebRequestHandler { ReadWriteTimeout = 10 * 1000 }) { Timeout = TimeSpan.FromMinutes(1000) };
        var clientConfig = new DropboxClientConfig($"{NativeManifest.ExecutableAppName}") { HttpClient = httpClient };
        _client = new DropboxClient(RefreshToken, ClientId, ClientSecret, clientConfig);
    }

    public List<Metadata> GetFolderFiles(Metadata folder, int wantedNumTokens, out string[] pathTokens)
    {
        pathTokens = AssertExtractPathTokens(folder.PathDisplay, wantedNumTokens);
        return pathTokens == null ? null : GetMatchingFiles(folder.Name, wantedNumTokens - 1);
    }

    public async Task GetAllFilesAndFolders()
    {
        try
        {
            await _client.RefreshAccessToken(null);
            Files = (await _client.Files.ListFolderAsync("", true)).Entries;
        }
        catch
        {
            Files = null;
        }
    }

    private List<Metadata> GetMatchingFiles(string token, int tokenIndex, string compareToken = "")
    {
        return Files.Where(i =>
        {
            string[] tokens = ExtractPathTokens(i.PathDisplay);
            bool isMatching = i.IsFile && IsMatching(tokens.ElementAtOrDefault(tokenIndex), token) && tokens.ElementAtOrDefault(tokenIndex + 1) == i.Name;
            if (compareToken != "") return isMatching && i.PathDisplay.Contains(compareToken);
            return isMatching;
        }).ToList();
    }

    public async Task GetItemThumbnailImages<T>(IEnumerable<T> items) where T : LibraryItem
    {
        List<T> filteredItems = items.Where(i => i.ImageUri != null).ToList();
        IEnumerable<ThumbnailArg> itemThumbnailArgs = filteredItems.Select(i =>
            new ThumbnailArg(i.ImageUri, ThumbnailFormat.Jpeg.Instance,
                ThumbnailSize.W640h480.Instance, ThumbnailMode.FitoneBestfit.Instance));
        IEnumerable<IEnumerable<ThumbnailArg>> thumbnailArgBatches = itemThumbnailArgs
            .Select((x, i) => new { Index = i, Value = x }).GroupBy(x => x.Index / 25)
            .Select(x => x.Select(v => v.Value));
        IList<GetThumbnailBatchResultEntry> itemThumbnails = new List<GetThumbnailBatchResultEntry>();
        foreach (IEnumerable<ThumbnailArg> thumbnailArgBatch in thumbnailArgBatches)
        {
            IList<GetThumbnailBatchResultEntry> currentBatchThumbnails = (await _client.Files.GetThumbnailBatchAsync(thumbnailArgBatch)).Entries;
            foreach (GetThumbnailBatchResultEntry thumbnail in currentBatchThumbnails) itemThumbnails.Add(thumbnail);
        }
        for (var i = 0; i < itemThumbnails.Count; ++i)
            filteredItems[i].SetImageFromStream(new MemoryStream(Convert.FromBase64String(itemThumbnails[i].AsSuccess.Value.Thumbnail)));
    }

    public async Task<IDownloadResponse<FileMetadata>> DownloadFile(string filePath)
    {
        try
        {
            return await _client.Files.DownloadAsync(filePath);
        }
        catch
        {
            return null;
        }
    }
}