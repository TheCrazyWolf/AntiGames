using System.Diagnostics;
using AntiGames.Services;

namespace AntiGames;

public class Worker(ILogger<Worker> logger, DisallowWordsConfiguration disallowWordsConfiguration)
    : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processes = Process.GetProcesses();

            var s = processes
                .Where(x => !string.IsNullOrEmpty(x.MainWindowTitle))
                .Where(x => disallowWordsConfiguration.DisallowWords
                    .Any(keyword => x.MainWindowTitle.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)));

            foreach (var process in s)
            {
                process.Kill();
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}