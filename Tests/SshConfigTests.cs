using SshConfigTui.Domain;

namespace SshConfigTui.Tests;

public class SshConfigTests
{
    private static SshConfig MakeConfig()
    {
        var config = new SshConfig();
        config.Nodes.Add(new HostSection { Pattern = "*", Groups = new() });
        config.Nodes.Add(new HostSection { Pattern = "server1", Groups = new() { "work" }, Directives = { new SshDirective { Key = "User", Value = "admin" } } });
        config.Nodes.Add(new HostSection { Pattern = "server2", Groups = new() { "work" } });
        config.Nodes.Add(new HostSection { Pattern = "server3", Groups = new() { "home" } });
        config.Nodes.Add(new HostSection { Pattern = "server4", Groups = new() });
        config.Nodes.Add(new EmptyLine());
        config.Nodes.Add(new MatchSection { Criteria = "host foo" });
        return config;
    }

    [Fact]
    public void GetHosts_ExcludesNonHostNodes()
    {
        var config = MakeConfig();
        var hosts = config.GetHosts();

        Assert.All(hosts, h => Assert.IsType<HostSection>(h));
        Assert.DoesNotContain(hosts, h => h is MatchSection);
    }

    [Fact]
    public void GetHost_ByName_ReturnsCorrectHost()
    {
        var config = MakeConfig();
        var host = config.GetHost("server1");

        Assert.NotNull(host);
        Assert.Equal("server1", host!.Pattern);
    }

    [Fact]
    public void GetHost_NotFound_ReturnsNull()
    {
        var config = MakeConfig();
        Assert.Null(config.GetHost("nonexistent"));
    }

    [Fact]
    public void GetGlobalConfig_ReturnsStarHost()
    {
        var config = MakeConfig();
        var global = config.GetGlobalConfig();

        Assert.NotNull(global);
        Assert.Equal("*", global!.Pattern);
    }

    [Fact]
    public void GetGlobalConfig_NoStar_ReturnsNull()
    {
        var config = new SshConfig();
        config.Nodes.Add(new HostSection { Pattern = "server1" });
        Assert.Null(config.GetGlobalConfig());
    }

    [Fact]
    public void GetAllGroups_ReturnsDistinctSorted()
    {
        var config = MakeConfig();
        var groups = config.GetAllGroups();

        Assert.Equal(2, groups.Count);
        Assert.Equal("home", groups[0]);
        Assert.Equal("work", groups[1]);
    }

    [Fact]
    public void GetHostsByGroup_All_ExcludesStar()
    {
        var config = MakeConfig();
        var hosts = config.GetHostsByGroup("All");

        Assert.DoesNotContain(hosts, h => h.Pattern == "*");
    }

    [Fact]
    public void GetHostsByGroup_Ungrouped_ReturnsHostsWithoutGroups()
    {
        var config = MakeConfig();
        var hosts = config.GetHostsByGroup("Ungrouped");

        Assert.Single(hosts);
        Assert.Equal("server4", hosts[0].Pattern);
    }

    [Fact]
    public void GetHostsByGroup_CustomGroup_ReturnsMatching()
    {
        var config = MakeConfig();
        var hosts = config.GetHostsByGroup("work");

        Assert.Equal(2, hosts.Count);
        Assert.All(hosts, h => Assert.Contains("work", h.Groups));
    }

    [Fact]
    public void GetHostsByGroup_CustomGroupCaseInsensitive_Matching()
    {
        var config = MakeConfig();
        var hosts = config.GetHostsByGroup("WORK");

        Assert.Equal(2, hosts.Count);
    }

    [Fact]
    public void GetHostsByGroup_NonExistentGroup_ReturnsEmpty()
    {
        var config = MakeConfig();
        var hosts = config.GetHostsByGroup("fakegroup");

        Assert.Empty(hosts);
    }

    [Fact]
    public void GetHostsByGroup_UngroupedCaseInsensitive_Works()
    {
        var config = MakeConfig();
        var hosts = config.GetHostsByGroup("ungrouped");

        Assert.Single(hosts);
        Assert.Equal("server4", hosts[0].Pattern);
    }
}
