using Magneto.Desktop.WinUI.Core.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Magneto.Desktop.WinUI.Core.Models.Print.Database;
[MongoCollection("prints")]
public class PrintModel
{
    /// <summary>
    /// Unique id for print
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string id
    {
        get; set;
    }

    /// <summary>
    /// Name of the directory containing all the slices for this print
    /// </summary>
    public string name
    {
        get; set;
    }

    /// <summary>
    /// Full path to the directory containing slices for this print
    /// </summary>
    public string directoryPath
    {
        get; set;
    }

    /// <summary>
    /// Time print stated
    /// </summary>
    public DateTime startTime
    {
        get; set;
    }

    /// <summary>
    /// Time print ends (null on initialization)
    /// </summary>
    [BsonIgnoreIfNull]
    public DateTime? endTime
    {
        get; set;
    }

    /// <summary>
    /// Duration of print (null on initialization)
    /// </summary>
    [BsonIgnoreIfNull]
    public TimeSpan? duration => endTime.HasValue && endTime.Value > startTime
                                ? endTime.Value - startTime
                                : null;

    /// <summary>
    /// List of slice ids
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> sliceIds
    {
        get; set;
    }

    /// <summary>
    /// Total slices associated with this print (based on the number of ids in the sliceId list)
    /// </summary>
    public int totalSlices => sliceIds?.Count ?? 0;

    /// <summary>
    /// Boolean indicating whether print is complete (false on initialization)
    /// </summary>
    public bool complete
    {
        get; set;
    }
}


