namespace OptionsEdge.API.Common.Options;

public class LotSizeOptions
{
    public int NIFTY { get; set; }
    public int BANKNIFTY { get; set; }

    public int GetLotSize(string symbol) =>
        symbol.ToUpperInvariant() switch
        {
            "BANKNIFTY" => BANKNIFTY,
            _ => NIFTY,
        };
}
