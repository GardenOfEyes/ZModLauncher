using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Dropbox.Api.Files;
using Dropbox.Api.Stone;
using Newtonsoft.Json.Linq;
using ZModLauncher.CustomControls;
using ZModLauncher.Pages;
using static ZModLauncher.StringHelper;
using static ZModLauncher.AnimationHelper;
using static ZModLauncher.UIHelper;
using static ZModLauncher.CommandLineHelper;
using static ZModLauncher.IOHelper;
using static ZModLauncher.GlobalStringConstants;
using Application = System.Windows.Application;

namespace ZModLauncher;

public class LibraryManager
{
    private static ManifestManager _manifestManager;
    private static Storyboard _refreshButtonAnim;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly DatabaseManager _modsDbManager = new();
    private static readonly DropboxFileManager _fileManager = new();
    private readonly StorePage _storePage;
    private BitmapImage _defaultCardImage;
    public Game FocusedGame;

    public LibraryManager(StorePage storePage)
    {
        _manifestManager = new ManifestManager(_fileManager);
        _storePage = storePage;
        SetupAllResources();
    }

    private void SetupAllResources()
    {
        _defaultCardImage = LocateResource<BitmapImage>(Application.Current, DefaultCardImageKey);
        _refreshButtonAnim = LocateResource<Storyboard>(_storePage, RefreshButtonAnimKey);
        ApplyStoryboardAnim(_refreshButtonAnim, _storePage.refreshButton);
    }

    private static T LocateResource<T>(dynamic container, string key)
    {
        return (T)container.FindResource(key);
    }

    private void ClearLibrary(bool showEmptyLibraryMessage)
    {
        _storePage.library.Children.Clear();
        if (showEmptyLibraryMessage) Show(_storePage.emptyLibraryMessage);
        else Collapse(_storePage.emptyLibraryMessage);
        Collapse(_storePage.loadLibraryProgressBar);
    }

