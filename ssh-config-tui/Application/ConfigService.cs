using SshConfigTui.Domain;
using SshConfigTui.Infrastructure;

namespace SshConfigTui.Application;

public class ConfigService
{
    private readonly SshConfigRepository _repository;

    public ConfigService(SshConfigRepository repository)
    {
        _repository = repository;
    }

    public SshConfig? CurrentConfig { get; private set; }

    public async Task LoadAsync()
    {
        CurrentConfig = await _repository.LoadAsync();
    }

    public async Task SaveAsync()
    {
        if (CurrentConfig != null)
            await _repository.SaveAsync(CurrentConfig);
    }

    public List<HostEntry> GetAllHosts()
    {
        if (CurrentConfig == null) return new();
        return CurrentConfig.GetHosts()
            .Where(h => h.Pattern != "*")
            .Select(HostEntry.FromHostSection)
            .ToList();
    }

    public HostEntry? GetHost(string name)
    {
        var section = CurrentConfig?.GetHost(name);
        return section != null ? HostEntry.FromHostSection(section) : null;
    }

    public HostEntry? GetEffectiveConfig(string hostName)
    {
        var host = CurrentConfig?.GetHost(hostName);
        if (host == null) return null;

        var entry = HostEntry.FromHostSection(host);
        var global = CurrentConfig?.GetGlobalConfig();

        if (global != null)
        {
            foreach (var dir in global.Directives)
            {
                switch (dir.Key.ToLowerInvariant())
                {
                    case "hostname" when string.IsNullOrEmpty(entry.HostName):
                        entry.HostName = dir.Value;
                        break;
                    case "user" when string.IsNullOrEmpty(entry.User):
                        entry.User = dir.Value;
                        break;
                    case "port" when entry.Port == 22:
                        int.TryParse(dir.Value, out var port);
                        entry.Port = port;
                        break;
                    case "identityfile" when string.IsNullOrEmpty(entry.IdentityFile):
                        entry.IdentityFile = dir.Value;
                        break;
                    case "proxyjump" when string.IsNullOrEmpty(entry.ProxyJump):
                        entry.ProxyJump = dir.Value;
                        break;
                }
            }
        }

        return entry;
    }

    public void AddHost(HostEntry entry)
    {
        if (CurrentConfig == null) return;

        var section = new HostSection
        {
            Pattern = entry.Name,
            Groups = new List<string>(entry.Groups),
            Directives = ToDirectives(entry)
        };

        CurrentConfig.Nodes.Add(section);
    }

    public void UpdateHost(HostEntry entry)
    {
        if (CurrentConfig == null) return;

        var existing = CurrentConfig.GetHost(entry.Name);
        if (existing != null)
        {
            existing.Directives = ToDirectives(entry);
            existing.Groups = new List<string>(entry.Groups);
        }
    }

    public void DeleteHost(string name)
    {
        if (CurrentConfig == null) return;
        var node = CurrentConfig.Nodes.OfType<HostSection>()
            .FirstOrDefault(h => h.Pattern == name);
        if (node != null)
            CurrentConfig.Nodes.Remove(node);
    }

    public HostEntry? GetGlobalConfig()
    {
        var section = CurrentConfig?.GetGlobalConfig();
        return section != null ? HostEntry.FromHostSection(section) : null;
    }

    private static List<SshDirective> ToDirectives(HostEntry entry)
    {
        var directives = new List<SshDirective>();

        if (!string.IsNullOrEmpty(entry.HostName))
            directives.Add(new SshDirective { Key = "HostName", Value = entry.HostName });
        if (!string.IsNullOrEmpty(entry.User))
            directives.Add(new SshDirective { Key = "User", Value = entry.User });
        if (entry.Port != 22)
            directives.Add(new SshDirective { Key = "Port", Value = entry.Port.ToString() });
        if (!string.IsNullOrEmpty(entry.IdentityFile))
            directives.Add(new SshDirective { Key = "IdentityFile", Value = entry.IdentityFile });
        if (!string.IsNullOrEmpty(entry.ProxyJump))
            directives.Add(new SshDirective { Key = "ProxyJump", Value = entry.ProxyJump });
        if (entry.ForwardAgent)
            directives.Add(new SshDirective { Key = "ForwardAgent", Value = "yes" });

        foreach (var lf in entry.LocalForwards)
            directives.Add(new SshDirective { Key = "LocalForward", Value = lf });
        foreach (var rf in entry.RemoteForwards)
            directives.Add(new SshDirective { Key = "RemoteForward", Value = rf });

        foreach (var kv in entry.ExtraDirectives)
            directives.Add(new SshDirective { Key = kv.Key, Value = kv.Value });

        return directives;
    }
}
