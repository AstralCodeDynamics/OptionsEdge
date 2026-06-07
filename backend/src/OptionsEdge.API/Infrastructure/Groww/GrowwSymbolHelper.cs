using System.Text.RegularExpressions;

namespace OptionsEdge.API.Infrastructure.Groww;

// Builds and parses Groww F&O trading symbols, e.g. "NIFTY26M1224200CE"
// Format: {UNDERLYING}{YY}{MONTH_CODE}{DD}{STRIKE}{CE|PE}
public static partial class GrowwSymbolHelper
{
    private static readonly Dictionary<int, char> MonthToCode = new()
    {
        [1] = 'J', [2] = 'F', [3] = 'H', [4] = 'A', [5] = 'K', [6] = 'M',
        [7] = 'N', [8] = 'Q', [9] = 'U', [10] = 'V', [11] = 'X', [12] = 'Z',
    };

    private static readonly Dictionary<char, int> CodeToMonth =
        MonthToCode.ToDictionary(kv => kv.Value, kv => kv.Key);

    [GeneratedRegex(@"^(?<underlying>NIFTY|BANKNIFTY)(?<yy>\d{2})(?<month>[A-Z])(?<dd>\d{2})(?<strike>\d+)(?<type>CE|PE)$")]
    private static partial Regex SymbolPattern();

    // e.g. FormatOptionSymbol("NIFTY", new DateOnly(2026, 6, 12), 24200, "CE") -> "NIFTY26M1224200CE"
    public static string FormatOptionSymbol(string symbol, DateOnly expiry, int strike, string optionType)
    {
        if (!MonthToCode.TryGetValue(expiry.Month, out var monthCode))
            throw new ArgumentOutOfRangeException(nameof(expiry), expiry, "Unsupported expiry month");

        return $"{symbol.ToUpperInvariant()}{expiry.Year % 100:D2}{monthCode}{expiry.Day:D2}{strike}{optionType.ToUpperInvariant()}";
    }

    // Reverses FormatOptionSymbol — used to recognise Groww portfolio positions on import
    public static bool TryParseOptionSymbol(string tradingSymbol, out (string Symbol, int Strike, string OptionType, DateOnly Expiry) result)
    {
        result = default;

        var match = SymbolPattern().Match(tradingSymbol.Trim().ToUpperInvariant());
        if (!match.Success || !CodeToMonth.TryGetValue(match.Groups["month"].Value[0], out var month))
            return false;

        int year = 2000 + int.Parse(match.Groups["yy"].Value);
        int day = int.Parse(match.Groups["dd"].Value);
        if (day < 1 || day > DateTime.DaysInMonth(year, month))
            return false;

        result = (
            match.Groups["underlying"].Value,
            int.Parse(match.Groups["strike"].Value),
            match.Groups["type"].Value,
            new DateOnly(year, month, day));
        return true;
    }
}
