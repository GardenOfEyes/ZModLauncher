using System;
using System.Windows;
using System.Windows.Controls;

namespace ZModLauncher;

public static class UIHelper
{
    private static readonly Frame _mainFrame = ((MainWindow)Application.Current.MainWindow).MainFrame;

    public static void Show(UIElement control)
    {
        control.Visibility = Visibility.Visible;
    }

    public static void Collapse(UIElement control)
    {
        control.Visibility = Visibility.Collapsed;
    }

    public static void Enable(UIElement control)
    {
        control.IsEnabled = true;
    }

    public static void Disable(UIElement control)
    {
        control.IsEnabled = false;
    }

    public static MessageBoxResult ShowYesNoErrorDialog(string message)
    {
        return MessageBox.Show(message, "Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
    }

    public static void ShowErrorDialog(string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static MessageBoxResult ShowInformationDialog(string message)
    {
        return MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static MessageBoxResult ShowQuestionDialog(string message)
    {
        return MessageBox.Show(message, "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
    }

    public static bool IsPreviousPage(string pageName)
    {
        return _mainFrame.Content != null && _mainFrame.Content.ToString().Contains(pageName);
    }

    public static bool IsCurrentPage(string pageName)
    {
        return _mainFrame.CurrentSource.ToString().Contains(pageName);
    }

    public static void NavigateToPage(string pageName)
    {
        _mainFrame.Navigate(new Uri($"../Pages/{pageName}.xaml", UriKind.Relative));
    }

    public static void GoBackFromCurrentPage()
    {
        _mainFrame.GoBack();
    }
}