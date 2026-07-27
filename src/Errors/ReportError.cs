namespace Linecount.Errors;

public abstract class ReportError(string message) : IError
{
    protected abstract string Name { get; }

    public override string ToString()
    {
        return $"""
            {Name}: 
            
            {message}

            **PLEASE FILE AN ISSUE at 'https://github.com/draconware-dev/LoC/issues/new' including above message.**
            """;
    }
}