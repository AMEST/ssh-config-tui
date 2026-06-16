using System.Diagnostics;
using SshConfigTui.Domain;

namespace SshConfigTui.Infrastructure;

public class SshConfigRepository
{
    private readonly SshConfigParser _parser;
    private readonly DebugLogger _log;
    private readonly string _configPath;
    private readonly string _backupPath;
    private SshConfig? _cachedConfig;
    private DateTime? _lastWriteTime;

    public SshConfigRepository(SshConfigParser parser, DebugLogger log)
    {
        _parser = parser;
        _log = log;
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ssh", "config");
        _backupPath = _configPath + ".backup";
        _log.Write($"Config path: {_configPath}");
    }

    public string ConfigPath => _configPath;

    public SshConfig Load()
    {
        _log.Write("Load: start");

        if (!File.Exists(_configPath))
        {
            _log.Write($"  Config file does not exist, creating empty: {_configPath}");
            var dir = Path.GetDirectoryName(_configPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(_configPath, string.Empty);
            SetFilePermissions(_configPath);
        }

        _log.Write($"  Reading config file: {_configPath}");
        var text = File.ReadAllText(_configPath);
        _lastWriteTime = File.GetLastWriteTimeUtc(_configPath);
        _log.Write($"  File read: {text.Length} chars, last write: {_lastWriteTime}");

        _cachedConfig = _parser.Parse(text);
        _log.Write($"Load: done, {_cachedConfig.GetHosts().Count} hosts");
        return _cachedConfig;
    }

    public void Save(SshConfig config)
    {
        _log.Write("Save: start");
        var serialized = _parser.Serialize(config);

        var dir = Path.GetDirectoryName(_configPath)!;
        var tempPath = Path.Combine(dir, ".ssh-config-tui.tmp");

        if (File.Exists(_configPath))
        {
            _log.Write($"  Creating backup: {_backupPath}");
            File.Copy(_configPath, _backupPath, overwrite: true);
        }

        _log.Write($"  Writing temp file: {tempPath}");
        File.WriteAllText(tempPath, serialized);
        SetFilePermissions(tempPath);

        _log.Write("  Atomic rename");
        File.Move(tempPath, _configPath, overwrite: true);
        _lastWriteTime = File.GetLastWriteTimeUtc(_configPath);
        _cachedConfig = config;
        _log.Write("Save: done");
    }

    public void CreateBackup()
    {
        _log.Write("CreateBackup: start");
        if (!File.Exists(_configPath))
        {
            _log.Write("  No config file to back up");
            return;
        }

        _log.Write($"  Copying to {_backupPath}");
        File.Copy(_configPath, _backupPath, overwrite: true);
        SetFilePermissions(_backupPath);
        _log.Write("CreateBackup: done");
    }

    public bool HasUnsavedChanges()
    {
        if (_lastWriteTime == null) return false;
        var currentWriteTime = File.GetLastWriteTimeUtc(_configPath);
        var changed = currentWriteTime != _lastWriteTime.Value;
        _log.Write($"HasUnsavedChanges: {changed}");
        return changed;
    }

    public (bool IsValid, string? Warning) ValidatePermissions()
    {
        _log.Write("ValidatePermissions: start");

        if (!File.Exists(_configPath))
        {
            _log.Write("  No file, skipping");
            return (true, null);
        }

        try
        {
            var fileInfo = new FileInfo(_configPath);
            _log.Write($"  Attributes: {fileInfo.Attributes}");

            if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                _log.Write("  WARNING: File is read-only");
                return (false, "SSH config file is read-only. Changes cannot be saved.");
            }

            var unixPermissions = GetUnixPermissions(_configPath);
            _log.Write($"  Unix permissions: {unixPermissions ?? "N/A"}");

            if (unixPermissions != null && unixPermissions != "600" && unixPermissions != "400")
            {
                _log.Write($"  WARNING: permissions {unixPermissions}, expected 600");
                return (true, $"SSH config file has permissions {unixPermissions}. Recommended: 600.");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _log.WriteError("ValidatePermissions error", ex);
            return (false, $"Cannot validate permissions: {ex.Message}");
        }
    }

    private void SetFilePermissions(string path)
    {
        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                _log.Write($"  chmod 600 {path}");
                var psi = new ProcessStartInfo("chmod", $"600 \"{path}\"")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                Process.Start(psi)?.WaitForExit(5000);
            }
        }
        catch (Exception ex)
        {
            _log.WriteError("chmod failed", ex);
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
