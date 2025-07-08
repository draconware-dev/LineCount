using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace LineCount;

public class LineCountData
{
    public Regex? Filter { get; }
    public Regex? LineFilter { get; }
    public Regex? ExcludeFilter { get; }
    public Regex? ExcludeLineFilter { get; }

    public FilterType FilterType { get; } = (FilterType)(-1);
    public required bool ListFiles { get; init; }

    public static readonly TimeSpan TimeOut = TimeSpan.FromMilliseconds(500);

    public LineCountData(string? filter, string? lineFilter, string? filterNot, string? lineFilterNot)
    {
        if(filter is not null)
        {
            string filterPattern = Globbing.ToRegex(filter);
            Filter = new Regex(filterPattern, RegexOptions.Singleline | RegexOptions.Compiled, TimeOut);
        }

        if (filterNot is not null)
        {
            string filterNotPattern = Globbing.ToRegex(filterNot);
            ExcludeFilter = new Regex(filterNotPattern, RegexOptions.Singleline | RegexOptions.Compiled, TimeOut);
        }

        if (lineFilter is null)
        {
            FilterType = lineFilterNot is null ? FilterType.None : FilterType.FilteredExcept;
        }
        else
        {
            FilterType = lineFilterNot is null ? FilterType.Filtered : FilterType.FilteredBoth;
            LineFilter = new Regex(lineFilter, RegexOptions.Singleline | RegexOptions.Compiled, TimeOut);
        }

        if (lineFilterNot is not null)
        {
            ExcludeLineFilter = new Regex(lineFilterNot, RegexOptions.Singleline | RegexOptions.Compiled, TimeOut);
        }
    }
}