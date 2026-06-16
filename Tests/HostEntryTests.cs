using SshConfigTui.Domain;

namespace SshConfigTui.Tests;

public class HostEntryTests
{
    [Fact]
    public void FromHostSection_WithAllDirectives_MapsCorrectly()
    {
        var section = new HostSection
        {
            Pattern = "myserver",
            Groups = new() { "work" },
            Directives = new()
            {
                new SshDirective { Key = "HostName", Value = "myserver.local" },
                new SshDirective { Key = "User", Value = "admin" },
                new SshDirective { Key = "Port", Value = "2222" },
                new SshDirective { Key = "IdentityFile", Value = "~/.ssh/id_rsa" },
                new SshDirective { Key = "ProxyJump", Value = "bastion" },
                new SshDirective { Key = "ForwardAgent", Value = "yes" },
            }
        };

        var entry = HostEntry.FromHostSection(section);

        Assert.Equal("myserver", entry.Name);
        Assert.Equal("myserver.local", entry.HostName);
        Assert.Equal("admin", entry.User);
        Assert.Equal(2222, entry.Port);
        Assert.Equal("~/.ssh/id_rsa", entry.IdentityFile);
        Assert.Equal("bastion", entry.ProxyJump);
        Assert.True(entry.ForwardAgent);
        Assert.Contains("work", entry.Groups);
    }

    [Fact]
    public void FromHostSection_WithLocalForward_ParsesCorrectly()
    {
        var section = new HostSection
        {
            Pattern = "tun",
            Directives = new()
            {
                new SshDirective { Key = "LocalForward", Value = "8080 localhost:80" },
                new SshDirective { Key = "LocalForward", Value = "3000 localhost:3000" },
            }
        };

        var entry = HostEntry.FromHostSection(section);

        Assert.Equal(2, entry.LocalForwards.Count);
        Assert.Contains("8080 localhost:80", entry.LocalForwards);
        Assert.Contains("3000 localhost:3000", entry.LocalForwards);
    }

    [Fact]
    public void FromHostSection_WithRemoteForward_ParsesCorrectly()
    {
        var section = new HostSection
        {
            Pattern = "remote",
            Directives = new()
            {
                new SshDirective { Key = "RemoteForward", Value = "9090 remote:80" },
            }
        };

        var entry = HostEntry.FromHostSection(section);

        Assert.Single(entry.RemoteForwards);
        Assert.Contains("9090 remote:80", entry.RemoteForwards);
    }

    [Fact]
    public void FromHostSection_WithUnknownDirective_AddsToExtra()
    {
        var section = new HostSection
        {
            Pattern = "custom",
            Directives = new()
            {
                new SshDirective { Key = "StrictHostKeyChecking", Value = "no" },
                new SshDirective { Key = "ServerAliveInterval", Value = "60" },
            }
        };

        var entry = HostEntry.FromHostSection(section);

        Assert.Equal(2, entry.ExtraDirectives.Count);
        Assert.Equal("no", entry.ExtraDirectives["StrictHostKeyChecking"]);
        Assert.Equal("60", entry.ExtraDirectives["ServerAliveInterval"]);
    }

    [Fact]
    public void FromHostSection_NoDirectives_UsesDefaults()
    {
        var section = new HostSection
        {
            Pattern = "defaults",
            Groups = new() { "test" },
        };

        var entry = HostEntry.FromHostSection(section);

        Assert.Equal("defaults", entry.Name);
        Assert.Empty(entry.HostName);
        Assert.Empty(entry.User);
        Assert.Equal(22, entry.Port);
        Assert.Empty(entry.IdentityFile);
        Assert.Empty(entry.ProxyJump);
        Assert.False(entry.ForwardAgent);
        Assert.Contains("test", entry.Groups);
    }

    [Fact]
    public void FromHostSection_ForwardAgentNo_ParsesAsFalse()
    {
        var section = new HostSection
        {
            Pattern = "test",
            Directives = { new SshDirective { Key = "ForwardAgent", Value = "no" } }
        };

        var entry = HostEntry.FromHostSection(section);

        Assert.False(entry.ForwardAgent);
    }

    [Fact]
    public void FromHostSection_PortInvalid_DefaultsTo22()
    {
        var section = new HostSection
        {
            Pattern = "test",
            Directives = { new SshDirective { Key = "Port", Value = "not-a-number" } }
        };

        var entry = HostEntry.FromHostSection(section);

        Assert.Equal(22, entry.Port);
    }
}
