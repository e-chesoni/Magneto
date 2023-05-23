using System;
using System.Collections.Generic;
using System.Linq;
using Magneto.Desktop.WinUI.Core.Models;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services;

// Remove this class once your pages/features are using your data.
public interface ISamplePrintService
{
    Task<IEnumerable<SamplePrint>> GetGridDataAsync();

    Task<IEnumerable<SamplePrint>> GetContentGridDataAsync();

    Task<IEnumerable<SamplePrint>> GetListDetailsDataAsync();
}
