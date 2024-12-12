using System.Diagnostics;

namespace KillerProcess.Services;

public class WatcherProcess(
    ILogger<WatcherProcess> logger,
    DisallowWordsConfiguration disallowWordsConfiguration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processes = Process.GetProcesses()
                .Where(x => !string.IsNullOrEmpty(x.MainWindowTitle))
                .Where(x => disallowWordsConfiguration.DisallowWords
                    .Any(keyword => x.MainWindowTitle.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)));

            foreach (var process in processes)
            {
                string explicitWord = disallowWordsConfiguration.DisallowWords.FirstOrDefault(x =>
                    process.MainWindowTitle.Contains(x, StringComparison.CurrentCultureIgnoreCase)) ?? string.Empty;
                
                logger.LogInformation($"Process: {process.ProcessName}. Detected: {explicitWord}. Trying killing..");
                try
                {
                    process.Kill();
                    logger.LogInformation($"Process: {process.ProcessName} killed");
                }
                catch (Exception e)
                {
                    logger.LogInformation($"Process: {process.ProcessName} failed killing:");
                    logger.LogCritical(e.Message);
                }
            }

            await Task.Delay(3000, stoppingToken);
        }
    }
}