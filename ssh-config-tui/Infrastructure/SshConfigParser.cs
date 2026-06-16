using System.Text.RegularExpressions;
using SshConfigTui.Domain;

namespace SshConfigTui.Infrastructure;

public class SshConfigParser
{
    private const string GroupPrefix = "# tui-group:";

    public SshConfig Parse(string text)
    {
        var config = new SshConfig();
        var lines = text.Split('\n');

        ConfigNode? currentSection = null;
        var pendingComments = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var lineNum = i + 1;

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushPendingComments(config, currentSection, pendingComments);
                config.Nodes.Add(new EmptyLine { LineNumber = lineNum });
                continue;
            }

            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            if (trimmed.StartsWith('#'))
            {
                if (currentSection is HostSection host && indent == 0 && trimmed.StartsWith(GroupPrefix))
                {
                    var groupsStr = trimmed[GroupPrefix.Length..].Trim();
                    var groups = groupsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(g => g.Trim())
                        .Where(g => g.Length > 0)
                        .ToList();
                    host.Groups.AddRange(groups);
                }
                else if (currentSection is HostSection hostSection && indent > 0)
                {
                    hostSection.TrailingComments.Add(line);
                }
                else if (currentSection is MatchSection matchSection)
                {
                    matchSection.LeadingComments.Add(line);
                }
                else if (indent == 0)
                {
                    pendingComments.Add(line);
                }
                else
                {
                    pendingComments.Add(line);
                }
                continue;
            }

            if (indent == 0)
            {
                FlushPendingComments(config, currentSection, pendingComments);

                if (trimmed.StartsWith("Host ", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals("Host", StringComparison.OrdinalIgnoreCase))
                {
                    var pattern = trimmed.Length > 5 ? trimmed[5..].Trim() : string.Empty;
                    currentSection = new HostSection
                    {
                        Pattern = pattern,
                        StartLine = lineNum,
                        LeadingComments = new List<string>(pendingComments)
                    };
                    pendingComments.Clear();
                    config.Nodes.Add(currentSection);
                }
                else if (trimmed.StartsWith("Match ", StringComparison.OrdinalIgnoreCase) ||
                         trimmed.Equals("Match", StringComparison.OrdinalIgnoreCase))
                {
                    var criteria = trimmed.Length > 6 ? trimmed[6..].Trim() : string.Empty;
                    currentSection = new MatchSection
                    {
                        Criteria = criteria,
                        StartLine = lineNum,
                        LeadingComments = new List<string>(pendingComments)
                    };
                    pendingComments.Clear();
                    config.Nodes.Add(currentSection);
                }
                else
                {
                    pendingComments.Add(line);
                }
            }
            else
            {
                var parts = ParseDirective(trimmed);
                if (parts != null)
                {
                    var directive = new SshDirective
                    {
                        Key = parts.Value.Key,
                        Value = parts.Value.Value,
                        LineNumber = lineNum
                    };

                    if (currentSection is HostSection host)
                        host.Directives.Add(directive);
                    else if (currentSection is MatchSection match)
                        match.Directives.Add(directive);
                }
                else
                {
                    pendingComments.Add(line);
                }
            }
        }

        FlushPendingComments(config, currentSection, pendingComments);
        return config;
    }

    public string Serialize(SshConfig config)
    {
        var lines = new List<string>();

        foreach (var node in config.Nodes)
        {
            switch (node)
            {
                case EmptyLine:
                    lines.Add(string.Empty);
                    break;

                case CommentLine comment:
                    lines.Add(comment.Text);
                    break;

                case HostSection host:
                    SerializeHostSection(lines, host);
                    break;

                case MatchSection match:
                    lines.AddRange(match.LeadingComments);
                    lines.Add($"Match {match.Criteria}".TrimEnd());
                    foreach (var dir in match.Directives)
                        lines.Add($"    {dir.Key} {dir.Value}");
                    break;
            }
        }

        return string.Join("\n", lines);
    }

    private void SerializeHostSection(List<string> lines, HostSection host)
    {
        lines.AddRange(host.LeadingComments);

        if (host.Groups.Count > 0 && !host.LeadingComments.Any(c => c.TrimStart().StartsWith(GroupPrefix)))
        {
            lines.Add($"{GroupPrefix} {string.Join(", ", host.Groups)}");
        }

        lines.Add($"Host {host.Pattern}".TrimEnd());

        foreach (var dir in host.Directives)
            lines.Add($"    {dir.Key} {dir.Value}");

        lines.AddRange(host.TrailingComments);
    }

    private (string Key, string Value)? ParseDirective(string line)
    {
        var match = Regex.Match(line, @"^(\S+)\s+(.*)$");
        if (match.Success)
            return (match.Groups[1].Value, match.Groups[2].Value.Trim());
        return null;
    }

    private void FlushPendingComments(SshConfig config, ConfigNode? currentSection, List<string> pendingComments)
    {
        if (pendingComments.Count == 0) return;

        if (currentSection is HostSection host)
        {
            host.TrailingComments.AddRange(pendingComments);
        }
        else
        {
            foreach (var c in pendingComments)
                config.Nodes.Add(new CommentLine { Text = c });
        }
        pendingComments.Clear();
    }
}
