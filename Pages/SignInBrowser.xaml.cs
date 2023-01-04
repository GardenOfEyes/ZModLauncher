using System;
using System.Windows;
using System.Windows.Controls;
using static ZModLauncher.GlobalStringConstants;
using static ZModLauncher.UIHelper;

namespace ZModLauncher.Pages;

/// <summary>
///     Interaction logic for SignInBrowser.xaml
/// </summary>
public partial class SignInBrowser : Page
{
    public SignInBrowser()
    {
        InitializeComponent();
        InitializeSignIn();
    }

    private void InitializeSignIn()
    {
        var configManager = new LauncherConfigManager();
        var client = new PatreonSignInClient(this)
        {
            RedirectUri = configManager.LauncherConfig[PatreonRedirectUriKey]?.ToString(),
            ClientId = configManager.LauncherConfig[PatreonClientIdKey]?.ToString(),
            ClientSecret = configManager.LauncherConfig[PatreonClientSecretKey]?.ToString(),
            TokenUrl = "https://www.patreon.com/api/oauth2/token",
            CreatorUrl = configManager.LauncherConfig[PatreonCreatorUrlKey]?.ToString()
        };
        client.AuthUrl = $"https://www.patreon.com/oauth2/authorize?response_type=code&client_id={client.ClientId}&redirect_uri={client.RedirectUri}";
        client.SetBrowserUrl(client.AuthUrl);
    }

    private async void browser_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await browser.EnsureCoreWebView2Async();
        }
        catch
        {
            ShowErrorDialog(AppPermissionsError);
            Environment.Exit(0);
        }
        browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
    }
}