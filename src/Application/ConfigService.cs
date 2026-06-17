using SshConfigTui.Domain;
using SshConfigTui.Infrastructure;

namespace SshConfigTui.Application;

public class ConfigService
{
    private readonly SshConfigRepository _repository;
    private readonly SshConfigParser _parser;
    private readonly DebugLogger _log;

    public ConfigService(SshConfigRepository repository, SshConfigParser parser, DebugLogger log)
    {
        _repository = repository;
        _parser = parser;
        _log = log;
    }

    public SshConfig? CurrentConfig { get; private set; }

    public void Load()
    {
        _log.Write("Load: start");
        CurrentConfig = _repository.Load();
        _log.Write($"Load: done, config={(CurrentConfig != null ? "OK" : "null")}");
    }

    public void Save()
    {
        _log.Write("Save: start");
        if (CurrentConfig != null)
        {
            _repository.Save(CurrentConfig);
            _log.Write("Save: done");
        }
        else
        {
            _log.Write("Save: skipped, no config loaded");
        }
    }

    public List<HostEntry> GetAllHosts()
    {
        if (CurrentConfig == null)
        {
            _log.Write("GetAllHosts: config is null, returning empty");
            return new();
        }

        var hosts = CurrentConfig.GetHosts()
            .Where(h => h.Pattern != "*")
            .Select(HostEntry.FromHostSection)
            .ToList();

        _log.Write($"GetAllHosts: returning {hosts.Count} hosts");
        return hosts;
    }

    public HostEntry? GetHost(string name)
    {
        var section = CurrentConfig?.GetHost(name);
        var result = section != null ? HostEntry.FromHostSection(section) : null;
        _log.Write($"GetHost('{name}'): {(result != null ? "found" : "not found")}");
        return result;
    }

    public HostEntry? GetEffectiveConfig(string hostName)
    {
        _log.Write($"GetEffectiveConfig('{hostName}'): start");

        var host = CurrentConfig?.GetHost(hostName);
        if (host == null)
        {
            _log.Write($"  Host '{hostName}' not found");
            return null;
        }

        var entry = HostEntry.FromHostSection(host);
        var global = CurrentConfig?.GetGlobalConfig();

        if (global != null)
        {
            _log.Write("  Merging with global config (Host *)");
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

        _log.Write($"GetEffectiveConfig: done -> {entry.Name} @ {entry.HostName}");
        return entry;
    }

    public void AddHost(HostEntry entry)
    {
        _log.Write($"AddHost: '{entry.Name}'");
        if (CurrentConfig == null) return;

        var section = new HostSection
        {
            Pattern = entry.Name,
            Groups = new List<string>(entry.Groups),
            Directives = ToDirectives(entry)
        };

        if (CurrentConfig.Nodes.Count > 0 && CurrentConfig.Nodes[^1] is not EmptyLine)
        {
            CurrentConfig.Nodes.Add(new EmptyLine());
        }

        CurrentConfig.Nodes.Add(section);
    }

    public void UpdateHost(HostEntry entry)
    {
        _log.Write($"UpdateHost: '{entry.Name}'");
        if (CurrentConfig == null) return;

        var existing = CurrentConfig.GetHost(entry.Name);
        if (existing != null)
        {
            existing.Directives = ToDirectives(entry);
            existing.Groups = new List<string>(entry.Groups);
            _log.Write($"  Updated {existing.Directives.Count} directives, {existing.Groups.Count} groups");
        }
        else
        {
            _log.Write("  Host not found for update");
        }
    }

    public void DeleteHost(string name)
    {
        _log.Write($"DeleteHost: '{name}'");
        if (CurrentConfig == null) return;
        var node = CurrentConfig.Nodes.OfType<HostSection>()
            .FirstOrDefault(h => h.Pattern == name);
        if (node != null)
        {
            CurrentConfig.Nodes.Remove(node);
            _log.Write("  Removed");
        }
        else
        {
            _log.Write("  Not found");
        }
    }

    public HostEntry? GetGlobalConfig()
    {
        var section = CurrentConfig?.GetGlobalConfig();
        var result = section != null ? HostEntry.FromHostSection(section) : null;
        _log.Write($"GetGlobalConfig: {(result != null ? "found" : "not found")}");
        return result;
    }

    public int ImportFragment(string fragment)
    {
        _log.Write("ImportFragment: start");
        if (CurrentConfig == null) return 0;

        var tempConfig = _parser.Parse(fragment);
        var imported = 0;
        foreach (var host in tempConfig.GetHosts())
        {
            if (CurrentConfig.GetHost(host.Pattern) != null)
            {
                _log.Write($"  Host '{host.Pattern}' already exists, skipping");
                continue;
            }
            CurrentConfig.Nodes.Add(host);
            imported++;
            _log.Write($"  Imported host '{host.Pattern}'");
        }
        _log.Write($"ImportFragment: imported {imported} hosts");
        return imported;
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
