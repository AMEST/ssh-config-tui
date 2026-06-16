using System.Text.Json;
using SshConfigTui.Domain;
using SshConfigTui.Infrastructure;

namespace SshConfigTui.Application;

public class TemplateService
{
    private readonly string _templatesPath;
    private readonly DebugLogger _log;
    private List<HostTemplate> _templates;

    public TemplateService(DebugLogger log)
    {
        _log = log;
        _templatesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ssh", "config-tui-templates.json");
        _templates = new();
        Load();
    }

    public List<HostTemplate> GetTemplates() => _templates;

    public void Load()
    {
        try
        {
            if (!File.Exists(_templatesPath))
            {
                _templates = GetDefaults();
                Save();
                return;
            }
            var json = File.ReadAllText(_templatesPath);
            _templates = JsonSerializer.Deserialize<List<HostTemplate>>(json) ?? GetDefaults();
            _log.Write($"Templates loaded: {_templates.Count}");
        }
        catch
        {
            _templates = GetDefaults();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_templates, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_templatesPath, json);
        }
        catch (Exception ex)
        {
            _log.WriteError("Failed to save templates", ex);
        }
    }

    public HostEntry ApplyTemplate(string templateName, string hostPattern)
    {
        var tmpl = _templates.FirstOrDefault(t =>
            t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));
        if (tmpl == null)
            throw new ArgumentException($"Template '{templateName}' not found.");

        return new HostEntry
        {
            Name = hostPattern,
            HostName = hostPattern,
            User = tmpl.User,
            Port = tmpl.Port,
            IdentityFile = tmpl.IdentityFile,
            ProxyJump = tmpl.ProxyJump,
            ForwardAgent = tmpl.ForwardAgent,
        };
    }

    private static List<HostTemplate> GetDefaults()
    {
        return new()
        {
            new HostTemplate
            {
                Name = "Default",
                User = "root",
                Port = 22,
                IdentityFile = "~/.ssh/id_rsa",
            },
            new HostTemplate
            {
                Name = "Ubuntu Server",
                User = "ubuntu",
                Port = 22,
                IdentityFile = "~/.ssh/id_rsa",
            },
            new HostTemplate
            {
                Name = "Jump Host",
                User = "admin",
                Port = 22,
                ForwardAgent = true,
            },
        };
    }
}

public class HostTemplate
{
    public string Name { get; set; } = "";
    public string User { get; set; } = "";
    public int Port { get; set; } = 22;
    public string IdentityFile { get; set; } = "";
    public string ProxyJump { get; set; } = "";
    public bool ForwardAgent { get; set; }
}
