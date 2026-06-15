using System.Text.RegularExpressions;

namespace OptionsEdge.API.Infrastructure.Groww;

// Builds and parses Groww F&O trading symbols, e.g. "NIFTY26JUN24200CE"
// Format: {UNDERLYING}{YY}{MMM}{STRIKE}{CE|PE} — no day component.
public static partial class GrowwSymbolHelper
{
    private static readonly string[] MonthAbbr =
        ["JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"];

    [GeneratedRegex(@"^(?<underlying>NIFTY|BANKNIFTY)(?<yy>\d{2})(?<mmm>JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)(?<strike>\d+)(?<type>CE|PE)$")]
    private static partial Regex SymbolPattern();

    // e.g. FormatOptionSymbol("NIFTY", new DateOnly(2026, 6, 12), 24200, "CE") -> "NIFTY26JUN24200CE"
    public static string FormatOptionSymbol(string symbol, DateOnly expiry, int strike, string optionType)
    {
        var mmm = MonthAbbr[expiry.Month - 1];
        return $"{symbol.ToUpperInvariant()}{expiry.Year % 100:D2}{mmm}{strike}{optionType.ToUpperInvariant()}";
    }

    // Reverses FormatOptionSymbol — used to recognise Groww portfolio positions on import.
    // Groww's symbol carries no day, so the expiry is approximated as the last Tuesday
    // of the parsed month/year (NSE monthly expiry for NIFTY and BANKNIFTY since the
    // Sep 2025 Tuesday-expiry change and the Nov 2024 BANKNIFTY weekly discontinuation).
    public static bool TryParseOptionSymbol(string tradingSymbol, out (string Symbol, int Strike, string OptionType, DateOnly Expiry) result)
    {
        result = default;

        var match = SymbolPattern().Match(tradingSymbol.Trim().ToUpperInvariant());
        if (!match.Success)
            return false;

        int month = Array.IndexOf(MonthAbbr, match.Groups["mmm"].Value) + 1;
        int year = 2000 + int.Parse(match.Groups["yy"].Value);

        result = (
            match.Groups["underlying"].Value,
            int.Parse(match.Groups["strike"].Value),
            match.Groups["type"].Value,
            LastTuesdayOfMonth(year, month));
        return true;
    }

    // Shared by OptionsService (expiry list) and BacktestService (BANKNIFTY contract
    // expiry — monthly only since Nov 2024) to avoid duplicating this date math.
    public static DateOnly LastTuesdayOfMonth(int year, int month)
    {
        var lastDay = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        int offset = ((int)lastDay.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
        return lastDay.AddDays(-offset);
    }
}
