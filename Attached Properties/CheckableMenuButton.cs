using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZModLauncher.Attached_Properties;

internal class CheckableMenuButton : RadioButton
{
    // Using a DependencyProperty as the backing store for Icon.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register("Icon", typeof(PathGeometry), typeof(CheckableMenuButton));

    // Using a DependencyProperty as the backing store for ImageIcon.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageIconProperty =
        DependencyProperty.Register("ImageIcon", typeof(ImageSource), typeof(CheckableMenuButton));

    // Using a DependencyProperty as the backing store for IconHeight.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IconHeightProperty =
        DependencyProperty.Register("IconHeight", typeof(double), typeof(CheckableMenuButton));

    // Using a DependencyProperty as the backing store for IconWidth.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IconWidthProperty =
        DependencyProperty.Register("IconWidth", typeof(double), typeof(CheckableMenuButton));

    // Using a DependencyProperty as the backing store for IconFill.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IconFillProperty =
        DependencyProperty.Register("IconFill", typeof(Brush), typeof(CheckableMenuButton));

    // Using a DependencyProperty as the backing store for IconFillOnHover.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IconFillOnHoverProperty =
        DependencyProperty.Register("IconFillOnHover", typeof(Brush), typeof(CheckableMenuButton));

    // Using a DependencyProperty as the backing store for IconBackground.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IconBackgroundProperty =
        DependencyProperty.Register("IconBackground", typeof(Brush), typeof(CheckableMenuButton));

    // Using a DependencyProperty as the backing store for IconBackgroundHover.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IconBackgroundHoverProperty =
        DependencyProperty.Register("IconBackgroundHover", typeof(Brush), typeof(CheckableMenuButton));
    public PathGeometry Icon
    {
        get => (PathGeometry)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public ImageSource ImageIcon
    {
        get => (ImageSource)GetValue(ImageIconProperty);
        set => SetValue(ImageIconProperty, value);
    }

    public double IconHeight
    {
        get => (double)GetValue(IconHeightProperty);
        set => SetValue(IconHeightProperty, value);
    }

    public double IconWidth
    {
        get => (double)GetValue(IconWidthProperty);
        set => SetValue(IconWidthProperty, value);
    }

    public Brush IconFill
    {
        get => (Brush)GetValue(IconFillProperty);
        set => SetValue(IconFillProperty, value);
    }

    public Brush IconFillOnHover
    {
        get => (Brush)GetValue(IconFillOnHoverProperty);
        set => SetValue(IconFillOnHoverProperty, value);
    }

    public Brush IconBackground
    {
        get => (Brush)GetValue(IconBackgroundProperty);
        set => SetValue(IconBackgroundProperty, value);
    }

    public Brush IconBackgroundHover
    {
        get => (Brush)GetValue(IconBackgroundHoverProperty);
        set => SetValue(IconBackgroundHoverProperty, value);
    }
}