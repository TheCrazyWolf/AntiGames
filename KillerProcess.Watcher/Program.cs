// See https://aka.ms/new-console-template for more information

using H.Pipes;
using KillerProcess.Shared;
using KillerProcess.Watcher;

var lastTitle = "";

var cancellationTokenSource = new CancellationTokenSource();
await using var namedPipeClient = new PipeClient<WindowChangeMessage>("ProcessKiller");

namedPipeClient.Disconnected += (_, _) =>
{
    Console.WriteLine("Disconnected");
    cancellationTokenSource.Cancel();
};
namedPipeClient.ExceptionOccurred += (_, args) =>
{
    Console.WriteLine(args.Exception);
    cancellationTokenSource.Cancel();
};

namedPipeClient.ConnectAsync().GetAwaiter().GetResult();
while (!cancellationTokenSource.Token.IsCancellationRequested)
{
    var currentWcm = ActiveWindowTitle.GetCaptionOfActiveWindow();
    if (lastTitle != currentWcm.WindowTitle)
    {
        Console.WriteLine(currentWcm.WindowTitle);
        lastTitle = currentWcm.WindowTitle;
        currentWcm.User = Environment.UserName;
        await namedPipeClient.WriteAsync(currentWcm);
    }

    await Task.Delay(1000);
}