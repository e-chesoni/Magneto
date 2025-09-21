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
using Magneto.Desktop.WinUI.Core.Models.States.PrintStates;

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

    public async Task CreatePrintInMongoDb(string sourcePath, PrintStateMachine.PrintMode printMode, int stlLayers) // TODO: need to pass number of slices here
    {
        // TODO: add conditional above this line; only need file stuff for 2d repeat
        //var printModeStl = false; // TODO: pass this in
        //var stlLayers = 100; // TODO: pass this in as an optional argument (default to 0? if print is 2D repeat)

        var printId = ObjectId.GenerateNewId().ToString(); // works because declared Bson on print model
        var sliceIds = new List<string>();
        int slices;
        List<string> files = new();
        string filePath;
        string fileName;
        string sourceFile; // for 3D

        if (printMode == PrintStateMachine.PrintMode.ThreeDStlSlice)
        {
            slices = stlLayers;
        }
        else
        {
            files = _fileService.GetFiles(sourcePath).ToList();
            if (!files.Any()) return;
            slices = files.Count();
        }
        for (var i = 0; i < slices; i++)
        {
            // gen slice id
            var sliceId = ObjectId.GenerateNewId().ToString();
            // add sliceId to sliceId list
            sliceIds.Add(sliceId);
            // get full path to job file
            if (printMode == PrintStateMachine.PrintMode.ThreeDStlSlice)
            {
                filePath = sourcePath;
                sourceFile = Path.GetFileNameWithoutExtension(sourcePath);
                fileName = Path.Combine(sourceFile, $"-slice-{i}"); // create file name
            }
            else
            {
                filePath = files[i];
                fileName = Path.GetFileName(filePath); // extract file name
            }
            // create a slice model
            var slice = new SliceModel
            {
                id = sliceId,
                printId = printId,
                layer = i,
                filePath = filePath, // full path (with file name at end)
                fileName = fileName, // fill name
                marked = false,
            };
            // add slices to slice collection using slice service
            await _sliceService.AddSlice(slice);
        }
        // get print directory
        var dirName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar));
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
            directoryPath = sourcePath, // TODO: You need to get the full path
            startTime = now,
            sliceIds = sliceIds,
        };
        await _printService.AddPrint(print);
    }
}

