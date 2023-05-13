﻿using Magneto.Desktop.WinUI.Core.Models;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services;

// Remove this class once your pages/features are using your data.
public interface ISampleDataService
{
    Task<IEnumerable<SampleOrder>> GetGridDataAsync();

    Task<IEnumerable<SampleOrder>> GetContentGridDataAsync();

    Task<IEnumerable<SampleOrder>> GetListDetailsDataAsync();
}
