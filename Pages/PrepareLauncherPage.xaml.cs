using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static ZModLauncher.UIHelper;
using static ZModLauncher.GlobalStringConstants;

namespace ZModLauncher.Pages;

/// <summary>
///     Interaction logic for PreparingToLaunchStore.xaml
/// </summary>
public partial class PreparingToLaunchStore : Page
{
    // Using a DependencyProperty as the backing store for PrepareMessage.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PrepareMessageProperty =
        DependencyProperty.Register("PrepareMessage", typeof(string), typeof(PreparingToLaunchStore));

    private readonly DispatcherTimer _dispatcherTimer = new();

    public PreparingToLaunchStore()
    {
        InitializeComponent();
        SetPrepareMessage();
        _dispatcherTimer.Tick += _dispatcherTimer_Tick;
        _dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
        _dispatcherTimer.Start();
    }

    public string PrepareMessage
    {
        get => (string)GetValue(PrepareMessageProperty);
        set => SetValue(PrepareMessageProperty, value);
    }

    private void SetPrepareMessage()
    {
        var configManager = new LauncherConfigManager();
        PrepareMessage = configManager.LauncherConfig[PrepareLauncherMessageLinkKey]?.ToString();
    }

    private void _dispatcherTimer_Tick(object sender, EventArgs e)
    {
        MainGrid.Height = 220;
        ProgressBar.Visibility = Visibility.Visible;
        _dispatcherTimer.Tick += _dispatcherTimer_Tick1;
        _dispatcherTimer.Stop();
        _dispatcherTimer.Interval = new TimeSpan(0, 0, 2);
        _dispatcherTimer.Start();
    }

    private void _dispatcherTimer_Tick1(object sender, EventArgs e)
    {
        NavigateToPage(MainPageName);
        _dispatcherTimer.Stop();
    }
}