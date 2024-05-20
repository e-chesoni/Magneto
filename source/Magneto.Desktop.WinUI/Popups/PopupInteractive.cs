using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace Magneto.Desktop.WinUI.Popups
{
    public static class PopupInteractive
    {
        private static MissionControl _missionControl;

        public static async Task ShowContentDialog(XamlRoot xamlRoot, MissionControl mc, string title, string message)
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
    }

}
