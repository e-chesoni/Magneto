using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Helpers;
public static class UpdateUITextHelper
{
    public static void UpdateUIText(TextBlock textBlock, string update)
    {
        if (textBlock != null)
        {
            // Assuming DispatcherQueue is accessible or passed in some way
            textBlock.DispatcherQueue.TryEnqueue(() =>
            {
                textBlock.Text = update;
            });
        }
        else
        {
            var msg = "Motor text block is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
        
    }

    public static void UpdateUIText(TextBox textBox, string update)
    {
        if (textBox != null)
        {
            // Assuming DispatcherQueue is accessible or passed in some way
            textBox.DispatcherQueue.TryEnqueue(() =>
            {
                textBox.Text = update;
            });
        }
        else
        {
            var msg = "Motor text box is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
        
    }

}
