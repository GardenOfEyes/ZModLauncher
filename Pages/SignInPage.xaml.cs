using System.Windows;
using System.Windows.Controls;
using static ZModLauncher.UIHelper;

namespace ZModLauncher.Pages;

/// <summary>
///     Interaction logic for SignInPage.xaml
/// </summary>
public partial class SignInPage : Page
{
    // Using a DependencyProperty as the backing store for LauncherVersion.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty LauncherVersionProperty =
        DependencyProperty.Register("LauncherVersion", typeof(string), typeof(SignInPage));

    public SignInPage()
    {
        InitializeComponent();
        LauncherVersion = "1.2.0";
    }

    public string LauncherVersion
    {
        get => (string)GetValue(LauncherVersionProperty);
        set => SetValue(LauncherVersionProperty, value);
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        NavigateToPage("SettingsPage");
    }

    private void PatreonButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToPage("SignInBrowser");
    }
}