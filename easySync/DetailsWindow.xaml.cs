using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace easySync
{
    /// <summary>
    /// Interaction logic for DetailsWindow.xaml
    /// </summary>
    public partial class DetailsWindow : Window
    {

        #region Public window properties

        public SyncPair EditedPair { get; private set; }

        #endregion

        #region Form init/load

        public DetailsWindow(SyncPair initialPair)
        {
            InitializeComponent();
            cbMonitoring.DataContext = SyncMonitoring.Strategies;
            cbBackup.DataContext = SyncBackup.BackupModes;
            this.EditedPair = initialPair;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.EditedPair != null)
            {
                tbSourceFolder.Text = this.EditedPair.LocalPath;
                tbDestinationFolder.Text = this.EditedPair.RemotePath;
                cbEnabled.IsChecked = this.EditedPair.IsEnabled;
                cbMonitoring.SelectedItem = SyncMonitoring.Strategies.SingleOrDefault(s => (s.ID.Equals(this.EditedPair.Strategy.ID)));
                cbBackup.SelectedItem = SyncBackup.BackupModes.SingleOrDefault(s => (s.ID.Equals(this.EditedPair.BackupMode.ID)));
                tbBackupFolder.Text = this.EditedPair.BackupPath;
            }
            else
            {
                cbMonitoring.SelectedItem = SyncMonitoring.Strategies.SingleOrDefault(s => (s.ID.Equals(SyncMonitoring.STRATEGY_ONSTART)));
                cbBackup.SelectedItem = SyncBackup.BackupModes.SingleOrDefault(s => (s.ID.Equals(SyncBackup.BACKUP_NOBACKUPS)));
            }
        }

        #endregion

        #region Command handling

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command.Equals(ApplicationCommands.Save))
            {
                if (!Directory.Exists(tbSourceFolder.Text))
                {
                    if (MessageBox.Show(String.Format(Properties.Resources.SourceFolderDoesNotExistFormat, tbSourceFolder.Text),
                        Properties.Resources.FolderDoesNotExistTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                if (!Directory.Exists(tbDestinationFolder.Text))
                {
                    if (MessageBox.Show(String.Format(Properties.Resources.DestinationFolderDoesNotExistFormat, tbDestinationFolder.Text),
                        Properties.Resources.FolderDoesNotExistTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                if ((!(cbBackup.SelectedItem as SyncBackupMode).ID.Equals(SyncBackup.BACKUP_NOBACKUPS)) &&
                    (!Directory.Exists(tbBackupFolder.Text)))
                {
                    if (MessageBox.Show(String.Format(Properties.Resources.BackupFolderDoesNotExistFormat, tbBackupFolder.Text),
                        Properties.Resources.FolderDoesNotExistTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                SyncMonitoringStrategy strategy = cbMonitoring.SelectedItem as SyncMonitoringStrategy;
                SyncBackupMode backupMode = cbBackup.SelectedItem as SyncBackupMode;

                if (this.EditedPair == null)
                {
                    this.DialogResult = true;
                    this.EditedPair = new SyncPair(tbSourceFolder.Text, tbDestinationFolder.Text, strategy.ID, backupMode.ID, tbBackupFolder.Text);
                }
                else
                {
                    if ((!tbSourceFolder.Text.Equals(this.EditedPair.LocalPath)) || (!tbDestinationFolder.Text.Equals(this.EditedPair.RemotePath)) ||
                        (!strategy.Equals(this.EditedPair.Strategy)) || (!backupMode.Equals(this.EditedPair.BackupMode)) ||
                        ((String.IsNullOrEmpty(tbBackupFolder.Text)) && (!String.IsNullOrEmpty(this.EditedPair.BackupPath))) ||
                        (!tbBackupFolder.Text.Equals(this.EditedPair.BackupPath)))
                    {
                        // there are changes in definitions, need to recreate the pair                        
                        this.EditedPair.IsEnabled = false; // stopping original pair
                        this.EditedPair.ClearMetadata(); // remove old metadata files

                        this.DialogResult = true;
                        this.EditedPair = new SyncPair(tbSourceFolder.Text, tbDestinationFolder.Text, strategy.ID, backupMode.ID, tbBackupFolder.Text);
                    }
                }
                this.EditedPair.IsEnabled = (cbEnabled.IsChecked.HasValue ? cbEnabled.IsChecked.Value : false);
                this.Close();
            }
            else if (e.Command.Equals(ApplicationCommands.Close))
            {
                this.Close();
            }
            else if (e.Command.Equals(ApplicationCommands.Open))
            {
                TextBox target = e.Parameter as TextBox;
                System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    target.Text = dialog.SelectedPath;
                }
            }
        }

        #endregion

    }
}
