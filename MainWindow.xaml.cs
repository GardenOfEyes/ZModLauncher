using System;
using System.Reflection;
using System.Windows;

namespace ZModLauncher;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    // Using a DependencyProperty as the backing store for ExecutableName.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ExecutableNameProperty =
        DependencyProperty.Register("ExecutableName", typeof(string), typeof(MainWindow));

    public MainWindow()
    {
        InitializeComponent();
    }

    public string ExecutableName
    {
        get => (string)GetValue(ExecutableNameProperty);
        set => SetValue(ExecutableNameProperty, value);
    }

    private void This_Initialized(object sender, EventArgs e)
    {
        ExecutableName = Assembly.GetExecutingAssembly().GetName().Name;
    }
}