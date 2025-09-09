using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database.Seeders;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Print.Database;
using MongoDB.Bson;

namespace Magneto.Desktop.WinUI.Core.Services.Database.Seeders;
public class PrintSeeder : IPrintSeeder
{
    private readonly IFileService _fileService;
    private readonly IPrintService _printService;
    private readonly ISliceService _sliceService;
    public PrintSeeder(IFileService fileService, IPrintService printService, ISliceService sliceService)
    {
        _fileService = fileService;
        _printService = printService;
        _sliceService = sliceService;
    }

    public async Task CreatePrintInMongoDb(string directoryPath) // TODO: need to pass number of slices here
    {
        // TODO: add conditional above this line; only need file stuff for 2d repeat

        var files = _fileService.GetFiles(directoryPath).ToList();
        if (!files.Any()) return;

        var printId = ObjectId.GenerateNewId().ToString(); // works because declared Bson on print model
        var sliceIds = new List<string>();

        for (var i = 0; i < files.Count(); i++)
        {
            // gen slice id
            var sliceId = ObjectId.GenerateNewId().ToString();

            // add sliceId to sliceId list
            sliceIds.Add(sliceId);

            // get full path to job file
            var filePath = files[i];

            // extract file name
            var fileName = Path.GetFileName(filePath);

            // create a slice model
            var slice = new SliceModel
            {
                id = sliceId,
                printId = printId,
                layer = i,
                filePath = filePath, // TODO: in 3D slice version, use STL file path
                fileName = fileName, // TODO: in 3D slice version, generate name like [stl_filename]_slice_[number]
                marked = false,
            };
            // add slices to slice collection using slice service
            await _sliceService.AddSlice(slice);
        }

        // get print directory
        var dirName = Path.GetFileName(directoryPath.TrimEnd(Path.DirectorySeparatorChar));
        // get truncated timestamp for print name
        var now = DateTime.UtcNow;
        var dateStamp = now.ToLocalTime().ToString("yyyy-MM-dd_HH:mm:ss");
        // create print name
        var printName = $"{dirName}_{dateStamp}";

        // create a print model and add slice ids to it
        var print = new PrintModel
        {
            id = printId,
            name = printName,
            directoryPath = directoryPath, // TODO: You need to get the full path
            startTime = now,
            sliceIds = sliceIds,
        };
        await _printService.AddPrint(print);
    }
}

