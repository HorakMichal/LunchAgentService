namespace LunchAgent.API;

public sealed record ApiSettings
{
    public required string ConnectionString { get; init; }

    public int? MaximumMessageSize { get; init; }
}
