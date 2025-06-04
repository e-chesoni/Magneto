using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.UI;

namespace Magneto.Desktop.WinUI.Toasts;

public enum ToastType
{
    Info,
    Success,
    Error
}
public static class ToastManager
{
    private static int _activeToastCount = 0;
    private const int ToastSpacing = 10;
    private const int ToastEstimatedHeight = 80;

    public static void ShowToast(string message, XamlRoot xamlRoot, ToastType type = ToastType.Info, int durationMs = 3000)
    {
        if (xamlRoot == null || !xamlRoot.IsHostVisible) return;

        var popup = new Popup
        {
            XamlRoot = xamlRoot
        };

        // set background color based on toast type
        SolidColorBrush background = type switch
        {
            ToastType.Success => new SolidColorBrush(Colors.ForestGreen),
            ToastType.Error => new SolidColorBrush(Colors.DarkRed),
            _ => new SolidColorBrush(Colors.Black),
        };

        var toastText = new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush(Colors.White),
            Padding = new Thickness(16),
            MaxWidth = 300,
            TextWrapping = TextWrapping.Wrap
        };

        var container = new Border
        {
            Background = background,
            CornerRadius = new CornerRadius(8),
            Child = toastText,
            RenderTransform = new TranslateTransform { X = 320 }, // start off screen
            Opacity = 0
        };

        popup.Child = container;

        // estimate vertical position based on number of active toasts
        double baseOffset = 40 + (_activeToastCount * (ToastEstimatedHeight + ToastSpacing));
        popup.HorizontalOffset = xamlRoot.Size.Width - 320;
        popup.VerticalOffset = baseOffset;
        popup.IsOpen = true;
        _activeToastCount++;

        var transform = (TranslateTransform)container.RenderTransform;

        // slide in + fade in
        var fadeInStoryboard = new Storyboard();

        var slideIn = new DoubleAnimation
        {
            From = 320,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(300)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(slideIn, transform);
        Storyboard.SetTargetProperty(slideIn, "X");

        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(300))
        };
        Storyboard.SetTarget(fadeIn, container);
        Storyboard.SetTargetProperty(fadeIn, "Opacity");

        fadeInStoryboard.Children.Add(slideIn);
        fadeInStoryboard.Children.Add(fadeIn);
        fadeInStoryboard.Begin();

        // auto-dismiss after delay
        Task.Delay(durationMs).ContinueWith(_ =>
        {
            container.DispatcherQueue.TryEnqueue(() =>
            {
                // slide out + fade out
                var fadeOutStoryboard = new Storyboard();

                var slideOut = new DoubleAnimation
                {
                    From = 0,
                    To = 320,
                    Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(slideOut, transform);
                Storyboard.SetTargetProperty(slideOut, "X");

                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(300))
                };
                Storyboard.SetTarget(fadeOut, container);
                Storyboard.SetTargetProperty(fadeOut, "Opacity");

                fadeOutStoryboard.Children.Add(slideOut);
                fadeOutStoryboard.Children.Add(fadeOut);

                fadeOutStoryboard.Completed += (s, e) =>
                {
                    popup.IsOpen = false;
                    _activeToastCount = Math.Max(0, _activeToastCount - 1);
                };

                fadeOutStoryboard.Begin();
            });
        });
    }
}