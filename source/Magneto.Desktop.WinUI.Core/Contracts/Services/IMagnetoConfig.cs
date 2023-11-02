using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services;

public interface IMagnetoConfig
{
    public MagnetoMotor GetFirstMotor();
    Task<IEnumerable<MagnetoMotor>> GetMotorDataAsync();

}