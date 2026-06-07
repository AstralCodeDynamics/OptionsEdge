namespace OptionsEdge.API.Features.Billing;

public static class BillingEndpoints
{
    public static void MapBillingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/billing");

        // POST /api/v1/billing/topup
        // Placeholder until Razorpay integration lands — accepts the request shape
        // the frontend will eventually send, but performs no wallet mutation.
        group.MapPost("/topup", (TopUpRequest req) =>
            Results.Ok(new TopUpResponse(
                $"Wallet top-up of ₹{req.Amount:0.##} isn't available yet. Razorpay integration is coming soon.")))
            .WithName("TopUpWallet");
    }
}