    private static async Task RunBackgroundAction(Action action)
    {
        await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, action);
    }

    private async Task<LibraryItemCard> UpdateItemCardProgress(LibraryItemCard card, LibraryItem item)
    {
        await RunBackgroundAction(() => { card = RefreshItemCard(card, item); });
        return card;
    }

    public async Task InstallLocalMod()
    {
        var dialog = new OpenFileDialog { Title = InstallLocalModPrompt, Filter = OpenZipFileFilter };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        var mod = new Mod { LocalPath = $"{Directory.GetParent(dialog.FileName)}\\{Path.GetFileNameWithoutExtension(dialog.FileName)}", IsLaunchable = true };
        await EnterLoadingLibraryState();
        Show(_storePage.installingLocalModMessage);
        await ExtractModAndUpdateCardStatus(new LibraryItemCard(), mod, dialog.FileName);
        ExitLoadingLibraryState();
        Collapse(_storePage.installingLocalModMessage);
        await RefreshLibrary();
        ShowInformationDialog(InstallLocalModSuccessMessage);
    }

    private async Task<LibraryItemCard> ExtractModAndUpdateCardStatus(LibraryItemCard card, Mod mod, string modFileZipPath)
    {
        await Task.Run(async () =>
        {
            using (ZipArchive archive = ZipFile.OpenRead(modFileZipPath))
            {
                while (!mod.IsExtracted)
                {
                    mod.IsExtracting = true;
                    mod.IsWaiting = false;
                    await RunBackgroundAction(() => card = RefreshItemCard(card, mod));
                    mod.IsExtracted = ExtractToDirectory(archive, mod.IsLaunchable ? FocusedGame.LocalPath : mod.LocalPath, true);
                    if (mod.IsExtracted) continue;
                    mod.IsWaiting = true;
                    await RunBackgroundAction(() => card = RefreshItemCard(card, mod));
                    await Task.Delay(4000);
                }
                mod.IsExtracted = false;
            }
            if (File.Exists(modFileZipPath) && modFileZipPath.Contains(FocusedGame.LocalPath)) File.Delete(modFileZipPath);
            mod.IsExtracting = false;
            return card;
        });
        return card;
    }

    private async Task<LibraryItemCard> DownloadModFileAndUpdateCardStatus(LibraryItemCard card, Mod mod, string uri, string localPath)
    {
        mod.IsBusy = true;
        await Task.Run(async () =>
        {
            while (mod.IsBusy)
            {
                mod.IsQueuing = true;
                card = await UpdateItemCardProgress(card, mod);
                mod.IsQueuing = false;
                IDownloadResponse<FileMetadata> response = await _fileManager.DownloadFile(uri);
                if (response == null)
                {
                    mod.IsReconnecting = true;
                    card = await UpdateItemCardProgress(card, mod);
                    mod.IsReconnecting = false;
                    await Task.Delay(4000);
                    continue;
                }
                Stream stream = await response.GetContentAsStreamAsync();
                var modFileZipPath = $"{localPath}.zip";
                FileStream fileStream;
                try
                {
                    fileStream = File.OpenWrite(modFileZipPath);
                }
                catch
                {
                    ShowErrorDialog(ZipAccessError);
                    continue;
                }
                var prevProgress = 0;
                int streamBufferLength;
                var resumeTask = false;
                do
                {
                    if (mod.IsCancellingDownload || !IsCurrentPage(MainPageName) && !IsCurrentPage(SettingsPageName))
                    {
                        fileStream.Close();
                        stream.Close();
                        if (!mod.IsCancellingDownload) return card;
                        if (File.Exists(modFileZipPath)) File.Delete(modFileZipPath);
                        mod.IsBusy = false;
                        await RunBackgroundAction(() => card = RefreshItemCard(card, mod));
                        mod.IsCancellingDownload = false;
                        return card;
                    }
                    var streamBuffer = new byte[1024];
                    try
                    {
                        streamBufferLength = await stream.ReadAsync(streamBuffer, 0, 1024);
                    }
                    catch
                    {
                        fileStream.Close();
                        resumeTask = true;
                        break;
                    }
                    try
                    {
                        fileStream.Write(streamBuffer, 0, streamBufferLength);
                    }
                    catch
                    {
                        ShowErrorDialog(NotEnoughSpaceError);
                        return card;
                    }
                    mod.Progress = (int)((double)fileStream.Length / response.Response.Size * 100);
                    if (mod.Progress == prevProgress) continue;
                    prevProgress = mod.Progress;
                    if (!mod.IsOptionsButtonActivated) card = await UpdateItemCardProgress(card, mod);
                } while (stream.CanRead && streamBufferLength > 0);
                if (resumeTask) continue;
                fileStream.Close();
                card = await ExtractModAndUpdateCardStatus(card, mod, modFileZipPath);
                await RunBackgroundAction(() => card = RefreshItemCard(card, mod));
                mod.IsBusy = false;
                if (mod.IsUpdated) mod.Configure(FocusedGame);
            }
            return card;
        });
        return card;
    }

    private static void AssertActionButtonsVisibility(LibraryItemCard card, LibraryItem item)
    {
        if (item is not Mod mod) return;
        card.TrafficLightVisibility = mod.IsInstalled && mod.IsUpdated && !mod.IsLaunchable && !mod.IsBusy ? Visibility.Visible : Visibility.Collapsed;
        card.TrafficLightColor = mod.IsEnabled ? Brushes.LimeGreen : Brushes.Red;
    }

    private static void UpdateItemCardStatus(LibraryItemCard card, LibraryItem item)
    {
        AssertActionButtonsVisibility(card, item);
        card.Status = item switch
        {
            Mod mod => mod.IsReconnecting ? ReconnectingStatus :
                mod.IsQueuing ? QueuingStatus :
                mod.IsBusy ? mod.IsWaiting ? WaitingStatus :
                mod.IsExtracting ? InstallingStatus :
                !mod.IsUpdated ? $"{UpdatingStatus} ({mod.InstalledUpdates}/{mod.UpdateFiles.Count}) ({mod.Progress}%)" : $"{DownloadingStatus} ({mod.Progress}%)" :
                mod.IsInstalled ? !mod.IsUpdated ? UpdateStatus :
                mod.IsLaunchable ? LaunchNowStatus :
                mod.IsToggling ? TogglingStatus :
                mod.IsEnabled ? EnabledStatus : DisabledStatus :
                mod.Uri == null ? ComingSoonStatus : DownloadStatus,
            Game game => game.IsInstalled ? PlayNowStatus : NotInstalledStatus,
            _ => card.Status
        };
    }

    private bool IsLibraryEmpty()
    {
        return _storePage.library.Children.Count == 0;
    }

    private IEnumerable<LibraryItemCard> GetAllItemCards()
    {
        return _storePage.library.Children.OfType<LibraryItemCard>();
    }

    private void VerifyVisibleItemCardsLibraryState()
    {
        if (GetAllItemCards().All(i => i.Visibility == Visibility.Collapsed)
            && _storePage.loadLibraryProgressBar.Visibility == Visibility.Collapsed)
            Show(_storePage.emptyLibraryMessage);
    }

    private bool DoesItemCardMatchSearch(LibraryItemCard card)
    {
        string[] tokens = _storePage.searchBox.textBox.Text.ToLower().Split(' ');
        var isMatch = false;
        foreach (string token in tokens)
            if (card.Title.ToLower().Contains(token)
                && (tokens.Length == 1 || token.Length > 1))
                isMatch = true;
        return isMatch;
    }

    private bool DoesItemCardMatchGeneralFilter(LibraryItemCard card)
    {
        return _storePage.filterBox.SelectedIndex == 0
            || _storePage.filterBox.SelectedIndex == 1
            && card.Status is LaunchNowStatus or EnabledStatus or DisabledStatus or PlayNowStatus
            || _storePage.filterBox.SelectedIndex == 2
            && card.Status is DisabledStatus
            || _storePage.filterBox.SelectedIndex == 3
            && card.Status is EnabledStatus
            || _storePage.filterBox.SelectedIndex == 4
            && card.Status is LaunchNowStatus;
    }

    private bool DoesItemCardMatchFilters(LibraryItemCard card)
    {
        return DoesItemCardMatchSearch(card) && DoesItemCardMatchGeneralFilter(card);
    }

    public void FilterLibrary()
    {
        Collapse(_storePage.emptyLibraryMessage);
        foreach (LibraryItemCard card in _storePage.library.Children)
        {
            Collapse(card);
            if (DoesItemCardMatchFilters(card)) Show(card);
        }
        VerifyVisibleItemCardsLibraryState();
    }

    public void SortLibrary()
    {
        IEnumerable<LibraryItemCard> cards = GetAllItemCards().ToList();
        if (!cards.Any()) return;
        cards = _storePage.sortbyBox.SelectedIndex switch
        {
            0 => cards.OrderBy(x => x.Title).ToList(),
            1 => cards.OrderByDescending(x => x.Title).ToList(),
            _ => cards
        };
        ClearLibrary(false);
        foreach (LibraryItemCard card in cards)
            _storePage.library.Children.Add(card);
        VerifyVisibleItemCardsLibraryState();
    }

    private void ReplaceItemCardWith(UIElement card, UIElement newCard)
    {
        try
        {
            int originalCardIndex = _storePage.library.Children.IndexOf(card);
            _storePage.library.Children.RemoveAt(originalCardIndex);
            _storePage.library.Children.Insert(originalCardIndex, newCard);
            SortLibrary();
            FilterLibrary();
        }
        catch (ArgumentOutOfRangeException) { }
    }

    private LibraryItemCard RefreshItemCard(LibraryItemCard card, LibraryItem item)
    {
        if (IsLibraryEmpty()) return card;
        LibraryItemCard newCard = card.Clone();
        UpdateItemCardStatus(newCard, item);
        if (item is Game or Mod { IsReconnecting: false, IsBusy: false, IsToggling: false })
            AddItemCardClickEvent(newCard, item);
        if (item is Mod { IsBusy: false, IsQueuing: false, IsExtracting: false, IsToggling: false }) AddItemCardOptionsButtonEvents(newCard, item);
        if (item is Mod { IsBusy: true, IsUpdated: true, IsQueuing: false, IsExtracting: false, IsToggling: false }) AddItemCardOptionsButtonEvents(newCard, item);
        newCard = SetItemCardImageAndAddToLibrary(newCard, item);
        ReplaceItemCardWith(card, newCard);
        return newCard;
    }

    private LibraryItemCard UpdateModCardToggleStatus(LibraryItemCard card, Mod mod)
    {
        mod.IsToggling = !mod.IsToggling;
        UpdateItemCardStatus(card, mod);
        card = RefreshItemCard(card, mod);
        return card;
    }

    private async Task<LibraryItemCard> ToggleModAndUpdateCardStatus(LibraryItemCard card, Mod mod)
    {
        card = UpdateModCardToggleStatus(card, mod);
        await Task.Run(mod.Toggle);
        card = UpdateModCardToggleStatus(card, mod);
        return card;
    }

    private void DisableNavigationControls()
    {
        Disable(_storePage.backButton);
        Disable(_storePage.refreshButton);
        Disable(_storePage.installLocalModButton);
    }

    private void EnableNavigationControls()
    {
        Enable(_storePage.backButton);
        Enable(_storePage.refreshButton);
        Enable(_storePage.installLocalModButton);
    }

    private LibraryItemCard PostInstallOrUpdateCleanup(LibraryItemCard card, Mod mod)
    {
        bool isOtherModBusy = GetAllItemCards().Any(i => (i.Status.Contains(DownloadingStatus)
                || i.Status.Contains(UpdatingStatus))
            && i.Title != card.Title);
        if (!isOtherModBusy) EnableNavigationControls();
        if (!mod.IsUpdated)
        {
            NativeManifest.WriteMod(mod);
            mod.CheckForUpdates();
        }
        card = RefreshItemCard(card, mod);
        return card;
    }

    private async Task<LibraryItemCard> InstallOrUpdateMod(LibraryItemCard card, Mod mod)
    {
        LibraryItemCard progressCard = card;
        DisableNavigationControls();
        if (!mod.IsUpdated)
        {
            if (!mod.IsLaunchable && mod.IsEnabled) mod.Toggle();
            LibraryItemCard originalCard = card.Clone();
            mod.UpdateFiles = mod.FilterValidUpdateFiles(mod.UpdateFiles);
            foreach (string updateFilePath in mod.UpdateFiles)
            {
                var localPath = $"{FocusedGame.LocalPath}\\{Path.GetFileNameWithoutExtension(updateFilePath)}";
                mod.InstalledUpdates = mod.UpdateFiles.IndexOf(updateFilePath) + 1;
                card = await DownloadModFileAndUpdateCardStatus(progressCard, mod, updateFilePath, localPath);
                if (Directory.Exists(localPath)) Directory.Delete(localPath, true);
                mod.Version = mod.GetUpdateFileVersionInfo(updateFilePath)[1];
                ReplaceItemCardWith(card, originalCard);
                card = originalCard;
                progressCard = card;
            }
        }
        else
        {
            if (mod.Version != null)
            {
                Version baseModVersion = mod.GetBaseModFileVersion(mod.Uri);
                if (baseModVersion != null) mod.Version = baseModVersion;
                NativeManifest.DeleteMod(mod);
            }
            var localPath = $"{FocusedGame.LocalPath}\\{Path.GetFileNameWithoutExtension(mod.LocalPath)}";
            card = await DownloadModFileAndUpdateCardStatus(progressCard, mod, mod.Uri, localPath);
        }
        card = PostInstallOrUpdateCleanup(card, mod);
        return card;
    }

    private async Task<LibraryItemCard> AddItemCardModClickEvent(LibraryItemCard card, Mod mod)
    {
        if (mod.Uri == null || card.OptionsButton.IsMouseOver) return card;
        if (mod.IsUpdated && mod.IsInstalled)
        {
            if (mod.IsLaunchable) LaunchExecutable(Path.GetDirectoryName(mod.ExecutablePath), mod.ExecutablePath);
            else card = await ToggleModAndUpdateCardStatus(card, mod);
            return card;
        }
        card = await InstallOrUpdateMod(card, mod);
        return card;
    }

    private static async Task<bool> DownloadGameResource(string destinationPath, string uri)
    {
        IDownloadResponse<FileMetadata> resourceFile = await _fileManager.DownloadFile(uri);
        if (resourceFile == null) return false;
        Stream stream = await resourceFile.GetContentAsStreamAsync();
        if (File.Exists(destinationPath)) File.Delete(destinationPath);
        return await WriteStreamToFile(stream, destinationPath);
    }

    private static async Task DownloadSharedToggleMacro(Game game)
    {
        if (game.SharedToggleMacroUri == null) return;
        game.SharedToggleMacroPath = $"{game.LocalPath}\\{Path.GetFileName(game.SharedToggleMacroUri)}";
        if (File.Exists(game.SharedToggleMacroPath) && game.SharedToggleMacroOnlineHash == GetFileHash(game.SharedToggleMacroPath)) return;
        if (!await DownloadGameResource(game.SharedToggleMacroPath, game.SharedToggleMacroUri)) game.SharedToggleMacroPath = null;
    }

    private static async Task RunIntegrityCheck(Game game)
    {
        if (game.IntegrityCheckerUri == null || NativeManifest.HasRunIntegrityCheck(game)) return;
        game.HasRunIntegrityCheck = false;
        var integrityCheckerPath = $"{game.LocalPath}\\{Path.GetFileName(game.IntegrityCheckerUri)}";
        if (!await DownloadGameResource(integrityCheckerPath, game.IntegrityCheckerUri)) return;
        LaunchExecutable(game.LocalPath, integrityCheckerPath, $"\"{game.LocalPath}\"", true);
        if (File.Exists(integrityCheckerPath)) File.Delete(integrityCheckerPath);
        NativeManifest.WriteGame(game, game.LocalPath, true);
        game.HasRunIntegrityCheck = true;
    }

    private async Task<LibraryItemCard> AddItemCardGameClickEvent(LibraryItemCard card, Game game)
    {
        if (game.IsInstalled)
        {
            FocusedGame = game;
            await LoadLibrary<Mod>(game, true, true);
        }
        else
        {
            _manifestManager.ReadManifestFiles<NativeManifest>(game);
            if (game.LocalPath == null || !File.Exists(game.ExecutablePath))
            {
                var dialog = new FolderBrowserDialog();
                dialog.Description = GameInstallFolderPrompt;
                if (dialog.ShowDialog() != DialogResult.OK) return card;
                if (!ManifestManager.ConfigureGameFromDatabase(game, dialog.SelectedPath, game.Name))
                {
                    ShowErrorDialog(GameExecutableError);
                    return card;
                }
                NativeManifest.WriteGame(game, dialog.SelectedPath);
            }
            card = RefreshItemCard(card, game);
            return card;
        }
        return card;
    }

    private static async Task AssertModCardOptionsButtonLoadedState(LibraryItemCard card)
    {
        while (card.OptionsButton == null) await Task.Delay(1000);
    }

    private static async Task SetModCardOptionsButtonItemVisibilityStates(LibraryItemCard card, Mod mod)
    {
        await AssertModCardOptionsButtonLoadedState(card);
        card.OptionsButton.Visibility = Visibility.Visible;
        var deleteButton = (ComboBoxItem)card.OptionsButton.Items.GetItemAt(0);
        var modInfoButton = (ComboBoxItem)card.OptionsButton.Items.GetItemAt(1);
        var directDownloadButton = (ComboBoxItem)card.OptionsButton.Items.GetItemAt(2);
        var cancelDownloadButton = (ComboBoxItem)card.OptionsButton.Items.GetItemAt(3);
        var versionInfo = (ComboBoxItem)card.OptionsButton.Items.GetItemAt(4);
        versionInfo.IsEnabled = false;
        deleteButton.Visibility = mod.IsInstalled ? Visibility.Visible : Visibility.Collapsed;
        modInfoButton.Visibility = mod.ModInfoUri != null ? Visibility.Visible : Visibility.Collapsed;
        versionInfo.Visibility = mod.IsInstalled && mod.Version != null ? Visibility.Visible : Visibility.Collapsed;
        versionInfo.Content = $"Version {mod.Version}";
        directDownloadButton.Visibility = !mod.IsInstalled && mod.DirectDownloadUri != null ? Visibility.Visible : Visibility.Collapsed;
        cancelDownloadButton.Visibility = !mod.IsInstalled && mod.IsBusy ? Visibility.Visible : Visibility.Collapsed;
        if (card.OptionsButton.Items.Cast<ComboBoxItem>().All(i => i.Visibility != Visibility.Visible))
            card.OptionsButton.Visibility = Visibility.Collapsed;
    }

    private static async Task ResetModCardOptionsButtonSelection(LibraryItemCard card)
    {
        await AssertModCardOptionsButtonLoadedState(card);
        card.OptionsButton.SelectedItem = null;
    }

    private void AddItemCardClickEvent(LibraryItemCard card, LibraryItem item)
    {
        card.PreviewMouseDown += async (_, _) =>
        {
            card = item switch
            {
                Mod mod => await AddItemCardModClickEvent(card, mod),
                Game game => await AddItemCardGameClickEvent(card, game),
                _ => card
            };
        };
    }

    private void AddItemCardOptionsButtonEvents(LibraryItemCard card, LibraryItem item)
    {
        card.Loaded += async (_, _) =>
        {
            if (item is not Mod mod) return;
            await SetModCardOptionsButtonItemVisibilityStates(card, mod);
            card.OptionsButton.DropDownOpened += (_, _) => { mod.IsOptionsButtonActivated = true; };
            card.OptionsButton.DropDownClosed += (_, _) => { mod.IsOptionsButtonActivated = false; };
            card.OptionsButton.SelectionChanged += async (_, _) =>
            {
                switch (card.OptionsButton.SelectedIndex)
                {
                    case 0:
                    {
                        MessageBoxResult shouldDelete = ShowQuestionDialog(DeleteModConfirmation);
                        if (shouldDelete != MessageBoxResult.Yes)
                        {
                            await ResetModCardOptionsButtonSelection(card);
                            return;
                        }
                        if (!mod.IsLaunchable && mod.IsEnabled) mod.Toggle();
                        try
                        {
                            Directory.Delete(mod.LocalPath, true);
                        }
                        catch { }
                        mod.IsUpdated = true;
                        mod.Configure(FocusedGame);
                        card = RefreshItemCard(card, mod);
                        break;
                    }
                    case 1:
                        Process.Start(mod.ModInfoUri.AbsoluteUri);
                        break;
                    case 2:
                        Process.Start(mod.DirectDownloadUri.AbsoluteUri);
                        break;
                    case 3:
                        mod.IsCancellingDownload = true;
                        break;
                }
                await ResetModCardOptionsButtonSelection(card);
            };
        };
    }

    private LibraryItemCard SetItemCardImageAndAddToLibrary(LibraryItemCard card, LibraryItem item)
    {
        item.Image ??= _defaultCardImage;
        if (item.Image == _defaultCardImage || item is Mod { Uri: null } or Game { IsInstalled: false })
        {
            var convertedBitmap = new FormatConvertedBitmap();
            convertedBitmap.BeginInit();
            convertedBitmap.Source = item.Image;
            convertedBitmap.DestinationFormat = PixelFormats.Gray8;
            convertedBitmap.EndInit();
            card.ImageSource = convertedBitmap;
        }
        else card.ImageSource = item.Image;
        if (item is Mod { IsUpdated: false }) card.ImageOpacity = 0.40;
        else card.ImageOpacity = 1;
        return card;
    }

    private void AddItemToLibrary(LibraryItem item)
    {
        var card = new LibraryItemCard { Title = item.Name };
        if (IsLibraryEmpty()) Collapse(_storePage.emptyLibraryMessage);
        UpdateItemCardStatus(card, item);
        AddItemCardClickEvent(card, item);
        AddItemCardOptionsButtonEvents(card, item);
        card = SetItemCardImageAndAddToLibrary(card, item);
        _storePage.library.Children.Add(card);
    }

    private static bool IsItemTypeMod<T>()
    {
        return typeof(T) == typeof(Mod);
    }

    private static async Task<T> CreateItemFromDropboxFolder<T>(Metadata folder) where T : LibraryItem, new()
    {
        int numTokens = IsItemTypeMod<T>() ? 4 : 3;
        List<Metadata> itemFiles = _fileManager.GetFolderFiles(folder, numTokens, out string[] pathTokens);
        if (itemFiles == null) return null;
        var item = new T
        {
            Name = pathTokens[numTokens - 1],
            ImageUri = itemFiles.FirstOrDefault(i => IsFileAnImage(i.Name))?.PathDisplay
        };
        if (IsItemTypeMod<T>())
        {
            var mod = item as Mod;
            mod!.GameName = pathTokens[numTokens - 2];
            List<Metadata> zipFiles = itemFiles.Where(i => i.Name.EndsWith(".zip")).ToList();
            foreach (Metadata zipFile in zipFiles)
            {
                if (mod.GetUpdateFileVersionInfo(zipFile.Name) == null)
                {
                    mod.SetModVersion(zipFile.Name);
                    mod.Uri = zipFile.PathDisplay;
                }
                else mod.UpdateFiles.Add(zipFile.PathDisplay);
            }
            mod.UpdateFiles.Sort();
            if (_modsDbManager.Database == null || mod.Uri == null) return (T)(object)mod;
            JToken modEntry = _modsDbManager.Database.GetValue(mod.Name, StringComparison.OrdinalIgnoreCase);
            Uri modInfoUri = GetAbsoluteUri(modEntry?[ModsDatabaseModInfoUriKey]?.ToString());
            if (modInfoUri != null) mod.ModInfoUri = modInfoUri;
            Uri directDownloadUri = GetAbsoluteUri(modEntry?[ModsDatabaseDirectDownloadUriKey]?.ToString());
            if (directDownloadUri != null) mod.DirectDownloadUri = directDownloadUri;
            mod.NativeToggleMacroPath = modEntry?[ModsDatabaseNativeMacroKey]?.ToString();
            var execPath = modEntry?[DatabaseExecutableKey]?.ToString();
            if (execPath == null) return (T)(object)mod;
            mod.ExecutablePath = execPath;
            mod.IsLaunchable = true;
            var isUsingSharedToggleMacro = modEntry[ModsDatabaseSharedMacroKey]?.ToString();
            if (isUsingSharedToggleMacro == null) return (T)(object)mod;
            mod.IsUsingSharedToggleMacro = bool.Parse(isUsingSharedToggleMacro);
            mod.IsLaunchable = !mod.IsUsingSharedToggleMacro;
            return (T)(object)mod;
        }
        var game = item as Game;
        await _manifestManager.ReadAllManifests(game);
        if (game == null) return item;
        foreach (Metadata metadata in itemFiles.Where(itemFile => itemFile.Name.EndsWith(".exe")))
        {
            var fileMetadata = (FileMetadata)metadata;
            if (fileMetadata.Name.ToLower().Contains("integritychecker")) game.IntegrityCheckerUri = fileMetadata.PathDisplay;
            else
            {
                game.SharedToggleMacroOnlineHash = fileMetadata.ContentHash;
                game.SharedToggleMacroUri = fileMetadata.PathDisplay;
            }
        }
        return item;
    }

    private void ClearSearch()
    {
        _storePage.searchBox.textBox.Text = "";
    }

    private async Task EnterLoadingLibraryState()
    {
        await _semaphore.WaitAsync();
        ClearSearch();
        ClearLibrary(false);
        Play(_refreshButtonAnim);
        Show(_storePage.loadLibraryProgressBar);
    }

    private void ExitLoadingLibraryState()
    {
        Stop(_refreshButtonAnim);
        Collapse(_storePage.loadLibraryProgressBar);
        _semaphore.Release();
    }

    private static bool IsCurrentlyLoadingLibrary()
    {
        return _semaphore.CurrentCount == 0;
    }

    private IEnumerable<T> GetMatchingItems<T>(LibraryItem item, IEnumerable<T> items) where T : LibraryItem
    {
        var filteredItems = new List<T>();
        foreach (T currentItem in items)
        {
            switch (currentItem)
            {
                case Mod mod when item.Name == "" || IsMatching(mod.GameName, item.Name):
                    if (mod.Uri != null)
                    {
                        string modFileDir = Path.GetFileNameWithoutExtension(mod.Uri);
                        if (mod.Version != null) mod.GetModFileDirWithoutVersion(modFileDir);
                        string execDir = Path.GetDirectoryName(mod.ExecutablePath);
                        mod.ExecutablePath = mod.IsLaunchable ? $"{FocusedGame.LocalPath}\\{mod.ExecutablePath}" : $"{mod.LocalPath}\\{mod.ExecutablePath}";
                        mod.LocalPath = mod.IsLaunchable ? $"{item.LocalPath}\\{execDir}" : $"{item.LocalPath}\\LauncherMods\\{execDir}";
                        if (mod.NativeToggleMacroPath != null) mod.NativeToggleMacroPath = $"{mod.LocalPath}\\{mod.NativeToggleMacroPath}";
                        mod.Configure(FocusedGame);
                    }
                    filteredItems.Add(currentItem);
                    break;
                case Game:
                    filteredItems.Add(currentItem);
                    break;
            }
        }
        return filteredItems;
    }

    private bool IsCurrentSectionTitle(string title)
    {
        return IsMatching(_storePage.sectionTitle.Text, title);
    }

    public async Task RefreshLibrary()
    {
        if (IsCurrentSectionTitle(GamesSectionTitle)) await LoadLibrary<Game>();
        else await LoadLibrary<Mod>(FocusedGame);
    }

    private void GetMatchingLibraryUIState<T>(LibraryItem item) where T : LibraryItem
    {
        string sectionTitle = IsItemTypeMod<T>() ? item == null ? "" : item.Name : GamesSectionTitle;
        _storePage.sectionTitle.Text = sectionTitle.ToUpper();
        _storePage.gamesButton.Content = sectionTitle;
        if (IsItemTypeMod<T>())
        {
            Show(_storePage.backButton);
            Show(_storePage.playButton);
            Show(_storePage.installLocalModButton);
        }
        else
        {
            Collapse(_storePage.backButton);
            Collapse(_storePage.playButton);
            Collapse(_storePage.installLocalModButton);
        }
    }

    private static async Task RefreshModsDatabaseAndManifest<T>() where T : LibraryItem
    {
        if (_modsDbManager.Database == null)
        {
            _modsDbManager.FileManager = _fileManager;
            await _modsDbManager.ReadDatabase(ModsDbFileName);
        }
        if (IsItemTypeMod<T>()) NativeManifest.ReadJSON();
    }

    public async Task LoadLibrary<T>(Game game = null, bool runIntegrityCheck = false, bool needsSharedToggleMacro = false) where T : LibraryItem, new()
    {
        if (IsCurrentlyLoadingLibrary()) return;
        await EnterLoadingLibraryState();
        GetMatchingLibraryUIState<T>(game);
        await _fileManager.GetAllFilesAndFolders();
        if (_fileManager.Files != null)
        {
            await RefreshModsDatabaseAndManifest<T>();
            if (runIntegrityCheck) await RunIntegrityCheck(game);
            if (needsSharedToggleMacro) await DownloadSharedToggleMacro(game);
            var items = new List<T>();
            foreach (Metadata folder in _fileManager.Files.Where(i => i.IsFolder))
            {
                var item = await CreateItemFromDropboxFolder<T>(folder);
                if (item != null) items.Add(item);
            }
            await _fileManager.GetItemThumbnailImages(items);
            foreach (T item in GetMatchingItems(game, items)) AddItemToLibrary(item);
            SortLibrary();
            FilterLibrary();
        }
        if (IsLibraryEmpty()) Show(_storePage.emptyLibraryMessage);
        ExitLoadingLibraryState();
    }
}