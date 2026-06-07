using Microsoft.EntityFrameworkCore;
using OptionsEdge.API.Common.Constants;
using OptionsEdge.API.Domain.Entities;
using OptionsEdge.API.Infrastructure.Data;
using OptionsEdge.API.Infrastructure.Groww;

namespace OptionsEdge.API.Features.Groww;

public class GrowwOrderService(
    GrowwApiClient groww,
    AppDbContext db,
    ILogger<GrowwOrderService> logger)
{
    public async Task<PlaceOrderResponse> PlaceOrderAsync(Guid userId, PlaceOrderRequest req, CancellationToken ct = default)
    {
        if (!DateOnly.TryParse(req.Expiry, out var expiry))
            throw new ArgumentException("Invalid expiry date");

        if (req.Symbol.ToUpper() is not ("NIFTY" or "BANKNIFTY"))
            throw new ArgumentException("Symbol must be NIFTY or BANKNIFTY");

        if (req.OptionType.ToUpper() is not ("CE" or "PE"))
            throw new ArgumentException("OptionType must be CE or PE");

        if (req.Quantity <= 0)
            throw new ArgumentException("Quantity must be at least 1 lot");

        var symbol = req.Symbol.ToUpper();
        var optionType = req.OptionType.ToUpper();
        var tradingSymbol = GrowwSymbolHelper.FormatOptionSymbol(symbol, expiry, req.Strike, optionType);
        int lotSize = symbol == "BANKNIFTY" ? AppConstants.LotSizes.BankNifty : AppConstants.LotSizes.Nifty;

        var referenceId = req.PositionId is { } pid
            ? $"OE-{pid:N}"[..11]
            : $"OE-{Guid.NewGuid():N}"[..11];

        var order = new GrowwOrderRequest(
            TradingSymbol: tradingSymbol,
            Quantity: req.Quantity * lotSize,
            Price: req.OrderType.Equals("MARKET", StringComparison.OrdinalIgnoreCase) ? 0 : req.Price,
            Validity: "DAY",
            Exchange: "NSE",
            Segment: "FNO",
            Product: "NRML",
            OrderType: req.OrderType.ToUpper(),
            TransactionType: req.TransactionType.ToUpper(),
            OrderReferenceId: referenceId);

        var result = await groww.PlaceOrderAsync(order, ct);
        logger.LogInformation("Placed Groww order {OrderId} ({Status}) for {Symbol} qty {Qty}",
            result.OrderId, result.Status, tradingSymbol, order.Quantity);

        return new PlaceOrderResponse(result.OrderId, result.Status, tradingSymbol, order.Quantity);
    }

    public async Task<bool> CancelOrderAsync(string orderId, CancellationToken ct = default)
    {
        var result = await groww.CancelOrderAsync(orderId, "FNO", ct);
        return result.Status is "CANCELLED" or "CANCELLED_AT_EXCHANGE" or "CANCELLATION_REQUESTED";
    }

    // Pulls open F&O positions from the Groww portfolio and creates local Position rows
    // for any not already tracked. Trading symbols are reverse-parsed via GrowwSymbolHelper
    // and matched against existing active positions by (symbol, strike, optionType, expiry).
    public async Task<int> ImportPositionsFromGrowwAsync(Guid userId, CancellationToken ct = default)
    {
        IReadOnlyList<GrowwPosition> growwPositions;
        try
        {
            growwPositions = await groww.GetPositionsAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch Groww positions for import");
            return 0;
        }

        var existing = await db.Positions
            .Where(p => p.UserId == userId && p.Status == "active")
            .ToListAsync(ct);

        int imported = 0;
        foreach (var gp in growwPositions)
        {
            if (gp.Quantity == 0) continue;
            if (!GrowwSymbolHelper.TryParseOptionSymbol(gp.TradingSymbol, out var parsed)) continue;

            bool alreadyTracked = existing.Any(p =>
                p.Symbol == parsed.Symbol &&
                p.Strike == parsed.Strike &&
                p.OptionType == parsed.OptionType &&
                p.Expiry == parsed.Expiry);
            if (alreadyTracked) continue;

            int lotSize = parsed.Symbol == "BANKNIFTY" ? AppConstants.LotSizes.BankNifty : AppConstants.LotSizes.Nifty;
            int lots = Math.Max(1, Math.Abs(gp.Quantity) / lotSize);

            db.Positions.Add(new Position
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Symbol = parsed.Symbol,
                Strike = parsed.Strike,
                OptionType = parsed.OptionType,
                Expiry = parsed.Expiry,
                EntryPrice = gp.AvgPrice,
                Quantity = lots,
                StopLoss = Math.Round(gp.AvgPrice * 0.65m, 2),
                Target1 = Math.Round(gp.AvgPrice * 1.5m, 2),
                Status = "active",
                CreatedAt = DateTimeOffset.UtcNow,
            });
            imported++;
        }

        if (imported > 0)
            await db.SaveChangesAsync(ct);

        logger.LogInformation("Imported {Count} positions from Groww for user {UserId}", imported, userId);
        return imported;
    }
}
