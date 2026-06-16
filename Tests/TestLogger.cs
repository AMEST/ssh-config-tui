using SshConfigTui.Infrastructure;

namespace SshConfigTui.Tests;

internal static class TestLogger
{
    public static DebugLogger Create()
    {
        return new DebugLogger(Path.Combine(Path.GetTempPath(), $"ssh-test-{Guid.NewGuid()}.log"));
    }
}
