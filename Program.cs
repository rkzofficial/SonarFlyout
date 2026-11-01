using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace SonarFlyout;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "SonarFlyout.SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance)
            return;

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            new App();
        });
    }
}
