using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.Database;
public interface IMongoDbService
{
    void SetDatabase(string db);
    IMongoDatabase GetDatabase();
    IMongoCollection<T> GetCollection<T>();
}
