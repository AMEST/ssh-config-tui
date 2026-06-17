using SshConfigTui.Infrastructure;

namespace SshConfigTui.Tests;

internal static class TestLogger
{
    public static DebugLogger Create()
    {
        return new DebugLogger(true);
    }
}
