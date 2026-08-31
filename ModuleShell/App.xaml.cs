using System.Windows;
namespace MATeacher.ModuleShell;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Any(a => string.Equals(a, "--self-test", StringComparison.OrdinalIgnoreCase)))
        {
            var exitCode = Task.Run(ReleaseSelfTest.RunAsync).GetAwaiter().GetResult();
            Shutdown(exitCode);
            return;
        }
        new MainWindow().Show();
    }
}
