using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Print.Database;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.Database;
public interface IPrintService
{

    #region Counters
    Task<long> TotalPrintsCount();
    public Task<long> TotalSlicesCount(string printId);
    public Task<long> MarkedOrUnmarkedCount(string printId);
    #endregion

    #region Getters
    public Task<PrintModel> GetFirstPrintAsync();
    Task<PrintModel> GetPrintByDirectory(string DirectoryPath);
    Task<PrintModel> GetPrintById(string id);
    Task<IEnumerable<SliceModel>> GetSlicesByPrintId(string id);
    Task<IEnumerable<PrintModel>> GetAllPrints();
    #endregion

    #region Checkers
    public Task<bool> IsPrintComplete(string printId);
    #endregion

    #region CRUD
    public Task EditSlice(SliceModel slice);
    Task AddPrint(PrintModel print);
    Task EditPrint(PrintModel print);
    Task DeletePrint(PrintModel print);
    Task DeleteAllPrints();
    #endregion
}