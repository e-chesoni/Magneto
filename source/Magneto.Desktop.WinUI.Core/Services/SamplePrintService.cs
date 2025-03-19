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
                // File naming convention: 0001_ss_316l_square_5x5.sjf
                UUID=1,
                SliceDirectory=@"C:\Scanner Application\Scanner Software\jobfiles\ss\316l\single_square_5x5mm", // @ symbol ensures backslashes are not interpreted as spaces
                DirectorySize=300000000,
                StartTimestamp=DateTime.Now,
                Status=SamplePrint.PrintStatus.NotStarted,
                SymbolCode = 57688,
                SymbolName = "Pictures",
            },
            new SamplePrint()
            {
                UUID=2,
                SliceDirectory=@"C:\Scanner Application\Scanner Software\jobfiles\copper\grcop-42\grid_lines_5mm",
                DirectorySize=300000000,
                StartTimestamp=DateTime.Now,
                Status=SamplePrint.PrintStatus.NotStarted,
                SymbolCode = 57688,
                SymbolName = "Pictures",
            },
            new SamplePrint()
            {
                UUID=3,
                SliceDirectory=@"C:\Scanner Application\Scanner Software\jobfiles\copper\grcop-42\grid_squares_5x5mm",
                DirectorySize=300000000,
                StartTimestamp=DateTime.Now,
                Status=SamplePrint.PrintStatus.NotStarted,
                SymbolCode = 57688,
                SymbolName = "Pictures",
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
