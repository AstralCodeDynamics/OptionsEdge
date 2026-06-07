using OptionsEdge.API.Infrastructure.Background;

namespace OptionsEdge.API.Tests;

// Verifies the IST market-hours gate that MarketDataWorker and AISignalService
// rely on to decide whether to poll/generate during a given tick.
// IST = UTC+5:30, so each scenario is expressed as its UTC equivalent.
public class MarketHoursHelperTests
{
    private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    [Fact]
    public void PreMarket_NineFourteenIst_IsClosed()
    {
        // 2026-06-08 (Monday) 09:14 IST == 03:44 UTC
        var utcNow = Utc(2026, 6, 8, 3, 44);

        Assert.False(MarketHoursHelper.IsMarketOpen(utcNow));
    }

    [Fact]
    public void MarketOpen_NineSixteenIst_IsOpen()
    {
        // 2026-06-08 (Monday) 09:16 IST == 03:46 UTC
        var utcNow = Utc(2026, 6, 8, 3, 46);

        Assert.True(MarketHoursHelper.IsMarketOpen(utcNow));
    }

    [Fact]
    public void LastMinute_ThreeTwentyNinePmIst_IsOpen()
    {
        // 2026-06-08 (Monday) 15:29 IST == 09:59 UTC
        var utcNow = Utc(2026, 6, 8, 9, 59);

        Assert.True(MarketHoursHelper.IsMarketOpen(utcNow));
    }

    [Fact]
    public void JustAfterClose_ThreeThirtyOnePmIst_IsClosed()
    {
        // 2026-06-08 (Monday) 15:31 IST == 10:01 UTC
        var utcNow = Utc(2026, 6, 8, 10, 1);

        Assert.False(MarketHoursHelper.IsMarketOpen(utcNow));
    }

    [Fact]
    public void Weekend_SaturdayDuringWeekdayMarketHours_IsClosed()
    {
        // 2026-06-06 is a Saturday; 11:00 IST == 05:30 UTC, well within weekday market hours
        var utcNow = Utc(2026, 6, 6, 5, 30);

        Assert.False(MarketHoursHelper.IsMarketOpen(utcNow));
    }
}
