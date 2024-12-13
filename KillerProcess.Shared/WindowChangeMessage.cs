namespace KillerProcess.Shared;

[Serializable]
public class WindowChangeMessage
{
    public string User { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string ProcessPath { get; set; } = string.Empty;
}