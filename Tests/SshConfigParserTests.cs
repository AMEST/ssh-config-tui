using SshConfigTui.Domain;
using SshConfigTui.Infrastructure;

namespace SshConfigTui.Tests;

public class SshConfigParserTests
{
    private readonly SshConfigParser _parser;

    public SshConfigParserTests()
    {
        _parser = new SshConfigParser(TestLogger.Create());
    }

    [Fact]
    public void Should_ReturnEmpty_When_ParsingEmptyConfig()
    {
        var config = _parser.Parse("");
        Assert.Empty(config.GetHosts());
    }

    [Fact]
    public void Should_CreateHostSection_When_ParsingSimpleHost()
    {
        var text = "Host myserver\n    HostName 192.168.1.1\n    User admin";
        var config = _parser.Parse(text);

        var host = Assert.Single(config.GetHosts());
        Assert.Equal("myserver", host.Pattern);
        Assert.Equal(2, host.Directives.Count);
    }

    [Fact]
    public void Should_ParseDirectives_When_NotIndented()
    {
        var text = "Host myserver\nHostName 192.168.1.1\nUser admin";
        var config = _parser.Parse(text);

        var host = Assert.Single(config.GetHosts());
        Assert.Equal(2, host.Directives.Count);
        Assert.Contains(host.Directives, d => d.Key == "HostName" && d.Value == "192.168.1.1");
        Assert.Contains(host.Directives, d => d.Key == "User" && d.Value == "admin");
    }

    [Fact]
    public void Should_AssignGroup_When_GroupCommentBeforeHost()
    {
        var text = "# tui-group: work\nHost *.local\n    User custom-user";
        var config = _parser.Parse(text);

        var host = Assert.Single(config.GetHosts());
        Assert.Contains("work", host.Groups);
    }

    [Fact]
    public void Should_AssignGroup_When_GroupCommentAfterHost()
    {
        var text = "Host myserver\n    User admin\n# tui-group: work";
        var config = _parser.Parse(text);

        var host = Assert.Single(config.GetHosts());
        Assert.Contains("work", host.Groups);
    }

    [Fact]
    public void Should_AssignAllGroups_When_MultipleGroupsInOneComment()
    {
        var text = "# tui-group: work, dev\nHost myserver\n    User admin";
        var config = _parser.Parse(text);

        var host = Assert.Single(config.GetHosts());
        Assert.Contains("work", host.Groups);
        Assert.Contains("dev", host.Groups);
    }

    [Fact]
    public void Should_AssignCorrectGroups_When_MultipleHostsWithGroupComments()
    {
        var text = """
                    # tui-group: work
                    Host *.local
                        User custom-user

                    # tui-group: my-group
                    Host srv-4.my-group.ru
                        User amest
                        Port 49160

                    # tui-group: home
                    Host opi
                        HostName 192.168.0.101
                        User amest
                    """;

        var config = _parser.Parse(text);

        Assert.Equal(3, config.GetHosts().Count);

        Assert.Contains("work", config.GetHost("*.local")!.Groups);
        Assert.Contains("my-group", config.GetHost("srv-4.my-group.ru")!.Groups);
        Assert.Contains("home", config.GetHost("opi")!.Groups);
    }

    [Fact]
    public void Should_CreateGlobalSection_When_HostStar()
    {
        var text = "Host *\n    User root\n    Port 22";
        var config = _parser.Parse(text);

        var global = config.GetGlobalConfig();
        Assert.NotNull(global);
        Assert.Equal("*", global!.Pattern);
    }

    [Fact]
    public void Should_KeepLeadingComments_When_CommentsBeforeHost()
    {
        var text = "# This is my server\nHost myserver\n    HostName example.com";
        var config = _parser.Parse(text);

        var host = Assert.Single(config.GetHosts());
        Assert.Contains(host.LeadingComments, c => c.Contains("This is my server"));
    }

    [Fact]
    public void Should_CreateMatchSection_When_ParsingMatch()
    {
        var text = "Match host foo\n    User bar";
        var config = _parser.Parse(text);

        Assert.Single(config.Nodes.OfType<MatchSection>());
        var match = config.Nodes.OfType<MatchSection>().First();
        Assert.Equal("host foo", match.Criteria);
    }

    [Fact]
    public void Should_AddEmptyLineNodes_When_EmptyLinesBetweenHosts()
    {
        var text = "Host a\n    User x\n\nHost b\n    User y";
        var config = _parser.Parse(text);

        Assert.Single(config.Nodes.OfType<EmptyLine>());
    }

    [Fact]
    public void Should_CreateHostWithEmptyDirectives_When_HostHasNoDirectives()
    {
        var text = "Host lonely";
        var config = _parser.Parse(text);

        var host = Assert.Single(config.GetHosts());
        Assert.Equal("lonely", host.Pattern);
        Assert.Empty(host.Directives);
    }

