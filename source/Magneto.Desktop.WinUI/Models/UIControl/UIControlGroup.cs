using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using static Magneto.Desktop.WinUI.Helpers.PrintUIControlGroupHelper;

namespace Magneto.Desktop.WinUI.Models.UIControl;
public interface UIControlGroup
{
    public IEnumerable<object> GetControlGroupEnuerable();
    public IEnumerable<object> GetBuildControlGroupEnuerable();
    public IEnumerable<object> GetPowderControlGroupEnuerable();
    public IEnumerable<object> GetSweepControlGroupEnuerable();
}
