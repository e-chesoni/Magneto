using System.Text;

using Magneto.Desktop.WinUI.Core.Contracts.Services;

using Newtonsoft.Json;

namespace Magneto.Desktop.WinUI.Core.Services;

public class FileService : IFileService
{
    public T Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json);
        }

        return default;
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileContent = JsonConvert.SerializeObject(content);
        File.WriteAllText(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8);
    }

    public void Delete(string folderPath, string fileName)
    {
        if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }
    public IEnumerable<string> GetSliceFilesFromDirectoryPath(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return Enumerable.Empty<string>();

        // sorts files by name (ex. 0000_square.sjf will be first, followed by 0001_square.sjf, and so on)
        return Directory.GetFiles(directoryPath, "*.sjf")
            .OrderBy(f => f);
    }
}
