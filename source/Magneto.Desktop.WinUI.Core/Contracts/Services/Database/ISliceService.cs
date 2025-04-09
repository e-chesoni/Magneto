using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Print.Database;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.Database;
public interface ISliceService
{
    #region Counters
    public Task<long> TotalSlicesCount(string printId);

    /// <summary>
    /// Counts slices that are marked or unmarked
    /// </summary>
    /// <param name="printId"></param>
    /// <param name="marked">If true, get marked slices, else get unmarked slices.</param>
    /// <returns>count of marked or unmarked slices</returns>
    public Task<long> MarkedOrUnmarkedCount(string printId, bool marked);
    #endregion

    #region Getters
    Task<IEnumerable<SliceModel>> GetSlicesByPrintId(string printId);
    Task<SliceModel> GetSliceBySliceId(string sliceId);
    /// <summary>
    /// Returns the first slice model of associated with a given print model
    /// </summary>
    /// <param name="print"></param>
    /// <returns>The first <see cref="SliceModel"/> of the print.</returns>
    public Task<SliceModel?> GetFirstSlice(PrintModel print);

    /// <summary>
    /// Returns the first unmarked slice associated with the given print.
    /// If all slices are marked, returns the last slice.
    /// </summary>
    /// <param name="print">The print to retrieve the slice from.</param>
    /// <returns>The next <see cref="SliceModel"/> to process, or the last one if all are marked.</returns>
    public Task<SliceModel?> GetNextSlice(PrintModel print);
    public Task<SliceModel?> GetLastSlice(PrintModel print);
    Task<IEnumerable<SliceModel>> GetAllSlices();
    #endregion

    #region Checkers
    public Task<bool> AllSlicesMarked(string printId);
    #endregion

    #region CRUD
    Task AddSlice(SliceModel slice);
    Task EditSlice(SliceModel slice);
    Task DeleteSlice(SliceModel slice);
    Task DeleteAllSlices();
    #endregion
}