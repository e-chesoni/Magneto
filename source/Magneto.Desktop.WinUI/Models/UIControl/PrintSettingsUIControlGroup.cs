using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Models.UIControl;
public class PrintSettingsUIControlGroup : UIControlGroup
{
    public IEnumerable<object> controlEnumerable;
    public PrintSettingsUIControlGroup(IEnumerable<object> controls)
    {
        controlEnumerable = controls;
    }

    IEnumerable<object> UIControlGroup.GetControlGroupEnuerable()
    {
        return controlEnumerable;
    }
}
