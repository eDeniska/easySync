using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace easySync
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {

        #region Properties with notify support

        public static readonly DependencyProperty IsUpdateCheckingProperty;
        private const string PROPERTY_ISUPDATECHECKING = "IsUpdateChecking";

        /// <summary>
        /// The IsUpdateChecking dependency property.
        /// </summary>
        public bool IsUpdateChecking
        {
            get { return (bool)GetValue(IsUpdateCheckingProperty); }
            set { SetValue(IsUpdateCheckingProperty, value); }
        }

        #endregion

        #region Form init/load/close

        static AboutWindow()
        {
            IsUpdateCheckingProperty = DependencyProperty.Register(PROPERTY_ISUPDATECHECKING, typeof(bool), typeof(AboutWindow), new UIPropertyMetadata(null));
        }

        public AboutWindow()
        {
            InitializeComponent();
            AssemblyName name = Assembly.GetExecutingAssembly().GetName();
            tbVersion.Text = String.Format(Consts.Application.VersionInfoFormat, Consts.Application.Version, Consts.Application.VersionCodeName);
            tbBuild.Text = String.Format(Consts.Application.BuildInfoFormat, name.Version.ToString(),
                (new DateTime(2000, 1, 1)).AddDays(name.Version.Build), name.ProcessorArchitecture.ToString());
            this.IsUpdateChecking = false;
        }

        #endregion

        #region Link click handlers

        private void TextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start((sender as TextBlock).Text);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }

        #endregion

        #region Command handlers

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Command.Equals(ApplicationCommands.Find))
            {
                e.CanExecute = !this.IsUpdateChecking;
            }
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command.Equals(ApplicationCommands.Find))
            {
                try
                {
                    Thread updateCheckThread = new Thread(UpdateCheck);
                    updateCheckThread.Start();
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }
            }
        }

        private void UpdateCheck()
        {
            try
            {
                this.Dispatcher.Invoke(new Action(delegate()
                   {
                       this.IsUpdateChecking = true;
                   }));

                UpdateHelper helper = new UpdateHelper(Consts.Application.UpdateCheckURL, Consts.Application.Version);
                helper.IsUpdateAvailable();
                Parameters.LastUpdateDate = DateTime.Now;
                if (UpdateHelper.IsVersionHigher(helper.AvailableVersion()))
                {
                    this.Dispatcher.Invoke(new Action(delegate()
                    {
                        if (MessageBox.Show(String.Format(Properties.Resources.UpdateAvailableFormat, Consts.Application.Version, helper.AvailableVersion()),
                            Properties.Resources.UpdateAvailableTitle, MessageBoxButton.OKCancel, MessageBoxImage.Information,
                            MessageBoxResult.OK) == MessageBoxResult.OK)
                        {
                            helper.StartUpdate(this.Owner);
                        }
                    }));
                }
                else
                {
                    MessageBox.Show(String.Format(Properties.Resources.NoUpdatesFormat, Consts.Application.Version),
                        Properties.Resources.NoUpdatesTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
            finally
            {
                this.Dispatcher.Invoke(new Action(delegate()
                {
                    this.IsUpdateChecking = false;
                }));
            }
        }


        #endregion

    }
}
