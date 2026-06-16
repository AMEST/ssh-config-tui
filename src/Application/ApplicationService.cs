using SshConfigTui.Domain;
using SshConfigTui.Infrastructure;

namespace SshConfigTui.Application;

public class ApplicationService
{
    private readonly SshConfigRepository _repository;
    private readonly ConfigService _configService;
    private readonly GroupService _groupService;
    private readonly SshConfigParser _parser;
    private readonly DebugLogger _log;

    public ApplicationService(
        SshConfigRepository repository,
        ConfigService configService,
        GroupService groupService,
        SshConfigParser parser,
        DebugLogger log)
    {
        _repository = repository;
        _configService = configService;
        _groupService = groupService;
        _parser = parser;
        _log = log;
    }

    public SshConfigRepository Repository => _repository;
    public ConfigService ConfigService => _configService;
    public GroupService GroupService => _groupService;

    public void Initialize()
    {
        _log.Write("Initialize: start");
        _configService.Load();
        _log.Write("Initialize: done");
    }

    public bool HasUnsavedChanges() => _repository.HasUnsavedChanges();

    public void SaveAll()
    {
        _log.Write("SaveAll: start");
        _configService.Save();
        _log.Write("SaveAll: done");
    }

    public string GetConfigPath() => _repository.ConfigPath;

    public string ExportHost(string hostName)
    {
        _log.Write($"ExportHost: '{hostName}'");
        var host = _configService.CurrentConfig?.GetHost(hostName);
        if (host == null)
        {
            _log.Write("  Host not found");
            return string.Empty;
        }

        var tempConfig = new SshConfig();
        tempConfig.Nodes.Add(host);
        return _parser.Serialize(tempConfig);
    }

    public string ExportGroup(string group)
    {
        _log.Write($"ExportGroup: '{group}'");
        var hosts = _groupService.GetHostsByGroup(group);
        if (hosts.Count == 0)
        {
            _log.Write("  No hosts in group");
            return string.Empty;
        }

        var tempConfig = new SshConfig();
        foreach (var hostEntry in hosts)
        {
            var section = _configService.CurrentConfig?.GetHost(hostEntry.Name);
            if (section != null)
                tempConfig.Nodes.Add(section);
        }
        return _parser.Serialize(tempConfig);
    }
}
