namespace Application.Helpers;

public static class DirectoryExtensions
{
    public static string GetSolutionDirectory()
    {
        var solutionDirectory = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(solutionDirectory) && Directory.GetFiles(solutionDirectory , "*.sln").Length == 0)
        {
            solutionDirectory = Directory.GetParent(solutionDirectory)?.FullName ?? string.Empty;
        }
        if (string.IsNullOrEmpty(solutionDirectory))
        {
            throw new DirectoryNotFoundException("Solution directory not found.");
        }
        return solutionDirectory;
    }
}