using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Attributes;
/// <summary>
/// Light weight class to allow MongoDB seeder to interpret collection names
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MongoCollectionAttribute : Attribute
{
    public string Name
    {
        get;
    }

    public MongoCollectionAttribute(string name)
    {
        Name = name;
    }
}