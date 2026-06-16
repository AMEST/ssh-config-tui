namespace SshConfigTui.Domain;

public class HostEntry
{
    public string Name { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string IdentityFile { get; set; } = string.Empty;
    public string ProxyJump { get; set; } = string.Empty;
    public bool ForwardAgent { get; set; }
    public List<string> LocalForwards { get; set; } = new();
    public List<string> RemoteForwards { get; set; } = new();
    public List<string> Groups { get; set; } = new();
    public Dictionary<string, string> ExtraDirectives { get; set; } = new();

    public static HostEntry FromHostSection(HostSection section)
    {
        var entry = new HostEntry
        {
            Name = section.Pattern,
            Groups = new List<string>(section.Groups)
        };

        foreach (var dir in section.Directives)
        {
            switch (dir.Key.ToLowerInvariant())
            {
                case "hostname":
                    entry.HostName = dir.Value;
                    break;
                case "user":
                    entry.User = dir.Value;
                    break;
                case "port":
                    entry.Port = int.TryParse(dir.Value, out var port) ? port : 22;
                    break;
                case "identityfile":
                    entry.IdentityFile = dir.Value;
                    break;
                case "proxyjump":
                    entry.ProxyJump = dir.Value;
                    break;
                case "forwardagent":
                    var fav = dir.Value.ToLowerInvariant();
                    entry.ForwardAgent = fav is "yes" or "true" or "1";
                    break;
                case "localforward":
                    entry.LocalForwards.Add(dir.Value);
                    break;
                case "remoteforward":
                    entry.RemoteForwards.Add(dir.Value);
                    break;
                default:
                    entry.ExtraDirectives[dir.Key] = dir.Value;
                    break;
            }
        }

        return entry;
    }
}
