using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.Database.Seeders;
public interface IMongoDbSeeder
{
    Task SeedDatabaseAsync();
    Task ClearDatabaseAsync(bool confirm);
}
