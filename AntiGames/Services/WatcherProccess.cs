using System.Diagnostics;

namespace AntiGames.Services;

public class WatcherProccess(ILogger<WatcherProccess> logger, DisallowWordsConfiguration disallowWordsConfiguration)
    : BackgroundService
{
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
                try
                {
                    process.Kill();
                }
                catch (Exception e)
                {
                    logger.LogCritical(e.Message);
                }
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}