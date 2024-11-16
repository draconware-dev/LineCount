using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace LineCount;

public class LineCountData
{
    public required string? Filter { get; init; }
    public required Regex? LineFilter { get; init; }
    public required string? FilterNot { get; init; }
    public required Regex? LineFilterNot { get; init; }

    public CountType FilterType { get; private init; } = (CountType)(-1);
    public required bool ListFiles { get; init; }

    [SetsRequiredMembers]
    public LineCountData(string? filter, string? lineFilter, string? filterNot, string? lineFilterNot, bool listFiles)
    {
        Filter = filter;
        FilterNot = filterNot;
        ListFiles = listFiles;

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

        if (hasFilter)
        {
            FilterType = hasFilterNot ? CountType.FilteredBoth : CountType.Filtered;
        }
        else
        {
            FilterType = hasFilterNot ? CountType.FilteredExcept : CountType.Normal;
        }

        ListFiles = listFiles;
    }
}