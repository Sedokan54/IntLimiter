using System.Windows;
using System.Linq;
using NetLimiterClone.Services;

namespace NetLimiterClone
{
    public partial class App : Application
    {
        private bool _isCliMode = false;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Check if running in CLI mode
            if (e.Args != null && e.Args.Length > 0)
            {
                _isCliMode = true;
                HandleCliMode(e.Args);
                return;
            }
            
            // Check if running as administrator for GUI mode
            if (!IsRunningAsAdministrator())
            {
                MessageBox.Show("This application requires administrator privileges to monitor and control network traffic.", 
                    "Administrator Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }
            
            // Check for --start-minimized flag
            if (e.Args != null && e.Args.Contains("--start-minimized"))
            {
                // Start minimized to system tray
                Current.MainWindow = new MainWindow();
                Current.MainWindow.WindowState = WindowState.Minimized;
                Current.MainWindow.ShowInTaskbar = false;
                Current.MainWindow.Show();
            }
        }
        
        private void HandleCliMode(string[] args)
        {
            try
            {
                var cliService = new CLIService();
                var result = cliService.ProcessArguments(args);
                System.Console.WriteLine(result);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                Shutdown();
            }
        }

        private bool IsRunningAsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }
}