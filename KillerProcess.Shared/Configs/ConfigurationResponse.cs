namespace KillerProcess.Shared.Configs;

public class ConfigurationResponse
{
    public IList<string> DisallowedWords { get; set; } = new List<string>();
    public IList<string> DisallowedProcesses { get; set; } = new List<string>();
    public IList<string> RestrictedNtUsers { get; set; } = new List<string>();
}