using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace KillerProcess.Utils;

[SuppressMessage("Interoperability", "CA1416:Проверка совместимости платформы")]
public class Impersonation
{
    #region DLL Imports

    internal const int SE_PRIVILEGE_ENABLED = 0x00000002;
    internal const int TOKEN_QUERY = 0x00000008;
    internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
    internal const int TOKEN_ASSIGN_PRIMARY = 0x0001;
    internal const int TOKEN_DUPLICATE = 0x0002;
    internal const int TOKEN_IMPERSONATE = 0X00000004;
    internal const int TOKEN_ADJUST_DEFAULT = 0x0080;
    internal const int TOKEN_ADJUST_SESSIONID = 0x0100;
    internal const int MAXIMUM_ALLOWED = 0x2000000;
    internal const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    internal const int NORMAL_PRIORITY_CLASS = 0x20;
    internal const int CREATE_NEW_CONSOLE = 0x00000010;
    internal const int CREATE_NO_WINDOW = 0x08000000;

    // private static WindowsImpersonationContext impersonatedUser;
    const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TokPriv1Luid
    {
        public int Count;
        public long Luid;
        public int Attr;
    }

    struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    struct STARTUPINFO
    {
        public uint cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);

    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
    internal static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall, ref TokPriv1Luid newst, int len,
        IntPtr prev, IntPtr relen);

    [DllImport("kernel32", SetLastError = true), SuppressUnmanagedCodeSecurity]
    static extern bool CloseHandle(IntPtr handle);

    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
    internal static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);

    [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
    static extern bool DuplicateTokenEx(IntPtr hExistingToken, Int32 dwDesiredAccess,
        ref SECURITY_ATTRIBUTES lpThreadAttributes,
        Int32 ImpersonationLevel, Int32 dwTokenType,
        ref IntPtr phNewToken);

    [DllImport("userenv.dll", SetLastError = true)]
    static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CreateProcessAsUser(
        IntPtr hToken,
        string lpApplicationName,
        string lpCommandLine,
        ref SECURITY_ATTRIBUTES lpProcessAttributes,
        ref SECURITY_ATTRIBUTES lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    #endregion

    private readonly ILogger<Impersonation> _logger;

    public Impersonation(ILogger<Impersonation> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Duplicates the token information derived
    /// from the logged in user's credentials. This
    /// is required to run the application on the
    /// logged in users desktop.
    /// </summary>
    /// <returns>Returns true if the application was successfully started in the user's desktop.</returns>
    public bool ExecuteAppAsLoggedOnUser(string appName, string? cmdLineArgs = null)
    {
        _logger.LogDebug("In ExecuteAppAsLoggedOnUser for all users.");
        var loggedInUserToken = IntPtr.Zero;
        var duplicateToken = IntPtr.Zero;
        var shellProcessToken = IntPtr.Zero;

        if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES, ref loggedInUserToken))
        {
            _logger.LogDebug("OpenProcessToken failed: " + Marshal.GetLastWin32Error());
            return false;
        }

        //Below part for increasing the UAC previleges to the token.
        var tp = new TokPriv1Luid
        {
            Count = 1,
            Luid = 0
        };
        if (!LookupPrivilegeValue(null, SE_INCREASE_QUOTA_NAME, ref tp.Luid))
        {
            _logger.LogDebug("LookupPrivilegeValue failed: " + Marshal.GetLastWin32Error());
            return false;
        }

        tp.Attr = SE_PRIVILEGE_ENABLED;
        if (!AdjustTokenPrivileges(loggedInUserToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
        {
            _logger.LogDebug("OpenProcessToken failed: " + Marshal.GetLastWin32Error());
            return false;
        }

        CloseHandle(loggedInUserToken);

        var explorerProcessList = new List<Process>();
        var trayProcessName =
            appName.Substring(appName.LastIndexOf(@"\") + 1, appName.Length - appName.LastIndexOf(@"\") - 5);
        foreach (var explorerProcess in Process.GetProcessesByName("explorer"))
        {
            bool isProcessRunningForUser = false;
            foreach (var phTrayProcess in Process.GetProcessesByName(trayProcessName))
            {
                if (explorerProcess.SessionId == phTrayProcess.SessionId)
                {
                    _logger.LogDebug(trayProcessName + " is already running for user SessionId " +
                                     explorerProcess.SessionId);
                    isProcessRunningForUser = true;
                    break;
                }
            }

            if (((Environment.OSVersion.Version.Major > 5 && explorerProcess.SessionId > 0)
                 || Environment.OSVersion.Version.Major == 5)
                && !isProcessRunningForUser)
            {
                _logger.LogDebug(trayProcessName + " is not running for user SessionId " + explorerProcess.SessionId);
                explorerProcessList.Add(explorerProcess);
            }
        }

        if (explorerProcessList.Count > 0)
        {
            foreach (Process explorerProcess in explorerProcessList)
            {
                Process shellProcess = explorerProcess;

                try
                {
                    var tokenRights = MAXIMUM_ALLOWED;
                    if (!OpenProcessToken(shellProcess.Handle, tokenRights, ref shellProcessToken))
                    {
                        _logger.LogDebug("Unable to OpenProcessToken " + Marshal.GetLastWin32Error());
                        return false;
                    }

                    SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
                    sa.nLength = Marshal.SizeOf(sa);

                    if (!DuplicateTokenEx(shellProcessToken, tokenRights, ref sa, 2, 1, ref duplicateToken))
                    {
                        _logger.LogDebug("Unable to duplicate token " + Marshal.GetLastWin32Error());
                        return false;
                    }

                    _logger.LogDebug("Duplicated the token " + WindowsIdentity.GetCurrent().Name);

                    var processAttributes = new SECURITY_ATTRIBUTES();
                    var threadAttributes = new SECURITY_ATTRIBUTES();
#if false
                    var si = new STARTUPINFO();
#else
                    var si = new STARTUPINFO
                    {
                        wShowWindow = 0
                    };
#endif
                    si.cb = (uint)Marshal.SizeOf(si);

                    // var userEnvironment = IntPtr.Zero;
                    uint dwCreationFlags = NORMAL_PRIORITY_CLASS;
                    if (!CreateEnvironmentBlock(out var userEnvironment, shellProcessToken, true))
                    {
                        _logger.LogDebug("Unable to create user's enviroment block " + Marshal.GetLastWin32Error());
                    }
                    else
                    {
#if false
                        dwCreationFlags |= CREATE_UNICODE_ENVIRONMENT;
#else
                        dwCreationFlags |= CREATE_UNICODE_ENVIRONMENT | CREATE_NO_WINDOW;
#endif
                    }

                    if (!CreateProcessAsUser(duplicateToken, appName,
                            cmdLineArgs ?? string.Empty, ref processAttributes,
                            ref threadAttributes, true, dwCreationFlags, userEnvironment,
                            appName.Substring(0, appName.LastIndexOf('\\')), ref si, out var pi))
                    {
                        _logger.LogDebug("Unable to create process " + Marshal.GetLastWin32Error());
                        if (Marshal.GetLastWin32Error() == 740)
                        {
                            _logger.LogDebug(
                                "Please check the installation as some elevated permissions is required to execute the binaries");
                        }

                        return false;
                    }
                }
                finally
                {
                    CloseHandle(shellProcessToken);
                    CloseHandle(duplicateToken);
                }
            }
        }
        else
        {
            _logger.LogDebug("No user has been identified to have logged into the system.");
            return false;
        }

        _logger.LogDebug("Finished ExecuteAppAsLoggedOnUser for all users.");
        return true;
    }
}