namespace LineCount;

public static class LineCount
{
    public static async Task<int> GetLineCount(string path, string? filter, string[] excludeDirectories, string[] excludeFiles)
    {
       var (excludeFileNames, excludeFilePaths, excludeRelativeFilePaths) = ExcludePaths(excludeFiles);
       var (excludeDirectoryNames, excludeDirectoryPaths, excludeRelativeDirectoryPaths) = ExcludePaths(excludeDirectories);

        if (File.Exists(path))
        {
            return await GetFileLineCount(path, filter);
        }

        if (!Directory.Exists(path))
        {
            throw new 
        }

        excludeFiles ??= Array.Empty<string>();
        excludeDirectories ??= Array.Empty<string>();

        List<Task<int>> filetasks = new List<Task<int>>();
        foreach (var file in filter is not null ? Directory.GetFiles(path, filter) : Directory.GetFiles(path))
        {
            if (!Array.Exists(excludeFileNames, x => x == Path.GetFileName(file)) && !Array.Exists(excludeFileNames, x => x == Path.GetFullPath(file)))
            {
                filetasks.Add(GetFileLineCount(file, filter));
            }
        }

        var filetaskResults = await Task.WhenAll(filetasks);
        int rootlineCount = filetaskResults.Sum();

        List<Task<int>> directorytasks = new List<Task<int>>();

        foreach (var directory in Directory.GetDirectories(path))
        {
            if (!Array.Exists(excludeDirectories, x => x == Path.GetFileName(directory)))
            {
                directorytasks.Add(GetLineCount(directory, filter, excludeDirectories, excludeFiles));
            }
        }

        var directorytasksResult = await Task.WhenAll(directorytasks);
        int directoriescount = directorytasksResult.Sum();

        return rootlineCount + directoriescount;
    }

    public static async Task<int> GetFileLineCount(string path, string? filter = null)
    {
        if (!File.Exists(path))
        {
            return 0;
        }

        using FileStream stream = File.OpenRead(path);
        using StreamReader reader = new StreamReader(stream);

        string? line = await reader.ReadLineAsync();
        int count = 0;
        while (line is not null)
        {
            count++;
            line = await reader.ReadLineAsync();
        }
        return count;
    }

    static PathPatterns ExcludePaths(string[] excludeFiles)
    {
        List<string> excludeFileNames = new List<string>(excludeFiles.Length);
        List<string> excludeFilePaths = new List<string>(excludeFiles.Length);
        List<string> excludeRelativeFilePaths = new List<string>(excludeFiles.Length);

        foreach (string filename in excludeFiles)
        {
            if(!filename.Contains(Path.DirectorySeparatorChar) && !filename.Contains(Path.AltDirectorySeparatorChar))
            {
                excludeFileNames.Add(filename);
                continue;
            }

            if(Path.IsPathFullyQualified(filename))  
            {
                excludeFilePaths.Add(filename);
                continue;
            }
            if(filename.StartsWith("./"))
            {
                excludeRelativeFilePaths.Add(filename[2..]);
            }
        }
        return (excludeFileNames.ToArray(), excludeFilePaths.ToArray(), excludeRelativeFilePaths.ToArray());
    }
}
