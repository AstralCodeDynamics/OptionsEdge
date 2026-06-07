namespace OptionsEdge.API.Features.Chat;

public record ChatMessageRequest(Guid SessionId, string Message);

public record ChatHistoryItem(
    Guid Id,
    string Role,
    string Content,
    string? ModelUsed,
    int? InputTokens,
    int? OutputTokens,
    decimal? CostUsd,
    string CreatedAt);

public record ChatHistoryResponse(Guid SessionId, IReadOnlyList<ChatHistoryItem> Messages);

public record NewSessionResponse(Guid SessionId);

// SSE chunk streamed to the client. Type is one of: delta, done, error
public record ChatStreamChunk(
    string Type,
    string? Text = null,
    string? ModelUsed = null,
    int? InputTokens = null,
    int? OutputTokens = null,
    decimal? CostUsd = null,
    string? Error = null);
