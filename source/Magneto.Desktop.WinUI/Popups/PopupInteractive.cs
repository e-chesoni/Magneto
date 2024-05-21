using Magneto.Desktop.WinUI.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Popups
{
    public class PopupInteractive : ContentDialog
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
            //InitializeComponent();
            XamlRoot = xamlRoot; // Set the XamlRoot for the dialog
            _missionControl = mc;

            DialogTitle = title;
            DialogMessage = message;
            DataContext = this; // Set the DataContext for data binding
        }

        private void IncrementButton_Click(object sender, RoutedEventArgs e)
        {
            //_missionControl.Increment();
        }

        private void DecrementButton_Click(object sender, RoutedEventArgs e)
        {
            //_missionControl.Decrement();
        }
    }
}
