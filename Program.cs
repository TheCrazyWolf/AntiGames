using KillerProcess.Services;
using KillerProcess.Utils;
var serviceName = "文女四心" + "\u200B" + "\u200C" + "\u200D" + "\u200E";
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<WatcherProcess>();
        services.AddTransient<Impersonation>();
        services.AddSingleton<DisallowWordsConfiguration>();
    })
    .UseWindowsService(options => { options.ServiceName = serviceName; })
    .Build();
host.Run();