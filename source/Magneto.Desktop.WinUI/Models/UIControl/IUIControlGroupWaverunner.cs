using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Models.UIControl;
public interface IUIControlGroupWaverunner
{
    public IEnumerable<object> GetSettingsEnuerable();
    public IEnumerable<object> GetLayerMoveEnumerable();
    public IEnumerable<object> GetMarkOnlyEnumerable();
    public CheckBox GetMarkOnlyCheckBox();
    public Button GetMarkButton();
}
