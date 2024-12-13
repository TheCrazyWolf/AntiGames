using KillerProcess.Services;
using KillerProcess.Utils;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<WatcherProcess>();
        services.AddTransient<Impersonation>();
        services.AddSingleton<DisallowWordsConfiguration>();
    })
    .UseWindowsService(options => { options.ServiceName = "KillerProcess"; })
    .Build();
host.Run();