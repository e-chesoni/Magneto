using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database.Seeders;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database;
using Magneto.Desktop.WinUI.Core.Models.Print.Database;
using MongoDB.Bson;

namespace Magneto.Desktop.WinUI.Core.Services.Database.Seeders;
public class MongoDbSeeder : IMongoDbSeeder
{
    private readonly IPrintService _printService;
    private readonly ISliceService _sliceService;
    public MongoDbSeeder(IPrintService printService, ISliceService sliceService)
    {
        _printService = printService;
        _sliceService = sliceService;
    }
    public async Task SeedDatabaseAsync()
    {
        var random = new Random();

        var existing = await _printService.TotalPrintsCount();
        if (existing > 0)
        {
            Debug.WriteLine($"DB already seeded. Database has {existing} prints");
            return;
        }

        Debug.WriteLine("No prints found. Generating seed data...");

        var fakePrints = new List<PrintModel>();
        var fakeSlices = new List<SliceModel>();

        foreach (var i in Enumerable.Range(1, 20))
        {
            var startTime = DateTime.UtcNow.AddDays(-random.Next(1, 30));
            var printId = ObjectId.GenerateNewId().ToString();
            var numSlices = random.Next(100, 200);
            var sliceIds = new List<string>();

            // constants
            var power = 200.0;
            var layerThickness = 0.05;
            var scanSpeed = 800.0;
            var hatchSpacing = 0.12;
            var energyDensity = power / (layerThickness * scanSpeed * hatchSpacing);

            for (var j = 0; j < numSlices; j++)
            {
                var sliceId = ObjectId.GenerateNewId().ToString();
                sliceIds.Add(sliceId);

                var markedTime = startTime.AddMinutes(j * 2); // ✅ 2-minute intervals

                fakeSlices.Add(new SliceModel
                {
                    id = sliceId,
                    printId = printId,
                    filePath = $"C:\\prints\\print{i}\\slice{j}.png",
                    layer = j,
                    layerThickness = layerThickness,
                    power = power,
                    scanSpeed = scanSpeed,
                    hatchSpacing = hatchSpacing,
                    beamArea = 0.01,
                    energyDensity = energyDensity,
                    marked = true,
                    markedAt = markedTime
                });
            }

            var endTime = startTime.AddMinutes(numSlices * 2); // ✅ duration based on slice count

            fakePrints.Add(new PrintModel
            {
                id = printId,
                directoryPath = $"C:\\prints\\print{i}",
                startTime = startTime,
                endTime = endTime,
                sliceIds = sliceIds,
                complete = true,
            });
        }

        foreach (var print in fakePrints)
        {
            await _printService.AddPrint(print);
        }

        foreach (var slice in fakeSlices)
        {
            await _sliceService.AddSlice(slice);
        }

        Debug.WriteLine($"✅ Inserted {fakePrints.Count} prints and {fakeSlices.Count} slices.");
    }

    public async Task ClearDatabaseAsync(bool confirm = false)
    {
#if !DEBUG
            throw new InvalidOperationException("ClearDatabaseAsync is only allowed in DEBUG mode.");
#endif

        if (!confirm)
        {
            Debug.WriteLine("⚠️ ClearDatabaseAsync was called without confirmation. No action taken.");
            return;
        }

        Debug.WriteLine("⛔ Clearing MongoDB collections...");

        await _printService.DeleteAllPrints();
        await _sliceService.DeleteAllSlices();

        Debug.WriteLine("✅ Collections cleared.");
    }
}

