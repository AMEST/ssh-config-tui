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
services.AddSingleton<SessionService>();
services.AddSingleton<TemplateService>();
var debugMode = args.Contains("--debug");
services.AddSingleton(_ => new DebugLogger(debugMode));

var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<DebugLogger>();

Application.Init();

Console.CancelKeyPress += (_, args) =>
{
    //args.Cancel = true;
    logger.Write("SIGINT received, requesting stop");
    Application.RequestStop();
};

try
{
    logger.Write("Run TUI");
    var appService = provider.GetRequiredService<ApplicationService>();
    var clipboard = provider.GetRequiredService<ClipboardService>();
    var session = provider.GetRequiredService<SessionService>();
    var templates = provider.GetRequiredService<TemplateService>();
    var mainWindow = new MainWindow(appService, clipboard, session, templates, logger);
    logger.Write("Initialize main window");
    mainWindow.Initialize();
    logger.Write("Run application");
    Application.Run(mainWindow);
}
catch (Exception ex)
{
    MessageBox.ErrorQuery("Error", $"Error: {ex.Message}", "OK");
    logger.WriteError(ex.StackTrace ?? "(no stack trace)");
}
finally
{
    Application.Shutdown();
}
