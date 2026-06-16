namespace SshConfigTui.Domain;

public class Group
{
    public string Name { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; }

    public static Group All => new() { Name = "All", IsBuiltIn = true };
    public static Group Ungrouped => new() { Name = "Ungrouped", IsBuiltIn = true };

    public static bool IsBuiltInGroup(string name) =>
        string.Equals(name, "All", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "Ungrouped", StringComparison.OrdinalIgnoreCase);
}
