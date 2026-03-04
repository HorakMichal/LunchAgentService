using LunchAgent.API;
using LunchAgent.Core.Menus;
using LunchAgent.Core.Menus.Entities;
using LunchAgent.Core.Restaurants;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{Environment.MachineName}.json", true, true)
    .AddEnvironmentVariables();

builder.Services.AddOptions<ApiSettings>()
    .BindConfiguration("GoogleChat")
    .Validate(settings => !string.IsNullOrEmpty(settings.ConnectionString),
        """The configuration value "ConnectionString:GoogleChat" must be defined and not empty!""")
    .ValidateOnStart();

builder.Services.AddOptions<HtmlClientSettings>()
    .BindConfiguration("HtmlClientSettings");

builder.Services.AddHttpClient();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();

app.MapGet("/postLunchMenu", PostLunchMenuMethod).WithName("PostLunchMenu");

app.Run();
return;

static async Task<Results<Ok, InternalServerError<string>>> PostLunchMenuMethod(
    IHttpClientFactory clientFactory, 
    IOptions<ApiSettings> settings,
    IOptions<HtmlClientSettings> htmlClientSettings,
    ILogger<Program> logger)
{
    try
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

        return TypedResults.Ok();
    }
    catch (Exception e)
    {
        return TypedResults.InternalServerError(e.Message);
    }
}