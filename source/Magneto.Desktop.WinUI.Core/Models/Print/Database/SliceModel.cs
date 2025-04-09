using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Magneto.Desktop.WinUI.Core.Models.Print.Database;
[MongoCollection("slices")]
public class SliceModel
{
    /// <summary>
    /// Unique id for slice
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string id
    {
        get; set;
    }

    /// <summary>
    /// Unique id of print this slice belongs to
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public string printId
    {
        get; set;
    }  // for reverse lookup

    /// <summary>
    /// Full path to the job file
    /// </summary>
    public string filePath
    {
        get; set;
    }

    /// <summary>
    /// Name of the job file
    /// </summary>
    public string fileName
    {
        get; set;
    }

    /// <summary>
    /// Slice layer
    /// </summary>
    public int layer
    {
        get; set;
    } // order of slice

    /// <summary>
    /// Slice thickness (null on initialization; update on mark)
    /// </summary>
    public double layerThickness
    {
        get; set;
    }

    /// <summary>
    /// Laser power used to mark slice (null on initialization; update on mark)
    /// </summary>
    public double power
    {
        get; set;
    }

    /// <summary>
    /// Scan speed used to mark slice (null on initialization; update on mark)
    /// </summary>
    public double scanSpeed
    {
        get; set;
    }

    /// <summary>
    /// Hatch spacing used to mark slice (null on initialization; update on mark)
    /// </summary>
    public double hatchSpacing
    {
        get; set;
    }

    /// <summary>
    /// Beam area used to mark slice (null on initialization; update on mark)
    /// </summary>
    public double beamArea
    {
        get; set;
    }

    /// <summary>
    /// Energy density of slice (null on initialization; calculated after mark)
    /// </summary>
    public double energyDensity
    {
        get; set;
    }

    /// <summary>
    /// Boolean indicating whether the slice has been marked on this print
    /// </summary>
    public bool marked
    {
        get; set;
    }

    /// <summary>
    /// Mark time (null on initialization; update on mark)
    /// </summary>
    public DateTime? markedAt
    {
        get; set;
    }
}