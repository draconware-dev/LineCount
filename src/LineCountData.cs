using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace LineCount;

public class LineCountData
{

    public required string Path { get; init; }
    public required string? Filter { get; init; }
    public required Regex? LineFilter { get; init; }
    public required string? FilterNot { get; init; }
    public required Regex? LineFilterNot { get; init; }

    public CountType FilterType 
    {  
        get
        {
            return (int)type == -1 ?
                (LineFilter is not null ?
                LineFilterNot is not null ?
                CountType.FilteredBoth
                : CountType.Filtered
                : LineFilterNot is not null ?
                CountType.FilteredExcept
                : CountType.Normal)
                : CountType.Normal;
        }
    }

    CountType type = (CountType)(-1);

    [SetsRequiredMembers]
    public LineCountData(string path, string? Filter, string? lineFilter, string? FilterNot, string? lineFilterNot)
    {
        this.Path = path;
        this.Filter = Filter;
        this.FilterNot = FilterNot;

        bool hasFilter = lineFilter is not null;
        bool hasFilterNot = lineFilterNot is not null;

        if (hasFilter)
        {
            LineFilter = new Regex(lineFilter!, RegexOptions.Singleline | RegexOptions.Compiled);
        }

        if (hasFilterNot)
        {
            LineFilterNot = new Regex(lineFilterNot!, RegexOptions.Singleline | RegexOptions.Compiled);
        }

        CountType type = hasFilter ? hasFilterNot ? CountType.FilteredBoth : CountType.Filtered : hasFilterNot ? CountType.FilteredExcept : CountType.Normal;
    }
}