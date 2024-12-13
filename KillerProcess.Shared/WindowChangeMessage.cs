namespace KillerProcess.Shared;

[Serializable]
public class WindowChangeMessage
{
    public int ProcessId { get; set; }
    public string ProcessPath { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
}