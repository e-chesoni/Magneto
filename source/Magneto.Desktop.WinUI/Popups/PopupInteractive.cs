using Magneto.Desktop.WinUI.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Popups
{
    public sealed partial class PopupInteractive : ContentDialog
    {
        private MissionControl _missionControl;

        public string DialogTitle
        {
            get; set;
        }
        public string DialogMessage
        {
            get; set;
        }

        public PopupInteractive(XamlRoot xamlRoot, MissionControl mc, string title, string message)
        {
            InitializeComponent();
            XamlRoot = xamlRoot; // Set the XamlRoot for the dialog
            _missionControl = mc;

            DialogTitle = title;
            DialogMessage = message;
            DataContext = this; // Set the DataContext for data binding
        }

        private void IncrementBuildButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Implement
        }

        private void DecrementBuildButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Implement
        }

        private void IncrementPowderButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Implement
        }

        private void DecrementPowderButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Implement
        }

        private void IncrementSweepButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Implement
        }

        private void DecrementSweepButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Implement
        }

        // Method to close the dialog
        public void CloseDialog()
        {
            this.Hide();
        }
    }
}
