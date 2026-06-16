using SshConfigTui.Domain;
using SshConfigTui.Infrastructure;

namespace SshConfigTui.Application;

public class GroupService
{
    private readonly ConfigService _configService;
    private readonly DebugLogger _log;

    public GroupService(ConfigService configService, DebugLogger log)
    {
        _configService = configService;
        _log = log;
    }

    public List<string> GetGroups()
    {
        var config = _configService.CurrentConfig;
        if (config == null)
        {
            _log.Write("GetGroups: config is null");
            return new();
        }

        var customGroups = config.GetAllGroups();
        var groups = new List<string> { "All", "Ungrouped" };
        groups.AddRange(customGroups);

        _log.Write($"GetGroups: returning {groups.Count} groups ({string.Join(", ", groups)})");
        return groups;
    }

    public List<HostEntry> GetHostsByGroup(string group)
    {
        _log.Write($"GetHostsByGroup('{group}'): start");

        var config = _configService.CurrentConfig;
        if (config == null)
        {
            _log.Write("  Config is null");
            return new();
        }

        var hosts = config.GetHostsByGroup(group)
            .Select(HostEntry.FromHostSection)
            .ToList();

        _log.Write($"GetHostsByGroup: returning {hosts.Count} hosts");
        return hosts;
    }

    public void AddHostToGroup(string hostName, string group)
    {
        _log.Write($"AddHostToGroup: '{hostName}' -> '{group}'");

        if (Group.IsBuiltInGroup(group))
        {
            _log.Write("  Cannot modify built-in group");
            return;
        }

        var config = _configService.CurrentConfig;
        var host = config?.GetHost(hostName);
        if (host != null && !host.Groups.Contains(group, StringComparer.OrdinalIgnoreCase))
        {
            host.Groups.Add(group);
            _log.Write("  Added");
        }
        else
        {
            _log.Write("  Host not found or already in group");
        }
    }

    public void RemoveHostFromGroup(string hostName, string group)
    {
        _log.Write($"RemoveHostFromGroup: '{hostName}' <- '{group}'");

        if (Group.IsBuiltInGroup(group)) return;

        var config = _configService.CurrentConfig;
        var host = config?.GetHost(hostName);
        if (host != null)
        {
            var count = host.Groups.RemoveAll(g => string.Equals(g, group, StringComparison.OrdinalIgnoreCase));
            _log.Write($"  Removed {count} occurrences");
        }
    }

    public void RenameGroup(string oldName, string newName)
    {
        _log.Write($"RenameGroup: '{oldName}' -> '{newName}'");

        if (Group.IsBuiltInGroup(oldName))
        {
            _log.Write("  Cannot rename built-in group");
            return;
        }

        var config = _configService.CurrentConfig;
        if (config == null) return;

        var renamed = 0;
        foreach (var host in config.GetHosts())
        {
            var idx = host.Groups.FindIndex(g =>
                string.Equals(g, oldName, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                host.Groups[idx] = newName;
                renamed++;
            }
        }
        _log.Write($"  Renamed in {renamed} hosts");
    }

    public void DeleteGroup(string group)
    {
        _log.Write($"DeleteGroup: '{group}'");

        if (Group.IsBuiltInGroup(group)) return;

        var config = _configService.CurrentConfig;
        if (config == null) return;

        var removed = 0;
        foreach (var host in config.GetHosts())
        {
            removed += host.Groups.RemoveAll(g =>
                string.Equals(g, group, StringComparison.OrdinalIgnoreCase));
        }
        _log.Write($"  Removed from {removed} entries");
    }
}
