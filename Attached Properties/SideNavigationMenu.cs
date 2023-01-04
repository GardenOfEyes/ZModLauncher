using System.Windows;
using System.Windows.Controls;

namespace ZModLauncher.Attached_Properties;

public class SideNavigationMenu : UserControl
{
    // Using a DependencyProperty as the backing store for MenuButtonsContainer.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MenuButtonsContainerProperty =
        DependencyProperty.Register("MenuButtonsContainer", typeof(object), typeof(SideNavigationMenu));

    // Using a DependencyProperty as the backing store for FooterMenuButtonsContainer.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty FooterMenuButtonsContainerProperty =
        DependencyProperty.Register("FooterMenuButtonsContainer", typeof(object), typeof(SideNavigationMenu));
    public object MenuButtonsContainer
    {
        get => GetValue(MenuButtonsContainerProperty);
        set => SetValue(MenuButtonsContainerProperty, value);
    }

    public object FooterMenuButtonsContainer
    {
        get => GetValue(FooterMenuButtonsContainerProperty);
        set => SetValue(FooterMenuButtonsContainerProperty, value);
    }
}