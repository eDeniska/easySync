using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Shell;

namespace easySync
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        private const string UniqueID = "easySync.{60BBBACE-EB31-4BB1-B31A-29AD050D602E}";

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance(UniqueID))
            {
                App application = new App();

                application.InitializeComponent();
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            if (this.MainWindow != null)
            {
                (this.MainWindow as MainWindow).ShowAndActivate();
            }

            return true;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Write("Starting...");
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            Log.Write("Exiting...");
        }
    }
}
