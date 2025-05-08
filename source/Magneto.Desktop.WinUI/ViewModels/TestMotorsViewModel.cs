using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.States.PrintStates;

namespace Magneto.Desktop.WinUI.ViewModels;

public class TestMotorsViewModel : ObservableRecipient
{
    private readonly PrintStateMachine _psm;
    public TestMotorsViewModel()
    {
        _psm = App.GetService<PrintStateMachine>();
    }

    public RoutineStateMachine GetRoutineStateMachine() => _psm.rsm;
}
