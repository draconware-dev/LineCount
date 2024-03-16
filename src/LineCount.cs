public static class LineCount
{ 
    public static async Task<int> GetLineCount(string path)
    {
        if (File.Exists(path))
        {
            return await GetFileLineCount(path);
        }

        if (!Directory.Exists(path))
        {
            return 0; 
        } 

        List<Task<int>> filetasks = new List<Task<int>>();

        foreach(var file in Directory.GetFiles(path))
        {
            filetasks.Add(GetFileLineCount(file)); 
        }

        var filetaskResults = await Task.WhenAll(filetasks);
        int rootlineCount = filetaskResults.Sum();

        List<Task<int>> directorytasks = new List<Task<int>>();

        foreach (var directory in Directory.GetDirectories(path))
        {
            directorytasks.Add(GetLineCount(directory));
        }

        var directorytasksResult = await Task.WhenAll(directorytasks);
        int directoriescount = directorytasksResult.Sum();

        return rootlineCount + directoriescount;
    }
    public static async Task<int> GetFileLineCount(string path)
    {
        if (!File.Exists(path))
        {
            return 0;
        }

        using FileStream stream = File.OpenRead(path);
        using StreamReader reader = new StreamReader(stream);

        string? line = await reader.ReadLineAsync();
        int count = 0;
        while(line is not null)
        {
            count++;
            line = await reader.ReadLineAsync();
        }
        return count;
    }
}