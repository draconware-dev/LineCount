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

    public CountType FilterType { get; private init; } = (CountType)(-1);

    [SetsRequiredMembers]
    public LineCountData(string path, string? filter, string? lineFilter, string? filterNot, string? lineFilterNot)
    {
        Path = path;
        Filter = filter;
         FilterNot = filterNot;

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
            type = hasFilterNot ? CountType.FilteredExcept : CountType.Normal;
        }
    }
}