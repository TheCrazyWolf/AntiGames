using AntiGames;
using AntiGames.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<WatcherProccess>();
        services.AddSingleton<DisallowWordsConfiguration>();
    })
    .UseWindowsService(options => { options.ServiceName = "AntiGames"; })
    .Build();
host.Run();