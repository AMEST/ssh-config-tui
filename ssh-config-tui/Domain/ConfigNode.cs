namespace SshConfigTui.Domain;

public abstract class ConfigNode
{
}

public class SshDirective
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int LineNumber { get; set; }

    public override string ToString() => $"{Key} {Value}";
}

public class HostSection : ConfigNode
{
    public string Pattern { get; set; } = string.Empty;
    public List<SshDirective> Directives { get; set; } = new();
    public List<string> LeadingComments { get; set; } = new();
    public List<string> TrailingComments { get; set; } = new();
    public List<string> Groups { get; set; } = new();
    public int StartLine { get; set; }

    public override string ToString() => $"Host {Pattern} ({Groups.Count} groups, {Directives.Count} directives)";
}

public class MatchSection : ConfigNode
{
    public string Criteria { get; set; } = string.Empty;
    public List<SshDirective> Directives { get; set; } = new();
    public List<string> LeadingComments { get; set; } = new();
    public int StartLine { get; set; }
}

public class CommentLine : ConfigNode
{
    public string Text { get; set; } = string.Empty;
    public int LineNumber { get; set; }
}

public class EmptyLine : ConfigNode
{
    public int LineNumber { get; set; }
}
