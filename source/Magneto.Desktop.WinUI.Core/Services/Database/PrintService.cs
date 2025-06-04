using System.Diagnostics;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database;
using Magneto.Desktop.WinUI.Core.Models.Print.Database;
using MongoDB.Driver;

namespace Magneto.Desktop.WinUI.Core.Services.Database;
public class PrintService : IPrintService
{
    private readonly IMongoCollection<PrintModel> _prints;

    private readonly ISliceService _sliceService;

    public PrintService(IMongoDbService mongoDbService, ISliceService sliceService)
    {
        _prints = mongoDbService.GetCollection<PrintModel>(); // gets all prints in db at start up
        _sliceService = sliceService;
    }

    public async Task EditSlice(SliceModel slice)
    {
        await _sliceService.EditSlice(slice);
    }

    #region Counters
    /// <summary>
    /// Gets the total number of prints in the print collection
    /// </summary>
    /// <returns>long total prints in database</returns>
    public async Task<long> TotalPrintsCount()
    {
        return await _prints.CountDocumentsAsync(_ => true);
    }
    public async Task<long> TotalSlicesCount(string printId)
    {
        return await _sliceService.TotalSlicesCount(printId);
    }
    public async Task<long> MarkedSliceCount(string printId)
    {
        return await _sliceService.MarkedOrUnmarkedCount(printId, true); // if true, get marked slices (false -> get unmarked slices)
    }
    #endregion

    #region Getters
    public async Task<PrintModel> GetFirstPrintAsync()
    {
        return await _prints
            .Find(_ => true)
            .SortByDescending(p => p.startTime)
            .FirstOrDefaultAsync();
    }
    public async Task<PrintModel?> GetPrintByDirectory(string directoryPath)
    {
        MagnetoLogger.Log($"Directory path: {directoryPath}", LogFactoryLogLevel.LogLevel.VERBOSE);
        var normalizedPath = Path.GetFullPath(directoryPath.Trim()).TrimEnd(Path.DirectorySeparatorChar);
        var filter = Builders<PrintModel>.Filter.Eq(p => p.directoryPath, normalizedPath);
        var print = await _prints.Find(filter).FirstOrDefaultAsync();
        var msg = print != null
            ? $"✅ Found print. Path: {print.directoryPath}"
            : $"❌ No print found for path: {normalizedPath}";
        if (print == null)
        {
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
        else
        {
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        }
        return print;
    }
    public async Task<PrintModel?> GetMostRecentPrintByDirectory(string directoryPath)
    {
        MagnetoLogger.Log($"Directory path: {directoryPath}", LogFactoryLogLevel.LogLevel.VERBOSE);
        var normalizedPath = Path.GetFullPath(directoryPath.Trim()).TrimEnd(Path.DirectorySeparatorChar);
        var filter = Builders<PrintModel>.Filter.Eq(p => p.directoryPath, normalizedPath);
        var sort = Builders<PrintModel>.Sort.Descending(p => p.startTime);

        var print = await _prints.Find(filter).Sort(sort).FirstOrDefaultAsync();

        var msg = print != null
            ? $"✅ Found most recent print. Path: {print.directoryPath}, Time: {print.startTime}"
            : $"❌ No print found for path: {normalizedPath}";

        MagnetoLogger.Log(msg, print != null ? LogFactoryLogLevel.LogLevel.SUCCESS : LogFactoryLogLevel.LogLevel.ERROR);

        return print;
    }

    public async Task<PrintModel> GetPrintById(string id)
    {
        var filter = Builders<PrintModel>.Filter.Eq(p => p.id, id);
        return await _prints.Find(filter).FirstOrDefaultAsync();
    }
    public async Task<IEnumerable<SliceModel>> GetSlicesByPrintId(string printId)
    {
        if (string.IsNullOrEmpty(printId))
            return Enumerable.Empty<SliceModel>();

        return await _sliceService.GetSlicesByPrintId(printId);
    }
    public async Task<IEnumerable<PrintModel>> GetAllPrints()
    {
        var results = await _prints.Find(_ => true).ToListAsync();
        return results.AsEnumerable();
    }
    #endregion

    #region Checkers
    public async Task<bool> IsPrintComplete(string printId)
    {
        return await _sliceService.AllSlicesMarked(printId);
    }
    #endregion

    #region CRUD
    public async Task AddPrint(PrintModel print)
    {
        await _prints.InsertOneAsync(print);
    }
    public async Task DeletePrint(PrintModel print)
    {
        var toDelete = Builders<PrintModel>.Filter.Eq(p => p.id, print.id);
        var slices = await GetSlicesByPrintId(print.id);
        // get print slices
        foreach (var s in slices)
        {
            // delete print slices
            await _sliceService.DeleteSlice(s);
        }
        // delete print
        await _prints.DeleteOneAsync(toDelete);
    }
    public async Task EditPrint(PrintModel print)
    {
        var toEdit = Builders<PrintModel>.Filter.Eq(p => p.id, print.id);
        await _prints.ReplaceOneAsync(toEdit, print);
    }
    public async Task DeleteAllPrints()
    {
        await _prints.DeleteManyAsync(_ => true);
    }
    #endregion
}

