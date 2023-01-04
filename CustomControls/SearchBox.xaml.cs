using System;
using System.Windows.Controls;

namespace ZModLauncher.CustomControls;

/// <summary>
///     Interaction logic for SearchBox.xaml
/// </summary>
public partial class SearchBox : UserControl
{
    public TextBox textBox;

    public SearchBox()
    {
        InitializeComponent();
    }

    private void TextBox_Initialized(object sender, EventArgs e)
    {
        textBox = (TextBox)sender;
    }

    private void ClearBtn_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        textBox.Clear();
    }
}