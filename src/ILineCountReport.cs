namespace Linecount;

public interface ILineCountReport
{
    int Files { get; }
    int Lines { get; }
    
    // C# ducktyping
    void Deconstruct(out int lines, out int files)
    {
        lines = Lines;
        files = Files;
    }
}