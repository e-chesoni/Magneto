using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Windows.UI;

namespace Magneto.Desktop.WinUI.Popups
{
    public static class PopupInfo
    {
        private static ContentDialog _dialog;

        public static async Task ShowContentDialog(XamlRoot xamlRoot, string title, string message)
        {
            _dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Ok",
                XamlRoot = xamlRoot,
            };

            await _dialog.ShowAsync();
        }

        public static async Task ShowContentDialog(XamlRoot xamlRoot, string title, string message, Color backgroundColor, Color foregroundColor)
        {
            _dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Ok",
                XamlRoot = xamlRoot,
                Background = new SolidColorBrush(backgroundColor),
                Foreground = new SolidColorBrush(foregroundColor)
            };

            // Customize button colors
            _dialog.CloseButtonStyle = new Style(typeof(Button));
            _dialog.CloseButtonStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Microsoft.UI.Colors.Gray)));
            _dialog.CloseButtonStyle.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Microsoft.UI.Colors.Black)));

            await _dialog.ShowAsync();
        }

        public static async Task<bool> ShowConfirmationDialog(XamlRoot xamlRoot, string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "Continue",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = xamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        public static async Task<bool> ShowYesNoDialog(XamlRoot xamlRoot, string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = xamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }


        // Method to hide the dialog
        public static void HideContentDialog()
        {
            _dialog?.Hide();
        }
    }
}
