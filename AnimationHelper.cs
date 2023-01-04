using System.Windows;
using System.Windows.Media.Animation;

namespace ZModLauncher;

public static class AnimationHelper
{
    public static void ApplyStoryboardAnim(DependencyObject anim, DependencyObject control)
    {
        if (anim != null) Storyboard.SetTarget(anim, control);
    }

    public static void Play(Storyboard anim)
    {
        anim.Begin();
    }

    public static void Stop(Storyboard anim)
    {
        anim.Stop();
    }
}