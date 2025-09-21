using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Print.Database;
using Magneto.Desktop.WinUI.Core.Models.States.PrintStates;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.Database.Seeders;
public interface IPrintSeeder
{
    Task CreatePrintInMongoDb(string fullPath, PrintStateMachine.PrintMode printMode, int stlLayers = 1);
}
