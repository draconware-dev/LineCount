using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace LineCount;

public class LineCountData
{
    public required string? Filter { get; init; }
    public required Regex? LineFilter { get; init; }
    public required string? FilterNot { get; init; }
    public required Regex? LineFilterNot { get; init; }

    public FilterType FilterType { get; } = (FilterType)(-1);
    public required bool ListFiles { get; init; }

    [SetsRequiredMembers]
    public LineCountData(string? filter, string? lineFilter, string? filterNot, string? lineFilterNot, bool listFiles)
    {
        Filter = filter;
        FilterNot = filterNot;
        ListFiles = listFiles;

        if (lineFilter is null)
        {
            FilterType = lineFilterNot is null ? FilterType.None : FilterType.FilteredExcept;
        }
        else
        {
            LineFilter = new Regex(lineFilter, RegexOptions.Singleline | RegexOptions.Compiled);
            FilterType = lineFilterNot is null ? FilterType.Filtered : FilterType.FilteredBoth;
        }

        if (lineFilterNot is not null)
        {
            LineFilterNot = new Regex(lineFilterNot, RegexOptions.Singleline | RegexOptions.Compiled);
        }

        ListFiles = listFiles;
    }
}