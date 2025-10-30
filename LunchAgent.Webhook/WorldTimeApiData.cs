namespace LunchAgent.Webhook;

// https://worldtimeapi.org/pages/examples
public sealed record WorldTimeApiData
{
    public required string Datetime { get; init; }
}
