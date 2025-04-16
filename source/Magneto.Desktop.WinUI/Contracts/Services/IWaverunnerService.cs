using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Contracts.Services;
public interface IWaverunnerService
{
    int TestConnection();
    int StartRedPointer(string filePath);
    int StopRedPointer();
    Task<int> MarkEntityAsync(string filePath);
    int GetMarkStatus();
    int StopMark();
}
