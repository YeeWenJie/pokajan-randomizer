using System.Windows;
using System.Windows.Threading;

namespace PokajanRandomizer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        base.OnStartup(e);
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.ToString(), "Pokajan Randomizer error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
