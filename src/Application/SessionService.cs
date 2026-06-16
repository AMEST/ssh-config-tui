using System.Text.Json;
using SshConfigTui.Infrastructure;

namespace SshConfigTui.Application;

public class SessionService
{
    private readonly string _sessionPath;
    private readonly DebugLogger _log;

    public SessionService(DebugLogger log)
    {
        _log = log;
        _sessionPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ssh", "config-tui-session.json");
    }

    public void SaveLastGroup(string group)
    {
        try
        {
            var data = new { LastGroup = group };
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(_sessionPath, json);
            _log.Write($"Session saved: last_group={group}");
        }
        catch (Exception ex)
        {
            _log.WriteError("Failed to save session", ex);
        }
    }

    public string? LoadLastGroup()
    {
        try
        {
            if (!File.Exists(_sessionPath))
                return null;

            var json = File.ReadAllText(_sessionPath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (data != null && data.TryGetValue("LastGroup", out var group))
            {
                _log.Write($"Session loaded: last_group={group}");
                return group;
            }
        }
        catch (Exception ex)
        {
            _log.WriteError("Failed to load session", ex);
        }
        return null;
    }
}
