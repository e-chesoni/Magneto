using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Popups;
public static class PopupInteractiveHelper
{
    public static async Task ShowContentDialog(XamlRoot xamlRoot, MissionControl mc, string title, string message)
    {
        var dialog = new PopupInteractive(xamlRoot, mc, title, message);
        await dialog.ShowAsync();
    }
}
