using CSharp.DeepSORT;
using CSharp.DeepSORT.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace Csharp.DeepSORT
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            Debug.WriteLine("App Launch.");

            /* Thread 処理の下準備 -------------------------------------------------------------- */
            try
            {
                ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);
                ThreadPool.SetMinThreads(workerThreads * 2, completionPortThreads * 2);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.Message} - {ex.Source}");
            }

            ServiceProvider = PrepareServiceCollection.Initialize();

            var mainWindow = new MainWindow();
            mainWindow.DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Debug.WriteLine("Exit app.");
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // 例外処理
            MessageBox.Show("An unhandled exception occurred: " + e.Exception.Message);
            e.Handled = true;
        }
    }

}