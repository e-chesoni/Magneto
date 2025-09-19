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

        public static async Task<double?> ShowThicknessDialogAsync(
        XamlRoot xamlRoot,
        double defaultValueMm = 0.03,
        double minMm = 0.001,
        double maxMm = 5.0)
        {
            var panel = new StackPanel { Spacing = 8, MinWidth = 320 };
            panel.Children.Add(new TextBlock { Text = "Enter slice thickness (mm):" });

            var nb = new NumberBox
            {
                Value = defaultValueMm,
                Minimum = minMm,
                Maximum = maxMm,
                SmallChange = 0.01,
                LargeChange = 0.1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };
            panel.Children.Add(nb);

            var dialog = new ContentDialog
            {
                Title = "Slice Thickness",
                Content = panel,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            // Validate before closing
            dialog.PrimaryButtonClick += (_, args) =>
            {
                if (double.IsNaN(nb.Value) || nb.Value < minMm || nb.Value > maxMm)
                {
                    args.Cancel = true; // keep dialog open
                    nb.Focus(FocusState.Programmatic);
                }
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? nb.Value : (double?)null;
        }

        public static async Task<(double? Thickness, double? HatchSpacing)> ShowSliceDialogAsync(
        XamlRoot xamlRoot,
        double defaultThicknessMm = 0.03,
        double minThicknessMm = 0.001,
        double maxThicknessMm = 1.0,
        double defaultHatchSpacingMm = 0.12,
        double minHatchSpacingMm = 0.001,
        double maxHatchSpacingMm = 0.5)
        {
            var panel = new StackPanel { Spacing = 12, MinWidth = 320 };

            // --- Thickness ---
            panel.Children.Add(new TextBlock { Text = "Enter slice thickness (mm):" });
            var thicknessBox = new NumberBox
            {
                Value = defaultThicknessMm,
                Minimum = minThicknessMm,
                Maximum = maxThicknessMm,
                SmallChange = 0.01,
                LargeChange = 0.1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };
            panel.Children.Add(thicknessBox);

            // --- Hatch Spacing ---
            panel.Children.Add(new TextBlock { Text = "Enter hatch spacing (mm):" });
            var hatchBox = new NumberBox
            {
                Value = defaultHatchSpacingMm,
                Minimum = minHatchSpacingMm,
                Maximum = maxHatchSpacingMm,
                SmallChange = 0.01,
                LargeChange = 0.1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };
            panel.Children.Add(hatchBox);

            var dialog = new ContentDialog
            {
                Title = "Slice Parameters",
                Content = panel,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            };

            // Validate before closing
            dialog.PrimaryButtonClick += (_, args) =>
            {
                if (double.IsNaN(thicknessBox.Value) ||
                    thicknessBox.Value < minThicknessMm ||
                    thicknessBox.Value > maxThicknessMm)
                {
                    args.Cancel = true;
                    thicknessBox.Focus(FocusState.Programmatic);
                    return;
                }

                if (double.IsNaN(hatchBox.Value) ||
                    hatchBox.Value < minHatchSpacingMm ||
                    hatchBox.Value > maxHatchSpacingMm)
                {
                    args.Cancel = true;
                    hatchBox.Focus(FocusState.Programmatic);
                    return;
                }
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary
                ? (thicknessBox.Value, hatchBox.Value)
                : (null, null);
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
