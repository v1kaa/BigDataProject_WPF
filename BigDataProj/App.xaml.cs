using System.Configuration;
using System.Data;
using System.Windows;
using System.Data.SQLite;


namespace BigDataProj
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Inicjalizacja SQLite
            SQLitePCL.Batteries.Init();

        }

    }
}
