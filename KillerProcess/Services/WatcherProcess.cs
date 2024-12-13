using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices;
using System.IO.Pipes;
using System.Security.AccessControl;
using H.Pipes;
using KillerProcess.Shared;
using KillerProcess.Utils;

namespace KillerProcess.Services;

[SuppressMessage("Interoperability", "CA1416:Проверка совместимости платформы")]
public class WatcherProcess : BackgroundService
{
    private readonly ILogger<WatcherProcess> _logger;
    private readonly DisallowWordsConfiguration _disallowWordsConfiguration;
    private readonly Impersonation _impersonation;
    private readonly PipeServer<WindowChangeMessage> _pipeServer;
    private readonly string _path;
    
    public WatcherProcess(ILogger<WatcherProcess> logger, DisallowWordsConfiguration disallowWordsConfiguration, Impersonation impersonation)
    {
        _logger = logger;
        _disallowWordsConfiguration = disallowWordsConfiguration;
        _impersonation = impersonation;
        _pipeServer = new PipeServer<WindowChangeMessage>("ProcessKiller");
        _path = GetWindowLoggerPath();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RunWindowLogger();
        
        _pipeServer.CreatePipeStreamFunc = pipeName =>
        {
            var found = false;
            var userGroupName = "";
            var machine = new DirectoryEntry("WinNT://" + Environment.MachineName + ",Computer");
            foreach (DirectoryEntry child in machine.Children)
            {
                if (found || child.SchemaClassName != "Group" ||
                    child.Name is not ("Пользователи" or "Users" or "User")) continue;
                found = true;
                userGroupName = child.Name;
            }

            var ps = new PipeSecurity();
            ps.AddAccessRule(new PipeAccessRule(userGroupName,
                PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
            ps.AddAccessRule(new PipeAccessRule("SYSTEM", PipeAccessRights.FullControl, AccessControlType.Allow));

            return NamedPipeServerStreamAcl.Create(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                0,
                0,
                ps);
        };

        _pipeServer.MessageReceived += (_, args) =>
        {
            if (stoppingToken.IsCancellationRequested) return;
            _logger.LogInformation($"Window changed - user: {args.Message?.User}, window title: {args.Message?.WindowTitle}, process path: {args.Message?.ProcessPath}");
            KillIfContainsExplicitWordProcess(args.Message);
        };

        _pipeServer.ClientDisconnected += (_, _) =>
        {
            if (stoppingToken.IsCancellationRequested) return;
            _logger.LogWarning("Какой то пользователь попытался закрыть логгер, открываем заново");
            RunWindowLogger();
        };

        await _pipeServer.StartAsync(stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await _pipeServer.StopAsync(stoppingToken);
    }

    private void KillIfContainsExplicitWordProcess(WindowChangeMessage? argsMessage)
    {
        if(argsMessage == null) return;
        
        if(!_disallowWordsConfiguration.Configuration.RestrictedNtUsers.
               Any(user => argsMessage.User.Contains(user, StringComparison.InvariantCultureIgnoreCase))) return;
        
        if(!_disallowWordsConfiguration.Configuration.DisallowedProcesses.
               Any(process => argsMessage.ProcessName.Contains(process, StringComparison.InvariantCultureIgnoreCase))) return;
        
        if (!_disallowWordsConfiguration.Configuration.DisallowedWords.Any(word => argsMessage.WindowTitle
                .Contains(word, StringComparison.InvariantCultureIgnoreCase))) return;
        try
        {
            var currentProcess = Process.GetProcessById(argsMessage.ProcessId);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if(currentProcess is null) return;
            currentProcess.Kill();
        }
        catch 
        {
            //
        }
    }

    private string GetWindowLoggerPath()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KillerProcess.Watcher.exe");
        _logger.LogDebug("Window logger path: {path}", path);
        return path;
    }

    private void RunWindowLogger() => _impersonation.ExecuteAppAsLoggedOnUser(_path);
}