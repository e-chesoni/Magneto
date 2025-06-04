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
    public async Task CreatePrintInMongoDb(string directoryPath)
    {
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
            var fullPath = files[i];

            // extract file name
            var fileName = Path.GetFileName(fullPath);

            // create a slice model
            var slice = new SliceModel
            {
                id = sliceId,
                printId = printId,
                layer = i,
                filePath = fullPath,
                fileName = fileName,
                marked = false,
            };
            // add slices to slice collection using slice service
            await _sliceService.AddSlice(slice);
        }

        // get print directory
        var dirName = Path.GetFileName(directoryPath.TrimEnd(Path.DirectorySeparatorChar));
        // get truncated timestamp for print name
        var dateStamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH:mm:ss");

        // create print name
        var printName = $"{dirName}_{dateStamp}";

        // create a print model and add slice ids to it
        var print = new PrintModel
        {
            id = printId,
            name = printName,
            directoryPath = directoryPath, // TODO: You need to get the full path
            startTime = DateTime.UtcNow,
            sliceIds = sliceIds,
        };
        await _printService.AddPrint(print);
    }
}

