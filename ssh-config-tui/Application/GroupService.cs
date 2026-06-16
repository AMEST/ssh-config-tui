using SshConfigTui.Domain;

namespace SshConfigTui.Application;

public class GroupService
{
    private readonly ConfigService _configService;

    public GroupService(ConfigService configService)
    {
        _configService = configService;
    }

    public List<string> GetGroups()
    {
        var config = _configService.CurrentConfig;
        if (config == null) return new();

        var groups = new List<string> { "All", "Ungrouped" };
        groups.AddRange(config.GetAllGroups());
        return groups;
    }

    public List<HostEntry> GetHostsByGroup(string group)
    {
        var config = _configService.CurrentConfig;
        if (config == null) return new();

        return config.GetHostsByGroup(group)
            .Select(HostEntry.FromHostSection)
            .ToList();
    }

    public void AddHostToGroup(string hostName, string group)
    {
        if (Group.IsBuiltInGroup(group)) return;

        var config = _configService.CurrentConfig;
        var host = config?.GetHost(hostName);
        if (host != null && !host.Groups.Contains(group, StringComparer.OrdinalIgnoreCase))
        {
            host.Groups.Add(group);
        }
    }

    public void RemoveHostFromGroup(string hostName, string group)
    {
        if (Group.IsBuiltInGroup(group)) return;

        var config = _configService.CurrentConfig;
        var host = config?.GetHost(hostName);
        if (host != null)
        {
            host.Groups.RemoveAll(g => string.Equals(g, group, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void RenameGroup(string oldName, string newName)
    {
        if (Group.IsBuiltInGroup(oldName)) return;

        var config = _configService.CurrentConfig;
        if (config == null) return;

        foreach (var host in config.GetHosts())
        {
            var idx = host.Groups.FindIndex(g =>
                string.Equals(g, oldName, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                host.Groups[idx] = newName;
            }
        }
    }

    public void DeleteGroup(string group)
    {
        if (Group.IsBuiltInGroup(group)) return;

        var config = _configService.CurrentConfig;
        if (config == null) return;

        foreach (var host in config.GetHosts())
        {
            host.Groups.RemoveAll(g =>
                string.Equals(g, group, StringComparison.OrdinalIgnoreCase));
        }
    }
}
