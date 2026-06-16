using System.Diagnostics;

namespace SshConfigTui.Application;

public class ClipboardService
{
    public void Copy(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        try
        {
            if (OperatingSystem.IsMacOS())
            {
                var psi = new ProcessStartInfo("pbcopy")
                {
                    RedirectStandardInput = true,
                    UseShellExecute = false
                };
                var process = Process.Start(psi);
                if (process != null)
                {
                    process.StandardInput.Write(text);
                    process.StandardInput.Close();
                    process.WaitForExit(2000);
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                var psi = new ProcessStartInfo("xclip", "-selection clipboard")
                {
                    RedirectStandardInput = true,
                    UseShellExecute = false
                };
                var process = Process.Start(psi);
                if (process != null)
                {
                    process.StandardInput.Write(text);
                    process.StandardInput.Close();
                    process.WaitForExit(2000);
                }
            }
        }
        catch
        {
        }
    }

    public string GetConnectionString(string hostName) => $"ssh {hostName}";
}
