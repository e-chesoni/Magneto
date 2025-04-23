using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Contracts.Services;
public interface IWaverunnerService
{
    int IsRunning();
    int TestConnection();
    int StartRedPointer(string filePath);
    int StopRedPointer();
    (int status, double markTIme) GetLastMark();
    Task<int> MarkEntityAsync(string filePath);
    int GetMarkStatus();
    int StopMark();
}
