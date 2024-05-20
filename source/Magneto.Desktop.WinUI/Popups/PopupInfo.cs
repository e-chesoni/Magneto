using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Windows.UI;

namespace Magneto.Desktop.WinUI.Popups;
public static class PopupInfo
{
    public static async Task ShowContentDialog(XamlRoot xamlRoot, string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "Ok",
            XamlRoot = xamlRoot,
        };

        await dialog.ShowAsync();
    }

    // TODO: Create custom dialog button
    public static async Task ShowContentDialog(XamlRoot xamlRoot, string title, string message, Color backgroundColor, Color foregroundColor)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "Ok",
            XamlRoot = xamlRoot,
            Background = new SolidColorBrush(backgroundColor),
            Foreground = new SolidColorBrush(foregroundColor)
        };

        // Customize button colors
        dialog.CloseButtonStyle = new Style(typeof(Button));
        dialog.CloseButtonStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Microsoft.UI.Colors.Gray)));
        dialog.CloseButtonStyle.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Microsoft.UI.Colors.Black)));

        await dialog.ShowAsync();
    }

}
