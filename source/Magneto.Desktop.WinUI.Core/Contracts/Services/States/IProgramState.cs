using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.States;
public interface IProgramState
{
    void Process();
    void Pause();
    void Add();
    void Remove();
    void Cancel();
    public void ChangeStateTo(IProgramState state);
}
