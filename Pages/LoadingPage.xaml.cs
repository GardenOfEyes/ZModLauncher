using System;
using System.Windows.Controls;
using System.Windows.Threading;
using static ZModLauncher.UIHelper;

namespace ZModLauncher.Pages;

/// <summary>
///     Interaction logic for LoadingPage.xaml
/// </summary>
public partial class LoadingPage : Page
{
    private readonly bool _shouldLoadSignInPage;
    private readonly DispatcherTimer _signInPageTimer = new();

    public LoadingPage()
    {
        InitializeComponent();
        _shouldLoadSignInPage = !IsPreviousPage("SignInBrowser");
        PrepareLauncher();
    }

    private async void PrepareLauncher()
    {
        if (_shouldLoadSignInPage)
        {
            var updater = new LauncherUpdater();
            await updater.CheckForUpdates();
        }
        _signInPageTimer.Tick += SignInPageTimerTick;
        _signInPageTimer.Interval = new TimeSpan(0, 0, 2);
        _signInPageTimer.Start();
    }

    private void SignInPageTimerTick(object sender, EventArgs e)
    {
        NavigateToPage(_shouldLoadSignInPage ? "SignInPage" : "PrepareLauncherPage");
        _signInPageTimer.Stop();
    }
}