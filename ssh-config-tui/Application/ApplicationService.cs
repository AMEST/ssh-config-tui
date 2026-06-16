using SshConfigTui.Domain;
using SshConfigTui.Infrastructure;

namespace SshConfigTui.Application;

public class ApplicationService
{
    private readonly SshConfigRepository _repository;
    private readonly ConfigService _configService;
    private readonly GroupService _groupService;

    public ApplicationService(
        SshConfigRepository repository,
        ConfigService configService,
        GroupService groupService)
    {
        _repository = repository;
        _configService = configService;
        _groupService = groupService;
    }

    public SshConfigRepository Repository => _repository;
    public ConfigService ConfigService => _configService;
    public GroupService GroupService => _groupService;

    public async Task InitializeAsync()
    {
        await _configService.LoadAsync();
    }

    public bool HasUnsavedChanges() => _repository.HasUnsavedChanges();

    public async Task SaveAllAsync()
    {
        await _configService.SaveAsync();
    }

    public string GetConfigPath() => _repository.ConfigPath;

    public string ExportHost(string hostName)
    {
        var host = _configService.CurrentConfig?.GetHost(hostName);
        if (host == null) return string.Empty;

        var parser = new SshConfigParser();
        var tempConfig = new SshConfig();
        tempConfig.Nodes.Add(host);
        return parser.Serialize(tempConfig);
    }

    public string ExportGroup(string group)
    {
        var hosts = _groupService.GetHostsByGroup(group);
        if (hosts.Count == 0) return string.Empty;

        var parser = new SshConfigParser();
        var tempConfig = new SshConfig();
        foreach (var hostEntry in hosts)
        {
            var section = _configService.CurrentConfig?.GetHost(hostEntry.Name);
            if (section != null)
                tempConfig.Nodes.Add(section);
        }
        return parser.Serialize(tempConfig);
    }
}
