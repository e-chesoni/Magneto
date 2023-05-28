using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;

namespace Magneto.Desktop.WinUI.Core.Services;

/// <summary>
/// Generates sample print data
/// </summary>
public class SamplePrintService : ISamplePrintService
{
    #region Private Variables

    /// <summary>
    /// A list of sample print data
    /// </summary>
    private List<SamplePrint> _allPrints;

    #endregion

    #region Constructor

    /// <summary>
    ///  Constructor
    /// </summary>
    public SamplePrintService() { }

    #endregion

    #region Generators

    /// <summary>
    /// Generate sample prints
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<SamplePrint> GetPrints()
    {
        return new List<SamplePrint>()
        {
            new SamplePrint()
            {
                FileName="Structure 1",
                // TODO: remember you may need to use at symbol with processing file locations
                // Example: @"c:\prints\Structure1.dwg"
                FileLocation="c:\\prints\\Structure1.dwg",
                FileSize=300000000,
                CreatedAt=DateTime.Now,
                Status=SamplePrint.PrintStatus.NotStarted,
                SymbolCode = 57643,
                SymbolName = "Globe",
            },
            new SamplePrint()
            {
                FileName="Structure 1",
                FileLocation="c:\\prints\\Structure2.dwg",
                FileSize=300000000,
                CreatedAt=DateTime.Now,
                Status=SamplePrint.PrintStatus.NotStarted,
                SymbolCode = 57737,
                SymbolName = "Audio",
            },
            new SamplePrint()
            {
                FileName="Structure 3",
                FileLocation="c:\\prints\\Structure3.dwg",
                FileSize=300000000,
                CreatedAt=DateTime.Now,
                Status=SamplePrint.PrintStatus.NotStarted,
                SymbolCode = 57620,
                SymbolName = "Camera",
            },
        };
    }

    public async Task<IEnumerable<SamplePrint>> GetContentGridDataAsync()
    {
        _allPrints = (List<SamplePrint>)GetPrints();
        await Task.CompletedTask;
        return _allPrints;
    }

    public async Task<IEnumerable<SamplePrint>> GetGridDataAsync()
    {
        _allPrints = (List<SamplePrint>)GetPrints();
        await Task.CompletedTask;
        return _allPrints;
    }

    public async Task<IEnumerable<SamplePrint>> GetListDetailsDataAsync()
    {
        _allPrints = (List<SamplePrint>)GetPrints();
        await Task.CompletedTask;
        return _allPrints;
    }

    #endregion
}
