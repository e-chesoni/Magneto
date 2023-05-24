﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.Image;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
public interface IBuildManagerState
{
    void SetStateMachine(BuildManager bm);

    void Start();

    void Draw();

    void Pause();

    void Cancel();
}
