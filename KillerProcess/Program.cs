using KillerProcess.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<WatcherProcess>();
        services.AddSingleton<DisallowWordsConfiguration>();
    })
    .UseWindowsService(options => { options.ServiceName = "KillerProcess"; })
    .Build();
host.Run();