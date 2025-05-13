namespace LineCount.Errors;

public abstract record Error : IError
{
    public abstract override string ToString();
}
