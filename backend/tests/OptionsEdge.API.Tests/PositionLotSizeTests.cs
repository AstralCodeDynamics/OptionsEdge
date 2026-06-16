using OptionsEdge.API.Common.Options;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Features.Positions;

namespace OptionsEdge.API.Tests;

public class PositionLotSizeTests
{
    [Fact]
    public void CalculatePnL_UsesConfiguredNiftyLotSize()
    {
        var lotSizes = new TestOptionsMonitor<LotSizeOptions>(new LotSizeOptions
        {
            NIFTY = 65,
            BANKNIFTY = 30,
        });
        var service = new PositionService(lotSizes);
        var position = CreatePosition("NIFTY", entryPrice: 100m, quantity: 1);

        var pnl = service.CalculatePnL(position, currentLtp: 110m);

        Assert.Equal(650m, pnl);
    }

    [Fact]
    public void CalculatePnL_PicksUpUpdatedLotSizeWithoutCodeChange()
    {
        var lotSizes = new TestOptionsMonitor<LotSizeOptions>(new LotSizeOptions
        {
            NIFTY = 65,
            BANKNIFTY = 30,
        });
        var service = new PositionService(lotSizes);
        var position = CreatePosition("NIFTY", entryPrice: 100m, quantity: 1);

        var initialPnL = service.CalculatePnL(position, currentLtp: 110m);

        lotSizes.Set(new LotSizeOptions
        {
            NIFTY = 75,
            BANKNIFTY = 30,
        });

        var updatedPnL = service.CalculatePnL(position, currentLtp: 110m);

        Assert.Equal(650m, initialPnL);
        Assert.Equal(750m, updatedPnL);
    }

    [Fact]
    public void CalculatePnL_UsesConfiguredBankNiftyLotSize()
    {
        var lotSizes = new TestOptionsMonitor<LotSizeOptions>(new LotSizeOptions
        {
            NIFTY = 65,
            BANKNIFTY = 30,
        });
        var service = new PositionService(lotSizes);
        var position = CreatePosition("BANKNIFTY", entryPrice: 200m, quantity: 2);

        var pnl = service.CalculatePnL(position, currentLtp: 210m);

        Assert.Equal(600m, pnl);
    }

    private static Position CreatePosition(string symbol, decimal entryPrice, int quantity) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Symbol = symbol,
        Strike = symbol == "BANKNIFTY" ? 52000 : 24000,
        OptionType = "CE",
        Expiry = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
        EntryPrice = entryPrice,
        Quantity = quantity,
        StopLoss = entryPrice * 0.8m,
        Target1 = entryPrice * 1.2m,
        Status = "active",
        CreatedAt = DateTimeOffset.UtcNow,
    };
}
