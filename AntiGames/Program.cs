using AntiGames;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => { services.AddHostedService<Worker>(); })
    .UseWindowsService(options => { options.ServiceName = "AntiGames"; })
    .Build();
host.Run();