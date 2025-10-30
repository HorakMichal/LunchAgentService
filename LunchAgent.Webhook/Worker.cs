using LunchAgent.Core.Menus;
using LunchAgent.Core.Menus.Entities;
using LunchAgent.Core.Restaurants;
using Microsoft.Extensions.Options;
using NCrontab;
using System.Net.Http.Json;

namespace LunchAgent.Webhook;

public sealed class Worker(
    ILogger<Worker> logger, 
    IOptions<LunchAgentSettings> settings,
    IOptions<HtmlClientSettings> htmlClientSettings,
    IHttpClientFactory clientFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var schedule = CrontabSchedule.Parse(settings.Value.Timing);
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = await GetTime();
            var nextExecutionTime = schedule.GetNextOccurrence(now);
            var remainingTime = nextExecutionTime - now;

            logger.LogInformation("Next menu post will happen at {NextTime} UTC.", nextExecutionTime);
            logger.LogDebug("Next post in {Minutes} minutes.", Math.Round(remainingTime.TotalMinutes));

            await Task.Delay(remainingTime, stoppingToken);

            await PostMenus();
        }
    }

    // Because of issues with time changes, we get try getting time from the internet
    private async Task<DateTime> GetTime()
    {
        var client = clientFactory.CreateClient();

        HttpResponseMessage? response;

        try
        {
            response = await client.GetAsync("https://worldtimeapi.org/api/timezone/Europe/Prague");
        }
        catch (Exception e)
        {
            logger.LogError("GetAsync to World Time Api failed with: {Exception}", e);
            return DateTime.UtcNow;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Request to World Time Api ended with code: {Code}. Using local time", response.StatusCode);
            return DateTime.UtcNow;
        }

        var result = await response.Content.ReadFromJsonAsync<WorldTimeApiData>();
        if (result is null)
        {
            logger.LogError("Could not read World Time Api response. Using local time");
            return DateTime.UtcNow;
        }

        if (DateTime.TryParse(result.Datetime, out var datetime)) 
            return datetime;

        logger.LogError("Could not parse datetime. Using local time");
        return DateTime.UtcNow;

    }

    private async Task PostMenus()
    {
        logger.LogInformation("PostMenus function started at: {Time}", DateTime.UtcNow);

        var restaurantService = new RestaurantService();
        var menuReadingService = new MenuReadingService(logger, htmlClientSettings.Value);

        var menus = (await menuReadingService
            .GetMenus(restaurantService.Get()))
            .CreateMenuMessages(settings.Value.MaximumMessageSize ?? int.MaxValue);

        var client = clientFactory.CreateClient();

        foreach (var menu in menus)
        {
            var response = await client.PostAsync(settings.Value.ConnectionString, menu
                .CreateHttpRequest());
        
            if(!response.IsSuccessStatusCode)
                logger.LogError("Request to Google Chat ended with code: {Code}", response.StatusCode);

            await Task.Delay(1000);
        }
        
        logger.LogInformation("PostMenus function ended at: {Time}", DateTime.UtcNow);
    }
}
