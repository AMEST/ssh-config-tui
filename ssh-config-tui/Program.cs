using Microsoft.Extensions.DependencyInjection;
using SshConfigTui.Application;
using SshConfigTui.Infrastructure;
using SshConfigTui.UI;
using Terminal.Gui;

var services = new ServiceCollection();

services.AddSingleton<SshConfigParser>();
services.AddSingleton<SshConfigRepository>();
services.AddSingleton<ConfigService>();
services.AddSingleton<GroupService>();
services.AddSingleton<ApplicationService>();
services.AddSingleton<ClipboardService>();

var provider = services.BuildServiceProvider();

Application.Init();

try
{
    var appService = provider.GetRequiredService<ApplicationService>();
    var mainWindow = new MainWindow(appService);

    await mainWindow.InitializeAsync();

    Application.Run(mainWindow);
}
catch (Exception ex)
{
    MessageBox.ErrorQuery("Error", $"Fatal error: {ex.Message}", "OK");
    Console.Error.WriteLine(ex.StackTrace);
}
finally
{
    Application.Shutdown();
}
