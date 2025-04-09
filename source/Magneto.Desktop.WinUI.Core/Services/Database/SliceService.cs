using Magneto.Desktop.WinUI.Core.Contracts.Services.Database;
using Magneto.Desktop.WinUI.Core.Models.Print.Database;
using MongoDB.Driver;

namespace Magneto.Desktop.WinUI.Core.Services.Database;
public class SliceService : ISliceService
{
    private readonly IMongoCollection<SliceModel> _slices;
    public SliceService(IMongoDbService mongoDbService)
    {
        _slices = mongoDbService.GetCollection<SliceModel>();
    }
    #region Counters
    public async Task<long> TotalSlicesCount(string printId)
    {
        var filter = Builders<SliceModel>.Filter.And(
            Builders<SliceModel>.Filter.Eq(s => s.printId, printId)
        );
        return await _slices.CountDocumentsAsync(filter);
    }
    /// <summary>
    /// Counts slices that are marked or unmarked
    /// </summary>
    /// <param name="printId"></param>
    /// <param name="marked">If true, get marked slices, else get unmarked slices.</param>
    /// <returns>count of marked or unmarked slices</returns>
    public async Task<long> MarkedOrUnmarkedCount(string printId, bool marked)
    {
        var filter = Builders<SliceModel>.Filter.And(
            Builders<SliceModel>.Filter.Eq(s => s.printId, printId),
            Builders<SliceModel>.Filter.Eq(s => s.marked, marked)
        );
        return await _slices.CountDocumentsAsync(filter);
    }
    #endregion

    #region Getters
    public async Task<IEnumerable<SliceModel>> GetSlicesByPrintId(string printId)
    {
        var filter = Builders<SliceModel>.Filter.Eq(s => s.printId, printId);
        var slices = await _slices.Find(filter).SortBy(s => s.layer).ToListAsync();
        return slices;
    }

    public async Task<SliceModel> GetSliceBySliceId(string id)
    {
        var filter = Builders<SliceModel>.Filter.Eq(s => s.id, id);
        return await _slices.Find(filter).FirstOrDefaultAsync();
    }
    public async Task<SliceModel?> GetFirstSlice(PrintModel print)
    {
        if (print == null || print.sliceIds == null || !print.sliceIds.Any())
            return null;

        var filter = Builders<SliceModel>.Filter.In(s => s.id, print.sliceIds);
        var slices = await _slices.Find(filter).ToListAsync();

        // Find slice with file name starting with "0000"
        var firstSlice = slices.FirstOrDefault(s =>
            Path.GetFileNameWithoutExtension(s.filePath)?.StartsWith("0000") == true);

        return firstSlice;
    }
    /// <summary>
    /// Returns the first unmarked slice associated with the given print.
    /// If all slices are marked, returns the last slice.
    /// </summary>
    /// <param name="print">The print to retrieve the slice from.</param>
    /// <returns>The next <see cref="SliceModel"/> to process, or the last one if all are marked.</returns>
    public async Task<SliceModel?> GetNextSlice(PrintModel print)
    {
        if (print?.sliceIds == null || !print.sliceIds.Any())
            return null;

        var filter = Builders<SliceModel>.Filter.And(
            Builders<SliceModel>.Filter.In(s => s.id, print.sliceIds),
            Builders<SliceModel>.Filter.Eq(s => s.marked, false)
        );

        var slice = await _slices
            .Find(filter)
            .SortBy(s => s.layer)
            .FirstOrDefaultAsync();

        return slice;
    }
    public async Task<SliceModel?> GetLastSlice(PrintModel print)
    {
        if (print?.sliceIds == null || !print.sliceIds.Any())
            return null;

        var filter = Builders<SliceModel>.Filter.And(
            Builders<SliceModel>.Filter.In(s => s.id, print.sliceIds),
            Builders<SliceModel>.Filter.Eq(s => s.marked, true)
        );

        var slice = await _slices
            .Find(filter)
            .SortByDescending(s => s.layer)
            .FirstOrDefaultAsync();

        return slice;
    }
    public async Task<IEnumerable<SliceModel>> GetAllSlices()
    {
        var results = await _slices.Find(_ => true).ToListAsync();
        return results.AsEnumerable();
    }

    #endregion

    #region Checkers
    public async Task<bool> AllSlicesMarked(string printId)
    {
        var marked = false;
        return await MarkedOrUnmarkedCount(printId, marked) == 0;
    }
    #endregion

    #region CRUD
    public async Task AddSlice(SliceModel slice)
    {
        await _slices.InsertOneAsync(slice);
    }
    public async Task DeleteSlice(SliceModel slice)
    {
        var toDelete = Builders<SliceModel>.Filter.Eq(s => s.id, slice.id);
        await _slices.DeleteOneAsync(toDelete);
    }
    public async Task EditSlice(SliceModel slice)
    {
        var toEdit = Builders<SliceModel>.Filter.Eq(s => s.id, slice.id);
        await _slices.ReplaceOneAsync(toEdit, slice);
    }
    public async Task DeleteAllSlices()
    {
        await _slices.DeleteManyAsync(_ => true);
    }
    #endregion
}
