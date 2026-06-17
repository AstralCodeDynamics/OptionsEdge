namespace OptionsEdge.API.Common.Options;

public class LotSizeOptions
{
    public int NIFTY { get; set; }
    public int BANKNIFTY { get; set; }
    // Set this after each manual NSE lot-size review so the 90-day staleness check resets.
    public string? LastReviewedUtc { get; set; }

    public int GetLotSize(string symbol) =>
        symbol.ToUpperInvariant() switch
        {
            "BANKNIFTY" => BANKNIFTY,
            _ => NIFTY,
        };
}
