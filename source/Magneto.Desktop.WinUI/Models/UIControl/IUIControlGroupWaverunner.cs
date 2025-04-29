using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Models.UIControl;
public interface IUIControlGroupWaverunner
{
    public IEnumerable<object> GetSettingsEnuerable();
    public IEnumerable<object> GetButtonGroupEnuerable();
}
