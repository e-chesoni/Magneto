using Magneto.Desktop.WinUI.Core.Models;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class PrintQueueDetailControl : UserControl
{
    public SamplePrint? ListDetailsMenuItem
    {
        get => GetValue(ListDetailsMenuItemProperty) as SamplePrint;
        set => SetValue(ListDetailsMenuItemProperty, value);
    }

    public static readonly DependencyProperty ListDetailsMenuItemProperty = DependencyProperty.Register("ListDetailsMenuItem", typeof(SamplePrint), typeof(PrintQueueDetailControl), new PropertyMetadata(null, OnListDetailsMenuItemPropertyChanged));

    public PrintQueueDetailControl()
    {
        InitializeComponent();
    }

    private static void OnListDetailsMenuItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PrintQueueDetailControl control)
        {
            control.ForegroundElement.ChangeView(0, 0, 1);
        }
    }
}
