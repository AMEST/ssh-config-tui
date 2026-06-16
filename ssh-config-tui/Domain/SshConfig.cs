namespace SshConfigTui.Domain;

public class SshConfig
{
    public List<ConfigNode> Nodes { get; set; } = new();

    public List<HostSection> GetHosts() =>
        Nodes.OfType<HostSection>().ToList();

    public HostSection? GetHost(string name) =>
        Nodes.OfType<HostSection>().FirstOrDefault(h => h.Pattern == name);

    public HostSection? GetGlobalConfig() =>
        Nodes.OfType<HostSection>().FirstOrDefault(h => h.Pattern == "*");

    public List<string> GetAllGroups()
    {
        var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var host in GetHosts())
        {
            foreach (var g in host.Groups)
                groups.Add(g);
        }
        return groups.OrderBy(g => g).ToList();
    }

    public List<HostSection> GetHostsByGroup(string group)
    {
        if (string.Equals(group, "All", StringComparison.OrdinalIgnoreCase))
            return GetHosts().Where(h => h.Pattern != "*").ToList();

        if (string.Equals(group, "Ungrouped", StringComparison.OrdinalIgnoreCase))
            return GetHosts().Where(h => h.Pattern != "*" && h.Groups.Count == 0).ToList();

        return GetHosts()
            .Where(h => h.Pattern != "*" && h.Groups.Contains(group, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }
}
