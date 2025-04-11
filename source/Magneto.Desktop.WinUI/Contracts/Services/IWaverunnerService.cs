using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Contracts.Services;
public interface IWaverunnerService
{
    public void TestConnection();
    public int StartRedPointer(string filePath);
    public int StopRedPointer();
    public Task<int> MarkEntityAsync(string filePath);
    public int GetMarkStatus();
    public int StopMark();

}
