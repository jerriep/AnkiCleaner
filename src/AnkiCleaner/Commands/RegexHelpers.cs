using System.Text.RegularExpressions;

namespace AnkiCleaner.Commands;

public partial class RegexHelpers
{
    [GeneratedRegex(@"[^\u0E00-\u0E7F\s]")]
    public static partial Regex NonThaiWordsRegex();

    [GeneratedRegex(@"<div>(?<thaiword>[\u0E00-\u0E7F\s]*)(&nbsp;)?</div>")]
    public static partial Regex ThaiWordSurroundedByDiv();

    [GeneratedRegex(@"<span.*>(?<thaiword>[\u0E00-\u0E7F\s]*)(&nbsp;)?</span>")]
    public static partial Regex ThaiWordSurroundedBySpan();

    [GeneratedRegex(@"(?<thaiword>[\u0E00-\u0E7F\s]*)\s*\[[\u0E00-\u0E7F\s]*\]")]
    public static partial Regex ThaiWordWithClassifier();
}
