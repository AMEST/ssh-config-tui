using System.Diagnostics;
using SshConfigTui.Domain;

namespace SshConfigTui.Infrastructure;

public class SshConfigRepository
{
    private readonly SshConfigParser _parser;
    private readonly string _configPath;
    private readonly string _backupPath;
    private SshConfig? _cachedConfig;
    private DateTime? _lastWriteTime;

    public SshConfigRepository(SshConfigParser parser)
    {
        _parser = parser;
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ssh", "config");
        _backupPath = _configPath + ".bak";
    }

    public string ConfigPath => _configPath;

    public async Task<SshConfig> LoadAsync()
    {
        if (!File.Exists(_configPath))
        {
            var dir = Path.GetDirectoryName(_configPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(_configPath, string.Empty);
            SetFilePermissions(_configPath);
        }

        var text = await File.ReadAllTextAsync(_configPath);
        _lastWriteTime = File.GetLastWriteTimeUtc(_configPath);
        _cachedConfig = _parser.Parse(text);
        return _cachedConfig;
    }

    public async Task SaveAsync(SshConfig config)
    {
        var serialized = _parser.Serialize(config);

        var dir = Path.GetDirectoryName(_configPath)!;
        var tempPath = Path.Combine(dir, ".ssh-config-tui.tmp");

        if (File.Exists(_configPath))
            File.Copy(_configPath, _backupPath, overwrite: true);

        await File.WriteAllTextAsync(tempPath, serialized);
        SetFilePermissions(tempPath);

        File.Move(tempPath, _configPath, overwrite: true);
        _lastWriteTime = File.GetLastWriteTimeUtc(_configPath);
        _cachedConfig = config;
    }

    public bool HasUnsavedChanges()
    {
        if (_lastWriteTime == null) return false;
        var currentWriteTime = File.GetLastWriteTimeUtc(_configPath);
        return currentWriteTime != _lastWriteTime.Value;
    }

    public (bool IsValid, string? Warning) ValidatePermissions()
    {
        if (!File.Exists(_configPath))
            return (true, null);

        try
        {
            var fileInfo = new FileInfo(_configPath);
            var currentUser = Environment.UserName;

            if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
                return (false, "SSH config file is read-only. Changes cannot be saved.");

            var unixPermissions = GetUnixPermissions(_configPath);
            if (unixPermissions != null && unixPermissions != "600" && unixPermissions != "400")
            {
                return (true, $"SSH config file has permissions {unixPermissions}. Recommended: 600.");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Cannot validate permissions: {ex.Message}");
        }
    }

    private void SetFilePermissions(string path)
    {
        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                var psi = new ProcessStartInfo("chmod", $"600 \"{path}\"")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                Process.Start(psi)?.WaitForExit(5000);
            }
        }
        catch
        {
        }
    }

    private static string? GetUnixPermissions(string path)
    {
        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                var psi = new ProcessStartInfo("stat", $"-c \"%a\" \"{path}\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                var process = Process.Start(psi);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit(2000);
                    return output;
                }
            }
        }
        catch
        {
        }
        return null;
    }
}
