using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using static ZModLauncher.UIHelper;

namespace ZModLauncher.Pages;

/// <summary>
///     Interaction logic for StorePage.xaml
/// </summary>
public partial class StorePage : Page
{
    private readonly LibraryManager libraryManager;

    public StorePage()
    {
        InitializeComponent();
        libraryManager = new LibraryManager(this);
    }

    private void BtnSettings_OnClick(object sender, RoutedEventArgs e)
    {
        NavigateToPage("SettingsPage");
    }

    private void GamesMenuButton_Loaded(object sender, RoutedEventArgs e)
    {
        gamesButton.IsChecked = true;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await libraryManager.RefreshLibrary();
    }

    private async void LoadGames(object sender, RoutedEventArgs e)
    {
        await libraryManager.LoadLibrary<Game>();
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(libraryManager.FocusedGame.ExecutablePath);
    }

    private void SortByBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        libraryManager?.SortLibrary();
    }

    private void FilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        libraryManager?.FilterLibrary();
    }

    private void SearchBox_Loaded(object sender, RoutedEventArgs e)
    {
        searchBox.textBox.TextChanged += TextBoxOnTextChanged;
    }

    private void TextBoxOnTextChanged(object sender, TextChangedEventArgs e)
    {
        libraryManager?.FilterLibrary();
    }

    private void ChangeModFiltersVisibility(Visibility visibility)
    {
        ((ComboBoxItem)filterBox.Items.GetItemAt(2)).Visibility = visibility;
        ((ComboBoxItem)filterBox.Items.GetItemAt(3)).Visibility = visibility;
        ((ComboBoxItem)filterBox.Items.GetItemAt(4)).Visibility = visibility;
    }

    private void HideModFilters()
    {
        ChangeModFiltersVisibility(Visibility.Collapsed);
    }

    private void ShowModFilters()
    {
        ChangeModFiltersVisibility(Visibility.Visible);
    }

    private async void BackButton_Click(object sender, RoutedEventArgs e)
    {
        await libraryManager.LoadLibrary<Game>();
        HideModFilters();
    }

    private void PlayButton_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        ShowModFilters();
    }

    private void FilterBox_Loaded(object sender, RoutedEventArgs e)
    {
        HideModFilters();
    }

    private async void InstallLocalModButton_Click(object sender, RoutedEventArgs e)
    {
        await libraryManager.InstallLocalMod();
    }
}