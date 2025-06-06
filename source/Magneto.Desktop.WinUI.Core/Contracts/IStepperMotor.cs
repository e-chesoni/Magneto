﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Contracts;
/// <summary>
/// Interface for Magneto Motors
/// </summary>
public interface IStepperMotor
{
    #region Movement
    public string[] CreateMoveProgramHelper(double target, bool isAbsolute, bool? moveUp);
    int WriteAbsoluteMoveRequest(double pos);
    int WriteRelativeMoveRequest(double pos);
    Task MoveMotorRelAsync(double steps);
    void Stop();
    Task<int> WaitForStop();
    int HomeMotor();
    #endregion
}
