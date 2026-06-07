namespace OptionsEdge.API.Features.Billing;

public record TopUpRequest(decimal Amount);

public record TopUpResponse(string Message);
