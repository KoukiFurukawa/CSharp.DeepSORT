using CSharp.DeepSORT.ViewModels;
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

            // 初めに表示する画面の設定
            var mainWindow = new MainWindow();
            mainWindow.DataContext = new MainWindowViewModel();

            // 表示
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