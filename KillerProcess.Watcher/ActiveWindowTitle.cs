using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using KillerProcess.Shared;

namespace KillerProcess.Watcher;

public class ActiveWindowTitle
{
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    public static WindowChangeMessage GetCaptionOfActiveWindow()
    {
        var wcm = new WindowChangeMessage()
        {
            WindowTitle = string.Empty
        };
        var handle = GetForegroundWindow();

        var intLength = GetWindowTextLength(handle) + 1;
        var titleSb = new StringBuilder(intLength);
        if (GetWindowText(handle, titleSb, intLength) > 0)
        {
            var title = titleSb.ToString();
            wcm.WindowTitle = string.IsNullOrWhiteSpace(title) ? "Unknown window" : title;
        }

        GetWindowThreadProcessId(handle, out var pid);
        var process = Process.GetProcessById((int)pid);
        try
        {
            wcm.ProcessId = process.Id;
            wcm.ProcessPath = process.MainModule?.FileName ?? "";
        }
        catch (Exception e)
        {
            wcm.ProcessPath = "";
        }

        return wcm;
    }
}