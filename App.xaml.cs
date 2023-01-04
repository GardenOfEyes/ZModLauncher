using System;
using System.Threading;
using System.Windows.Forms;

namespace ZModLauncher;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private const string _launcherError = "The launcher has encountered an unexpected error, please share the below error details with the developers:\n\nError";

    [STAThread]
    public static void Main()
    {
        Application.ThreadException += Application_ThreadException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        var application = new App();
        application.InitializeComponent();
        application.Run();
    }

    private static void ShowLauncherErrorDialog(string errorDetails)
    {
        MessageBox.Show($@"{_launcherError}: {errorDetails}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
        ShowLauncherErrorDialog(e.Exception.Message);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        ShowLauncherErrorDialog(e.ExceptionObject.ToString());
    }
}