using LunchAgent.Webhook;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(configurationBuilder =>
    {
        configurationBuilder
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile($"appsettings.{Environment.MachineName}.json", true, true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices(services =>
    {
        services.AddOptions<LunchAgentSettings>()
            .BindConfiguration("GoogleChat")
            .Validate(settings => !string.IsNullOrEmpty(settings.ConnectionString),
                """The configuration value "ConnectionString:GoogleChat" must be defined and not empty!""")
            .Validate(settings => !string.IsNullOrEmpty(settings.Timing),
                """The configuration value "ConnectionString:Timing" must be defined and not empty!""")
            .ValidateOnStart();

        services.AddHttpClient();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
