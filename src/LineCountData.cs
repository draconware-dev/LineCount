using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace LineCount;

public class LineCountData
{
    public string? Filter { get; }
    public Regex? LineFilter { get; }
    public string? ExcludeFilter { get; }
    public Regex? ExcludeLineFilter { get; }

    public FilterType FilterType { get; } = (FilterType)(-1);
    public required bool ListFiles { get; init; }
    public required Format Format { get; init; }

    public static readonly TimeSpan TimeOut = TimeSpan.FromMilliseconds(500);

    public LineCountData(string? filter, string? lineFilter, string? filterNot, string? lineFilterNot)
    {
        Filter = filter;
        ExcludeFilter = filterNot;
        
        if (lineFilter is null)
        {
            FilterType = lineFilterNot is null ? FilterType.None : FilterType.FilteredExcept;
        }
        else
        {
            LineFilter = new Regex(lineFilter, RegexOptions.Singleline | RegexOptions.Compiled, TimeOut);
            FilterType = lineFilterNot is null ? FilterType.Filtered : FilterType.FilteredBoth;
        }

        if (lineFilterNot is not null)
        {
            ExcludeLineFilter = new Regex(lineFilterNot, RegexOptions.Singleline | RegexOptions.Compiled, TimeOut);
        }
    }
}