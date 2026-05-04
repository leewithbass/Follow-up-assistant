using System.Windows;
using FloatBrowser.App.Application;
using FloatBrowser.App.Infrastructure;

namespace FloatBrowser.App;

public partial class App : System.Windows.Application
{
    public AppHost? Host { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Host = new AppHost();
        await Host.InitializeAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (Host is not null)
        {
            await Host.ShutdownAsync();
            Host.Dispose();
        }

        base.OnExit(e);
    }
}
