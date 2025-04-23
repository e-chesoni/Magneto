namespace Magneto.Desktop.WinUI.Core.Contracts.Services;

public interface IFileService
{
    #region RUD
    T Read<T>(string folderPath, string fileName);
    void Save<T>(string folderPath, string fileName, T content);
    void Delete(string folderPath, string fileName);
    #endregion

    #region Validation
    int ValidateFilePath(string directoryPath, string fileName);
    #endregion

    #region Finders
    int FindFile(string fileName);
    int FindDirectory(string fullPath);
    #endregion

    #region Getters
    IEnumerable<string> GetFiles(string directoryPath);
    int PrintDirectoryFiles(string targetDirectory);
    #endregion
}