    [Fact]
    public void Should_ParseAllDirectives_When_HostHasMultipleDirectives()
    {
        var text = "Host srv\n    HostName srv.local\n    User admin\n    Port 2222\n    IdentityFile ~/.ssh/id_rsa\n    ProxyJump jump.host\n    ForwardAgent yes";
        var config = _parser.Parse(text);

        var host = Assert.Single(config.GetHosts());
        Assert.Contains(host.Directives, d => d.Key == "HostName" && d.Value == "srv.local");
        Assert.Contains(host.Directives, d => d.Key == "User" && d.Value == "admin");
        Assert.Contains(host.Directives, d => d.Key == "Port" && d.Value == "2222");
        Assert.Contains(host.Directives, d => d.Key == "IdentityFile" && d.Value == "~/.ssh/id_rsa");
        Assert.Contains(host.Directives, d => d.Key == "ProxyJump" && d.Value == "jump.host");
        Assert.Contains(host.Directives, d => d.Key == "ForwardAgent" && d.Value == "yes");
    }

    [Fact]
    public void Should_ProduceSameConfig_When_RoundtripParseSerialize()
    {
        var original = """
                       # tui-group: work
                       Host *.local
                           User custom-user

                       # tui-group: home
                       Host opi
                           HostName 192.168.0.101
                           User amest
                       """;

        var config = _parser.Parse(original);
        var serialized = _parser.Serialize(config);
        var reparsed = _parser.Parse(serialized);

        Assert.Equal(config.GetHosts().Count, reparsed.GetHosts().Count);
        Assert.Equal(
            config.GetHost("*.local")!.Groups,
            reparsed.GetHost("*.local")!.Groups);
        Assert.Equal(
            config.GetHost("opi")!.Groups,
            reparsed.GetHost("opi")!.Groups);
    }

    [Fact]
    public void Should_ContainGroupComment_When_SerializingHostWithGroups()
    {
        var text = "# tui-group: work\nHost srv\n    User admin";
        var config = _parser.Parse(text);
        var serialized = _parser.Serialize(config);

        Assert.Contains("# tui-group: work", serialized);
    }

    [Fact]
    public void Should_TopLevelDirectiveBeLeadingComment_When_BeforeFirstHost()
    {
        var text = "ForwardAgent yes\nHost srv\n    User admin";
        var config = _parser.Parse(text);

        var host = Assert.Single(config.GetHosts());
        Assert.Single(host.Directives);
        Assert.Contains(host.LeadingComments, c => c.Contains("ForwardAgent yes"));
    }

    [Fact]
    public void Should_ParseAllDirectives_When_MixedIndentation()
    {
        var text = "Host srv\nHostName srv.local\n  User admin\n    Port 2222";
        var config = _parser.Parse(text);

        var host = Assert.Single(config.GetHosts());
        Assert.Equal(3, host.Directives.Count);
    }

    [Fact]
    public void Should_AssignThreeGroups_When_ThreeGroupsInOneComment()
    {
        var text = "# tui-group: group1, group2, group3\nHost multi\n    User test";
        var config = _parser.Parse(text);

        var host = Assert.Single(config.GetHosts());
        Assert.Equal(3, host.Groups.Count);
        Assert.Contains("group1", host.Groups);
        Assert.Contains("group2", host.Groups);
        Assert.Contains("group3", host.Groups);
    }

    [Fact]
    public void Should_TrimGroupName_When_ExtraSpacesInGroupComment()
    {
        var text = "# tui-group:   spaced-group   ";
        var config = _parser.Parse(text + "\nHost h\n    User u");

        var host = Assert.Single(config.GetHosts());
        Assert.Contains("spaced-group", host.Groups);
    }

    [Fact]
    public void Should_UseActualConfigFromUser_When_ConfigWithMultipleGroups()
    {
        var text = """
                   # tui-group: work
                   Host *.local
                       User custom-user

                   # tui-group: my-group
                   Host srv-4.my-group.ru
                       User amest
                       Port 49160

                   # tui-group: my-group
                   Host srv-6.my-group.ru
                       User amest
                       Port 49270

                   # tui-group: my-group
                   Host *.my-group.ru
                       User amest

                   # tui-group: home
                   Host opi
                       HostName 192.168.0.101
                       User amest

                   # tui-group: home
                   Host hal-0101
                       HostName 192.168.0.105
                       User amest
                   """;

        var config = _parser.Parse(text);

        Assert.Equal(6, config.GetHosts().Count);
        Assert.Contains("work", config.GetHost("*.local")!.Groups);
        Assert.Contains("my-group", config.GetHost("srv-4.my-group.ru")!.Groups);
        Assert.Contains("my-group", config.GetHost("srv-6.my-group.ru")!.Groups);
        Assert.Contains("my-group", config.GetHost("*.my-group.ru")!.Groups);
        Assert.Contains("home", config.GetHost("opi")!.Groups);
        Assert.Contains("home", config.GetHost("hal-0101")!.Groups);
    }
}
