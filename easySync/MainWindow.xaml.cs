using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace easySync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Form members

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private ObservableCollection<SyncPair> pairList = null;
        private WindowState shownState = WindowState.Normal;

        #endregion

        #region Properties with notify support

        public static readonly DependencyProperty StatusTextProperty;
        private const string PROPERTY_STATUSTEXT = "StatusText";

        /// <summary>
        /// The StatusText dependency property.
        /// </summary>
        public string StatusText
        {
            get { return (string)GetValue(StatusTextProperty); }
            set { SetValue(StatusTextProperty, value); }
        }

        #endregion

        #region Form init/load/close

        static MainWindow()
        {
            StatusTextProperty = DependencyProperty.Register(PROPERTY_STATUSTEXT, typeof(string), typeof(MainWindow), new UIPropertyMetadata(null));
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Parameters.DebugLog)
            {
                Log.Write("Loading settings...");
            }

            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon = Properties.Resources.sync_notify;
            //notifyIcon.ShowBalloonTip(1000, "test", "tip text", ToolTipIcon.Info);
            notifyIcon.Visible = true;
            notifyIcon.Text = String.Format(Properties.Resources.NotifyIconTextFormat, Consts.Application.Version);
            notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseDoubleClick);
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();

            System.Windows.Forms.ToolStripMenuItem mClose = new System.Windows.Forms.ToolStripMenuItem();
            mClose.Text = Properties.Resources.MenuItemClose;
            mClose.Image = Properties.Resources.Cancel_16x16;
            mClose.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            mClose.Click += new EventHandler(mClose_Click);

            System.Windows.Forms.ToolStripMenuItem mShow = new System.Windows.Forms.ToolStripMenuItem();
            mShow.Text = Properties.Resources.MenuItemShow;
            mShow.Image = Properties.Resources.Settings_16x16;
            mShow.Click += new EventHandler(mShow_Click);
            mShow.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;

            System.Windows.Forms.ToolStripMenuItem mPause = new System.Windows.Forms.ToolStripMenuItem();
            mPause.Text = Properties.Resources.MenuItemPause;
            mPause.Image = Properties.Resources.Pause_16x16;
            mPause.Click += new EventHandler(mPause_Click);
            mPause.Enabled = false;
            mPause.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;

            notifyIcon.ContextMenuStrip.Items.Add(mShow);
            notifyIcon.ContextMenuStrip.Items.Add(mPause);
            notifyIcon.ContextMenuStrip.Items.Add(mClose);

            if ((Parameters.MainWindowTop != -1) && (Parameters.MainWindowLeft != -1) && (Parameters.MainWindowHeight != -1) && (Parameters.MainWindowWidth != -1))
            {
                this.Left = Parameters.MainWindowLeft;
                this.Top = Parameters.MainWindowTop;
                this.Height = Parameters.MainWindowHeight;
                this.Width = Parameters.MainWindowWidth;
                this.WindowStartupLocation = WindowStartupLocation.Manual;
            }

            if (Parameters.MainWindowMaximized)
            {
                shownState = WindowState.Maximized;
            }

            if (Parameters.DebugLog)
            {
                Log.Write("Loading pairs...");
            }

            pairList = Parameters.LoadPairs();
            lbPairs.DataContext = pairList;
            lbPairs.Items.SortDescriptions.Add(new SortDescription(SyncPair.PROPERTY_LOCALPATH, ListSortDirection.Ascending));

            if (pairList.Any())
            {
                this.Hide();
            }
            else
            {
                // show window
                notifyIcon_MouseDoubleClick(null, null);
            }
            StatusText = Properties.Resources.StatusReady;
            
            if (Parameters.LastUpdateDate.AddDays(Consts.Updater.AutoUpdateCheckDuration) < DateTime.Now)
            {
                Thread updateCheckThread = new Thread(UpdateCheck);
                updateCheckThread.Start();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            StatusText = Properties.Resources.StatusClosing;
            if (Parameters.DebugLog)
            {
                Log.Write("Saving settings...");
            }

            base.OnClosing(e);
            notifyIcon.Visible = false;
            notifyIcon.Dispose();

            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                Parameters.MainWindowLeft = this.Left;
                Parameters.MainWindowTop = this.Top;
                Parameters.MainWindowHeight = this.Height;
                Parameters.MainWindowWidth = this.Width;
            }
            else
            {
                Parameters.MainWindowLeft = this.RestoreBounds.Left;
                Parameters.MainWindowTop = this.RestoreBounds.Top;
                Parameters.MainWindowHeight = this.RestoreBounds.Height;
                Parameters.MainWindowWidth = this.RestoreBounds.Width;
            }
            Parameters.MainWindowMaximized = (this.WindowState == WindowState.Maximized);

            Parameters.Save();

            if (Parameters.DebugLog)
            {
                Log.Write("Saving pairs...");
            }
            Parameters.SavePairs(pairList);


            if (Parameters.DebugLog)
            {
                Log.Write("Stopping pairs...");
            }
            // stop all processing, wait tasks to finish
            Parallel.ForEach(pairList, pair =>
            {
                pair.IsEnabled = false;
            });
        }

        #endregion

        #region Notify icon handlers

        public void ShowAndActivate()
        {
            this.Show();
            this.WindowState = shownState;
            this.Activate();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
            else
            {
                shownState = this.WindowState;
            }
        }

        void notifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.ShowAndActivate();
        }

        void mShow_Click(object sender, EventArgs e)
        {
            notifyIcon_MouseDoubleClick(null, null);
        }

        void mClose_Click(object sender, EventArgs e)
        {
            ApplicationCommands.Close.Execute(null, this);
        }

        void mPause_Click(object sender, EventArgs e)
        {
            ApplicationCommands.Stop.Execute(null, this);
        }
        
        #endregion

        #region List box event handlers

        private void lbPairs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ApplicationCommands.Open.Execute(lbPairs.SelectedItem, this);
            }
        }

        private void lbPairs_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplicationCommands.Open.Execute(lbPairs.SelectedItem, this);
            }
        }

        #endregion

        #region Command processing

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if ((e.Command.Equals(ApplicationCommands.Delete)) || (e.Command.Equals(ApplicationCommands.Open)))
            {
                SyncPair pair = e.Parameter as SyncPair;
                e.CanExecute = (pair != null);
            }
            else if ((e.Command.Equals(ApplicationCommands.New)) || (e.Command.Equals(ApplicationCommands.Close)) || (e.Command.Equals(ApplicationCommands.Stop)) ||
                (e.Command.Equals(ApplicationCommands.Help)))
            {
                e.CanExecute = true;
            }
            else if (e.Command.Equals(ApplicationCommands.Redo))
            {
                SyncPair pair = e.Parameter as SyncPair;
                e.CanExecute = ((pair != null) && (!pair.IsEnabled));
            }
        }


        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command.Equals(ApplicationCommands.Open))
            {
                SyncPair edited = e.Parameter as SyncPair;
                DetailsWindow details = new DetailsWindow(edited);
                details.Owner = this;
                if (details.ShowDialog() == true)
                {
                    pairList.Remove(edited);
                    pairList.Add(details.EditedPair);
                }
            }
            else if (e.Command.Equals(ApplicationCommands.Delete))
            {
                SyncPair pair = e.Parameter as SyncPair;

                if (MessageBox.Show(String.Format(Properties.Resources.DeletePairFormat, pair.LocalPath, pair.RemotePath),
                    Properties.Resources.DeletePairTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    pair.IsEnabled = false;
                    pair.ClearMetadata();
                    pairList.Remove(pair);
                    StatusText = Properties.Resources.StatusDeleted;
                }
            }
            else if (e.Command.Equals(ApplicationCommands.New))
            {
                DetailsWindow details = new DetailsWindow(null);
                details.Owner = this;
                if (details.ShowDialog() == true)
                {
                    pairList.Add(details.EditedPair);
                    StatusText = Properties.Resources.StatusAdded;
                }                
            }
            else if (e.Command.Equals(ApplicationCommands.Close))
            {
                this.Close();
            }
            else if (e.Command.Equals(ApplicationCommands.Stop))
            {
                cbPauseActivities_Checked(null, null);
            }
            else if (e.Command.Equals(ApplicationCommands.Help))
            {
                AboutWindow about = new AboutWindow();
                about.Owner = this;
                about.ShowDialog();
            }
            else if (e.Command.Equals(ApplicationCommands.Redo))
            {
                SyncPair pair = e.Parameter as SyncPair;
                if ((pair != null) && (!pair.IsEnabled))
                {
                    pair.IsEnabled = true;
                    StatusText = Properties.Resources.StatusStarted;
                }

            }
        }

        #endregion

        #region Pause handlers

        private void cbPauseActivities_Checked(object sender, RoutedEventArgs e)
        {
            Parallel.ForEach(pairList, pair =>
            {
                pair.Pause = true;
            });
            StatusText = Properties.Resources.StatusPaused;
        }

        private void cbPauseActivities_Unchecked(object sender, RoutedEventArgs e)
        {
            Parallel.ForEach(pairList, pair =>
            {
                pair.Pause = false;
            });
            StatusText = Properties.Resources.StatusReady;
        }

        #endregion

        #region Background update check routine

        private void UpdateCheck()
        {
            try
            {
                UpdateHelper helper = new UpdateHelper(Consts.Application.UpdateCheckURL, Consts.Application.Version);
                helper.IsUpdateAvailable();
                Parameters.LastUpdateDate = DateTime.Now;
                if (UpdateHelper.IsVersionHigher(helper.AvailableVersion()))
                {
                    this.Dispatcher.Invoke(new Action(delegate()
                        {
                            this.Show();
                            this.WindowState = WindowState.Normal;
                            this.Activate();

                            if (MessageBox.Show(String.Format(Properties.Resources.UpdateAvailableFormat, Consts.Application.Version, helper.AvailableVersion()),
                                Properties.Resources.UpdateAvailableTitle, MessageBoxButton.OKCancel, MessageBoxImage.Information,
                                MessageBoxResult.OK) == MessageBoxResult.OK)
                            {
                                helper.StartUpdate(this);
                            }
                        }));
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }

        #endregion

    }
}
