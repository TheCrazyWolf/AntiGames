using System.Diagnostics;

namespace AntiGames;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    string[] keywords = { "игры", "онлайн", "example" , "roblox", "games"};

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processes = Process.GetProcesses();

            var s = processes
                .Where(x => !string.IsNullOrEmpty(x.MainWindowTitle))
                .Where(x => keywords.Any(keyword => x.MainWindowTitle.ToLower().Contains(keyword.ToLower())));

            foreach (var process in s)
            {
                process.Kill();
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}