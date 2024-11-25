using LunchAgent.Core.Menus;
using LunchAgent.Core.Menus.Entities;
using LunchAgent.Core.Restaurants;
using Microsoft.Extensions.Options;
using NCrontab;

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
            var now = DateTime.UtcNow;
            var nextExecutionTime = schedule.GetNextOccurrence(now);
            var remainingTime = nextExecutionTime - now;

            logger.LogInformation("Next menu post will happen at {NextTime} UTC.", nextExecutionTime);
            logger.LogDebug("Next post in {Minutes} minutes.", Math.Round(remainingTime.TotalMinutes));

            await Task.Delay(remainingTime, stoppingToken);

            await PostMenus();
        }
    }

    private async Task PostMenus()
    {
        logger.LogInformation("PostMenus function started at: {Time}", DateTime.UtcNow);

        var restaurantService = new RestaurantService();
        var menuReadingService = new MenuReadingService(logger, htmlClientSettings.Value);

        var menus = menuReadingService
            .GetMenus(restaurantService.Get())
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
