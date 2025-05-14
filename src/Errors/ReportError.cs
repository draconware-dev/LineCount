namespace LineCount.Errors;

public abstract record ReportError(string Message) : Error
{
    protected abstract string Name { get; }

    public override string ToString()
    {
        return $"""
            {Name}: 
            
            {Message}

            **PLEASE FILE AN ISSUE at 'https://github.com/draconware-dev/LineCount/issues/new' including above message.**
            """;
    }
}