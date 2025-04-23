using System.Text;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Newtonsoft.Json;

namespace Magneto.Desktop.WinUI.Core.Services;

public class FileService : IFileService
{
    #region RUD
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
    #endregion

    #region Validation
    public int ValidateFilePath(string directoryPath, string fileName)
    {
        MagnetoLogger.Log("Generating full path...", LogFactoryLogLevel.LogLevel.VERBOSE);
        var fullPath = Path.Combine(directoryPath, fileName);
        return ValidateFilePath(fullPath);
    }
    public int ValidateFilePath(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            var msg = $"File not found. File path {fullPath} is invalid";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        return 1;
    }
    #endregion

    #region Finders
    public int FindFile(string fileName)
    {
        // Check if the file exists
        if (!File.Exists(fileName))
        {
            // TODO: Use Log & Display once it's extrapolated from TestPrintPage.xaml.cs
            var msg = $"Could not find: {fileName}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        else
        {
            var msg = $"Found file: {fileName}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            return 1;
        }
    }
    public int FindDirectory(string fullPath)
    {
        // Log the target directory
        var msg = $"Target Directory: {fullPath}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        // Check if the directory exists
        if (!Directory.Exists(fullPath))
        {
            msg = "Directory does not exist.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        else
        {
            return 1;
        }
    }
    #endregion

    #region Getters
    public IEnumerable<string> GetFiles(string directoryPath) // Requires full directory path as input
    {
        if (!Directory.Exists(directoryPath))
            return Enumerable.Empty<string>();
        // sorts files by name (ex. 0000_square.sjf will be first, followed by 0001_square.sjf, and so on)
        return Directory.GetFiles(directoryPath, "*.sjf")
            .OrderBy(f => f);
    }
    public int PrintDirectoryFiles(string targetDirectory)
    {
        string msg;
        if (!Directory.Exists(targetDirectory))
        {
            msg = "Directory does not exist. Cannot print files.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        // Get all file names in the directory
        var fileEntries = Directory.GetFiles(targetDirectory);
        foreach (var fileName in fileEntries)
        {
            msg = $"File: {fileName}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        }
        return 1;
    }
    #endregion
}
