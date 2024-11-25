namespace LunchAgent.Webhook;

public sealed record LunchAgentSettings
{
    public required string ConnectionString { get; init; }

    public required string Timing { get; init; }

    public int? MaximumMessageSize { get; init; }
}
