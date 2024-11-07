namespace LunchAgent.Core.Menus.Entities;

public sealed record HtmlClientSettings
{
    public int Attempts { get; set; }

    public int AttemptDelay { get; set; }
}
