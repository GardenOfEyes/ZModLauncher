using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using static ZModLauncher.UIHelper;
using static ZModLauncher.GlobalStringConstants;

namespace ZModLauncher.Pages;

/// <summary>
///     Interaction logic for SettingsPage.xaml
/// </summary>
public partial class SettingsPage : Page
{
    // Using a DependencyProperty as the backing store for YouTubeLink.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty YouTubeLinkProperty =
        DependencyProperty.Register("YouTubeLink", typeof(string), typeof(SettingsPage));

    // Using a DependencyProperty as the backing store for TwitterLink.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TwitterLinkProperty =
        DependencyProperty.Register("TwitterLink", typeof(string), typeof(SettingsPage));

    // Using a DependencyProperty as the backing store for YouTubeLink.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty RoadmapLinkProperty =
        DependencyProperty.Register("RoadmapLink", typeof(string), typeof(SettingsPage));

    private static readonly string _loginInfoFolderPath = $"{NativeManifest.AppRootPath}\\{NativeManifest.ExecutableAppName}.exe.WebView2";

    public SettingsPage()
    {
        InitializeComponent();
        SetResourceLinks();
        IsInvokedFromSignIn();
        HasLoginInfo();
    }

    public string YouTubeLink
    {
        get => (string)GetValue(YouTubeLinkProperty);
        set => SetValue(YouTubeLinkProperty, value);
    }

    public string TwitterLink
    {
        get => (string)GetValue(TwitterLinkProperty);
        set => SetValue(TwitterLinkProperty, value);
    }

    public string RoadmapLink
    {
        get => (string)GetValue(RoadmapLinkProperty);
        set => SetValue(RoadmapLinkProperty, value);
    }

    private void SetResourceLinks()
    {
        var configManager = new LauncherConfigManager();
        YouTubeLink = configManager.LauncherConfig[YouTubeResourceLinkKey]?.ToString();
        TwitterLink = configManager.LauncherConfig[TwitterResourceLinkKey]?.ToString();
        RoadmapLink = configManager.LauncherConfig[RoadmapResourceLinkKey]?.ToString();
    }

    private void IsInvokedFromSignIn()
    {
        Collapse(IsPreviousPage(SignInPageName) ? signOutButton : clearLoginInfoButton);
    }

    private void HasLoginInfo()
    {
        if (!Directory.Exists(_loginInfoFolderPath)) Collapse(clearLoginInfoButton);
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        GoBackFromCurrentPage();
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            if (((Hyperlink)e.Source).Inlines.FirstOrDefault() is Run)
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
        }
        catch { }
        e.Handled = true;
    }

    private void SignOutButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToPage(SignInPageName);
    }

    private void ClearLoginInfoButton_Click(object sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(_loginInfoFolderPath))
        {
            ShowErrorDialog(LoginInfoClearError);
            HasLoginInfo();
            return;
        }
        Directory.Delete(_loginInfoFolderPath, true);
        HasLoginInfo();
    }
}