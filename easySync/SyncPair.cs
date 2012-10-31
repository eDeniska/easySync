using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Files;
using System.Collections.Generic;

namespace easySync
{
    public class SyncPair : INotifyPropertyChanged
    {

        #region Public class properties

        public const String XML_ROOT = "SyncPair";
        public const String PROPERTY_LOCALPATH = "LocalPath";
        public const String PROPERTY_REMOTEPATH = "RemotePath";
        public const String PROPERTY_STRATEGY = "Strategy";
        public const String PROPERTY_BACKUPMODE = "BackupMode";
        public const String PROPERTY_BACKUPPATH = "BackupPath";
        public const String PROPERTY_LOCALGUID = "LocalGUID";
        public const String PROPERTY_REMOTEGUID = "RemoteGUID";

        public Guid LocalGUID { get; private set; }
        public Guid RemoteGUID { get; private set; }
        public String LocalPath { get; private set; }
        public String RemotePath { get; private set; }
        public String BackupPath { get; private set; }
        public SyncMonitoringStrategy Strategy { get; private set; }
        public SyncBackupMode BackupMode { get; private set; }

        #endregion

        #region Properties with notify support

        public const String PROPERTY_ISENABLED = "IsEnabled";
        public const String PROPERTY_PAUSE = "Pause";
        public const String PROPERTY_STATE = "State";
        private bool _isEnabled;
        private bool _isPaused = false;
        private bool _skipWait = true;
        private SyncPairState _state = SyncPairState.Unknown;

        public SyncPairState State
        {
            get
            {
                return _state;
            }

            private set
            {
                if (Parameters.DebugLog)
                {
                    Log.Write(String.Format("Pair [{0}]<=>[{1}], state: {2}->{3}", LocalPath, RemotePath, _state.ToString(), value.ToString()));
                }
                _state = value;
                OnPropertyChanged(PROPERTY_STATE);
            }
        }

        public bool IsEnabled
        {
            get 
            { 
                return _isEnabled; 
            }
            set
            {
                _isEnabled = value;
                if (_isEnabled)
                {
                    Start();
                }
                else
                {
                    Stop();
                }
                OnPropertyChanged(PROPERTY_ISENABLED);
            }
        }

        /// <summary>
        /// Signal to pause/resume worker thread
        /// </summary>
        public bool Pause
        {
            get
            {
                return _isPaused;
            }

            set
            {
                _isPaused = value;
                _skipWait = true;
                lock (waitWorker)
                {
                    Monitor.Pulse(waitWorker);
                }
                OnPropertyChanged(PROPERTY_PAUSE);
            }
        }

        #endregion
        
        #region Class members/properties

        private FileSyncProvider localProvider;
        private FileSyncProvider remoteProvider;
        private FileSystemWatcher localFSWatcher;
        private FileSystemWatcher remoteFSWatcher;

        private int fsChangesCounter = 1; // this will trigger initialization on program start for AutoSync mode
        private object lockFSCounter = new object();
        private Thread workerThread = null;
        private object waitWorker = new object();
        private Thread tickerThread = null;
        private object waitTicker = new object();

        private int FSChangesCounter
        {
            get { return fsChangesCounter; }

            set
            {
                lock (lockFSCounter)
                {
                    fsChangesCounter = value;
                }
            }
        }

        #endregion

        #region Pair init

        private void setValues(String localPath, String remotePath, String strategy, String backupMode, String backupPath, String localGUID, String remoteGUID)
        {
            if (String.IsNullOrEmpty(localGUID))
            {
                LocalGUID = Guid.NewGuid();
            }
            else
            {
                Guid guid;
                if (Guid.TryParse(localGUID, out guid))
                {
                    LocalGUID = guid;
                }
                else
                {
                    LocalGUID = Guid.NewGuid();
                }
            }
            if (String.IsNullOrEmpty(remoteGUID))
            {
                RemoteGUID = Guid.NewGuid();
            }
            else
            {
                Guid guid;
                if (Guid.TryParse(remoteGUID, out guid))
                {
                    RemoteGUID = guid;
                }
                else
                {
                    RemoteGUID = Guid.NewGuid();
                }
            }
            LocalPath = localPath;
            RemotePath = remotePath;
            BackupPath = String.IsNullOrEmpty(backupPath) ? String.Empty : backupPath;

            Strategy = SyncMonitoring.Strategies.SingleOrDefault(s => (s.ID.Equals(strategy)));

            if (Strategy == null)
            {
                Strategy = SyncMonitoring.Strategies.SingleOrDefault(s => (s.ID.Equals(SyncMonitoring.STRATEGY_ONSTART)));
            }

            BackupMode = SyncBackup.BackupModes.SingleOrDefault(s => (s.ID.Equals(backupMode)));

            if (BackupMode == null)
            {
                BackupMode = SyncBackup.BackupModes.SingleOrDefault(s => (s.ID.Equals(SyncBackup.BACKUP_NOBACKUPS)));
            }

            State = SyncPairState.NotReady;
        }

        public SyncPair(String xml)
        {
            // init from xml string
            XmlDocument doc = new XmlDocument();
            doc.InnerXml = xml;
            XmlElement root = doc.DocumentElement;
            setValues(root.Attributes[PROPERTY_LOCALPATH].InnerText, root.Attributes[PROPERTY_REMOTEPATH].InnerText, 
                root.Attributes[PROPERTY_STRATEGY].InnerText, root.Attributes[PROPERTY_BACKUPMODE].InnerText, 
                root.HasAttribute(PROPERTY_BACKUPPATH) ? root.Attributes[PROPERTY_BACKUPPATH].InnerText : String.Empty,
                root.Attributes[PROPERTY_LOCALGUID].InnerText,
                root.Attributes[PROPERTY_REMOTEGUID].InnerText);

            IsEnabled = Boolean.Parse(root.Attributes[PROPERTY_ISENABLED].InnerText);
        }

        public String SaveAsXML()
        {
            XmlDocument doc = new XmlDocument();
            XmlNode root = doc.CreateElement(XML_ROOT);

            XmlAttribute attrLocalGuid = doc.CreateAttribute(PROPERTY_LOCALGUID);
            attrLocalGuid.InnerText = this.LocalGUID.ToString();
            root.Attributes.Append(attrLocalGuid);

            XmlAttribute attrRemoteGuid = doc.CreateAttribute(PROPERTY_REMOTEGUID);
            attrRemoteGuid.InnerText = this.RemoteGUID.ToString();
            root.Attributes.Append(attrRemoteGuid);

            XmlAttribute attrLocalPath = doc.CreateAttribute(PROPERTY_LOCALPATH);
            attrLocalPath.InnerText = this.LocalPath;
            root.Attributes.Append(attrLocalPath);

            XmlAttribute attrRemotePath = doc.CreateAttribute(PROPERTY_REMOTEPATH);
            attrRemotePath.InnerText = this.RemotePath;
            root.Attributes.Append(attrRemotePath);

            XmlAttribute attrBackupPath = doc.CreateAttribute(PROPERTY_BACKUPPATH);
            attrBackupPath.InnerText = this.BackupPath;
            root.Attributes.Append(attrBackupPath);

            XmlAttribute attrBackupMode = doc.CreateAttribute(PROPERTY_BACKUPMODE);
            attrBackupMode.InnerText = this.BackupMode.ID;
            root.Attributes.Append(attrBackupMode);

            XmlAttribute attrStrategy = doc.CreateAttribute(PROPERTY_STRATEGY);
            attrStrategy.InnerText = this.Strategy.ID;
            root.Attributes.Append(attrStrategy);

            XmlAttribute attrIsEnabled = doc.CreateAttribute(PROPERTY_ISENABLED);
            attrIsEnabled.InnerText = this.IsEnabled.ToString();
            root.Attributes.Append(attrIsEnabled);

            doc.AppendChild(root);
            return doc.InnerXml;
        }

        public SyncPair(String localPath, String remotePath, String strategy, String backupMode, String backupPath = null, String localGUID = null, String remoteGUID = null)
        {
            setValues(localPath, remotePath, strategy, backupMode, backupPath, localGUID, remoteGUID);
        }

        /// <summary>
        /// Tries to initialize folder pair, result is set via State
        /// </summary>
        private void Init()
        {
            try
            {
                State = SyncPairState.Initializing;
                // verify paths
                LocalPath = Path.GetFullPath(LocalPath);
                RemotePath = Path.GetFullPath(RemotePath);
                String metafileFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Consts.Application.ProfileFolder);
                String localMetadataFile = String.Format(Consts.Application.MetadataFileFormat, LocalGUID.ToString());
                String remoteMetadataFile = String.Format(Consts.Application.MetadataFileFormat, RemoteGUID.ToString());

                // set sync framework environment
                FileSyncScopeFilter filterLocal = new FileSyncScopeFilter();
                FileSyncScopeFilter filterRemote = new FileSyncScopeFilter();
                
                if (!Directory.Exists(metafileFolder))
                {
                    Directory.CreateDirectory(metafileFolder);
                }

                if (localProvider != null)
                {
                    localProvider.Dispose();
                    localProvider = null;
                }

                if (remoteProvider != null)
                {
                    remoteProvider.Dispose();
                    remoteProvider = null;
                }

                localProvider = new FileSyncProvider(LocalGUID, LocalPath, filterLocal,
                    FileSyncOptions.RecycleConflictLoserFiles | FileSyncOptions.RecycleDeletedFiles,
                    metafileFolder, localMetadataFile, LocalPath, null);

                remoteProvider = new FileSyncProvider(RemoteGUID, RemotePath, filterRemote,
                    FileSyncOptions.RecycleConflictLoserFiles | FileSyncOptions.RecycleDeletedFiles,
                    metafileFolder, remoteMetadataFile, RemotePath,  null);

                localProvider.Configuration.ConflictResolutionPolicy = ConflictResolutionPolicy.SourceWins;
                remoteProvider.Configuration.ConflictResolutionPolicy = ConflictResolutionPolicy.SourceWins;


                if (Strategy.AutoSync)
                {
                    if (localFSWatcher != null)
                    {
                        localFSWatcher.Dispose();
                        localFSWatcher = null;
                    }
                    if (remoteFSWatcher != null)
                    {
                        remoteFSWatcher.Dispose();
                        remoteFSWatcher = null;
                    }

                    localFSWatcher = new FileSystemWatcher(LocalPath);
                    remoteFSWatcher = new FileSystemWatcher(RemotePath);

                    localFSWatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime |
                        NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite |
                        NotifyFilters.Security | NotifyFilters.Size;
                    remoteFSWatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime |
                        NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite |
                        NotifyFilters.Security | NotifyFilters.Size;

                    localFSWatcher.Filter = Consts.FileSystem.WatcherFilter;
                    remoteFSWatcher.Filter = Consts.FileSystem.WatcherFilter;

                    localFSWatcher.IncludeSubdirectories = true;
                    remoteFSWatcher.IncludeSubdirectories = true;

                    localFSWatcher.Changed += FSEventHandler;
                    remoteFSWatcher.Changed += FSEventHandler;

                    localFSWatcher.Deleted += FSEventHandler;
                    remoteFSWatcher.Deleted += FSEventHandler;

                    localFSWatcher.Created += FSEventHandler;
                    remoteFSWatcher.Created += FSEventHandler;

                    localFSWatcher.Renamed += FSRenamedHander;
                    remoteFSWatcher.Renamed += FSRenamedHander;

                    localFSWatcher.Error += FSErrorHandler;
                    remoteFSWatcher.Error += FSErrorHandler;

                    localFSWatcher.EnableRaisingEvents = true;
                    remoteFSWatcher.EnableRaisingEvents = true;

                    if (Parameters.DebugLog)
                    {
                        Log.Write(String.Format("Pair [{0}]<=>[{1}] - file system watchers set, buffers local={2}, remote={3}...", LocalPath, RemotePath, 
                            localFSWatcher.InternalBufferSize, remoteFSWatcher.InternalBufferSize));
                    }
                }
                State = SyncPairState.Ready;
            }
            catch (Exception ex)
            {
                Log.Write(String.Format("Problems during initialization of pair [{0}]<=>[{1}], initialization will continue.", LocalPath, RemotePath), ex);
                State = SyncPairState.NotInitialized;
            }
        }

        #endregion

        #region Filesystem event handlers

        private void FSEventHandler(object sender, FileSystemEventArgs e)
        {
            signalFSChanges();
        }

        private void FSRenamedHander(object sender, RenamedEventArgs e)
        {
            signalFSChanges();
        }

        private void FSErrorHandler(object sender, ErrorEventArgs e)
        {
            signalFSChanges();
        }

        private void signalFSChanges()
        {
            ++FSChangesCounter;
            lock (waitWorker)
            {
                Monitor.Pulse(waitWorker);
            }
        }

        #endregion

        #region Sync routine

        /// <summary>
        /// Performs folder pair synchronization; returns true, if there were any changes
        /// </summary>
        /// <returns>true, if sync changed any data; false, if no data changed; null, if init failed or paused</returns>
        private bool? Sync()
        {
            if (State != SyncPairState.Ready)
            {
                // need to init pair first
                Init();

                // if initialization failed signal not to restart sync immediately
                // rather it will wait for the next event (FS event, or tick)
                if (State != SyncPairState.Ready) return null;
            }

            State = SyncPairState.Synchronizing;

            SyncOrchestrator agent = new SyncOrchestrator();
            agent.LocalProvider = localProvider;
            agent.RemoteProvider = remoteProvider;
            agent.Direction = SyncDirectionOrder.DownloadAndUpload;

            SyncOperationStatistics stats = agent.Synchronize();

            State = SyncPairState.Ready;

            if ((stats.DownloadChangesFailed + stats.UploadChangesFailed) > 0)
            {
                Log.Write(String.Format("Problems during synchronization of [{0}]<=>[{1}]: {2} download errors, {3} upload errors. Pair processing will continue.",
                    LocalPath, RemotePath, stats.DownloadChangesFailed, stats.UploadChangesFailed));

                // signal to restart sync process
                return true;
            }

            return ((stats.DownloadChangesApplied + stats.UploadChangesApplied) > 0);
        }
        
        #endregion

        #region Backup routines
        
        private void recursiveDelete(String path)
        {
            Stack<DirectoryInfo> folders = new Stack<DirectoryInfo>();
            folders.Push(new DirectoryInfo(path));
            while (folders.Any())
            {
                DirectoryInfo folder = folders.Pop();
                folder.Attributes = folder.Attributes & ~(FileAttributes.Archive | FileAttributes.ReadOnly | FileAttributes.Hidden);
                foreach (DirectoryInfo d in folder.GetDirectories())
                {
                    folders.Push(d);
                }
                foreach (FileInfo f in folder.GetFiles())
                {
                    f.Attributes = f.Attributes & ~(FileAttributes.Archive | FileAttributes.ReadOnly | FileAttributes.Hidden);
                    f.Delete();
                }
            }
            Directory.Delete(path, true);
        }

        /// <summary>
        /// Delete archives that are older than required age
        /// </summary>
        private void purgeOldArchives()
        {
            try
            {
                if (Parameters.DebugLog)
                {
                    Log.Write(String.Format("Pair [{0}]<=>[{1}], purging old backups in [{2}]", LocalPath, RemotePath, BackupPath));
                } 
                
                foreach (String folder in Directory.GetDirectories(BackupPath, Consts.FileSystem.WatcherFilter, SearchOption.TopDirectoryOnly))
                {
                    if (Directory.GetLastWriteTime(folder).Date.AddDays(BackupMode.KeepDays) < DateTime.Now)
                    {
                        Log.Write(String.Format("Pair [{0}]<=>[{1}], deleting outdated backup [{2}]", LocalPath, RemotePath, folder));
                        recursiveDelete(folder);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(String.Format("Error purging old backups for pair [{0}]<=>[{1}], backup folder [{2}]", LocalPath, RemotePath, BackupPath), ex);
            }
        }

        private DateTime latestBackupDate()
        {
            try
            {
                DateTime oldest = DateTime.MinValue;
                foreach (String folder in Directory.GetDirectories(BackupPath, Consts.FileSystem.WatcherFilter, SearchOption.TopDirectoryOnly))
                {
                    DateTime current = Directory.GetLastWriteTime(folder).Date;
                    if (current > oldest)
                    {
                        oldest = current;
                    }
                }
                return oldest;
            }
            catch (Exception ex)
            {
                Log.Write(String.Format("Error getting backup list for pair [{0}]<=>[{1}], backup folder: {2}", LocalPath, RemotePath, BackupPath), ex);
                return DateTime.MaxValue;
            }
        }

        /// <summary>
        /// Create a backup of remote folder to backup directory, return false in case of upload/download failures (restartable) 
        /// throws exception in case of general fault
        /// </summary>
        /// <returns>true if operation finished without faults</returns>
        private bool Backup()
        {
            Guid backupGuid = Guid.NewGuid();
            String backupFullPath = Path.Combine(BackupPath, String.Format(Consts.Backup.SubfolderIncompleteFormat, DateTime.Now));
            String backupCompletePath = Path.Combine(BackupPath, String.Format(Consts.Backup.SubfolderCompleteFormat, DateTime.Now));
            String metafileFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Consts.Application.ProfileFolder);
            String backupMetadataFile = String.Format(Consts.Application.MetadataFileFormat, backupGuid.ToString());

            try
            {
                if (State != SyncPairState.Ready)
                {
                    // need to init pair first
                    Init();

                    if (State != SyncPairState.Ready) return false;
                }

                State = SyncPairState.BackingUp;


                // set sync framework environment
                FileSyncScopeFilter filterBackup = new FileSyncScopeFilter();

                if (!Directory.Exists(backupFullPath))
                {
                    Directory.CreateDirectory(backupFullPath);
                }

                if (!Directory.Exists(metafileFolder))
                {
                    Directory.CreateDirectory(metafileFolder);
                }

                FileSyncProvider backupProvider = new FileSyncProvider(backupGuid, backupFullPath, filterBackup,
                    FileSyncOptions.RecycleConflictLoserFiles | FileSyncOptions.RecycleDeletedFiles,
                    metafileFolder, backupMetadataFile, backupFullPath, null);

                backupProvider.Configuration.ConflictResolutionPolicy = ConflictResolutionPolicy.SourceWins;

                SyncOrchestrator agent = new SyncOrchestrator();
                agent.LocalProvider = remoteProvider;
                agent.RemoteProvider = backupProvider;
                agent.Direction = SyncDirectionOrder.DownloadAndUpload;

                SyncOperationStatistics stats = agent.Synchronize();

                // removing metadata file, since it won't be used anymore
                File.Delete(Path.Combine(metafileFolder, backupMetadataFile));

                if ((stats.DownloadChangesFailed + stats.UploadChangesFailed) > 0)
                {
                    // this could be fixed by subsequent backup retry, leaving folder as is
                    Log.Write(String.Format("Problems during backup operation of pair [{0}]<=>[{1}], backup folder [{2}]: {3} download errors, {4} upload errors. Pair processing will continue.",
                        LocalPath, RemotePath, backupFullPath, stats.DownloadChangesFailed, stats.UploadChangesFailed));
                }

                Directory.Move(backupFullPath, backupCompletePath);

                State = SyncPairState.Ready;
                return (stats.DownloadChangesFailed + stats.UploadChangesFailed) == 0;
            }
            catch (Exception ex)
            {
                // exceptions usually mean that this backup operation failed, subsequent restart probably will not help
                Log.Write(String.Format("Error during backup operation or pair [{0}]<=>[{1}], backup folder [{2}], backup parts will be removed", LocalPath, RemotePath, backupFullPath), ex);

                // trying to remove inconsistent parts
                try
                {
                    Directory.Delete(backupFullPath, true);
                }
                catch (Exception) { }

                try
                {
                    File.Delete(Path.Combine(metafileFolder, backupMetadataFile));
                }
                catch (Exception) { }
                throw ex;
            }
        }

        #endregion

        #region Worker thread code

        /// <summary>
        /// Synchronization thread code
        /// </summary>
        private void WorkerProcess()
        {
            DateTime lastSync = DateTime.Now;
            DateTime? lastBackup = null;
            bool onStartCompleted = false;
            try
            {
                // if no normal operations in strategy, just exit thread
                while (IsEnabled)
                {
                    if (_skipWait)
                    {
                        _skipWait = false;
                    }
                    else
                    {
                        lock (waitWorker)
                        {
                            Monitor.Wait(waitWorker);
                        }
                    }

                    if (_isPaused)
                    {
                        if (State != SyncPairState.Paused)
                        {
                            State = SyncPairState.Paused;
                            if (Strategy.AutoSync)
                            {
                                if (localFSWatcher != null)
                                {
                                    localFSWatcher.EnableRaisingEvents = false;
                                }
                                if (remoteFSWatcher != null)
                                {
                                    remoteFSWatcher.EnableRaisingEvents = false;
                                }
                            }
                        }
                        continue;
                    }
                    else
                    {
                        if (State == SyncPairState.Paused)
                        {
                            if (Strategy.AutoSync)
                            {
                                FSChangesCounter = 1;
                            }
                        }
                    }

                    // try to init pair
                    if (State != SyncPairState.Ready)
                    {
                        Init();
                    }

                    // on start - on success never do again
                    if ((Strategy.OnStart) && (!onStartCompleted))
                    {
                        if (Sync() == false)
                        {
                            onStartCompleted = true;
                        }
                    }

                    // auto sync
                    if (Strategy.AutoSync)
                    {
                        if (FSChangesCounter > 0)
                        {
                            // if synchronization had some changes doing it again to ensure
                            // that all changes were commited (even those that were made during
                            // synchronization process)

                            switch (Sync())
                            {
                                case true:
                                    FSChangesCounter = 1;
                                    continue;

                                case false:
                                    FSChangesCounter = 0;
                                    break;
                            }
                        }
                    }

                    // periodic sync
                    if (Strategy.Periodic)
                    {
                        if (lastSync.AddHours(Strategy.PeriodHours) > DateTime.Now)
                        {
                            switch (Sync())
                            {
                                case true:
                                    continue;

                                case false:
                                    lastSync = DateTime.Now;
                                    break;
                            }
                        }

                    }

                    // check backups
                    if ((BackupMode.KeepArchives) && ((!Parameters.BackupAtNight) || 
                        ((DateTime.Now.Hour >= Consts.Backup.NightTimeStartHour) && (DateTime.Now.Hour < Consts.Backup.NightTimeEndHour))))
                    {
                        // TODO: remove!!!
                        purgeOldArchives();
                        // archiving is enabled
                        if (!lastBackup.HasValue)
                        {
                            lastBackup = latestBackupDate();
                        }

                        if (lastBackup.Value.AddDays(BackupMode.CreateArchiveDays) < DateTime.Now)
                        {
                            if (Parameters.DebugLog)
                            {
                                Log.Write(String.Format("Pair [{0}]<=>[{1}], time to create new backup", LocalPath, RemotePath));
                            }

                            // time to make new archive
                            if (Backup())
                            {
                                // backup operation is complete
                                lastBackup = DateTime.Now;
                                purgeOldArchives();
                            }
                        }
                    }


                }
                State = SyncPairState.Completed;
            }
            catch (Exception ex)
            {
                Log.Write(String.Format("Error during synchronization of [{0}]<=>[{1}]. Pair is disabled, processing will NOT continue.", LocalPath, RemotePath), ex);
                IsEnabled = false;
                State = SyncPairState.Failure;
            }
        }

        #endregion
        
        #region Ticker thread

        private void TickerProcess()
        {
            while (IsEnabled)
            {
                lock (waitTicker)
                {
                    Monitor.Wait(waitTicker, Consts.Threading.TickInterval);
                }
                lock (waitWorker)
                {
                    if (Parameters.DebugLog)
                    {
                        Log.Write(String.Format("Pair [{0}]<=>[{1}], tick - signaling worker thread to wake up", LocalPath, RemotePath));
                    }
                    Monitor.Pulse(waitWorker);
                }
            }
        }

        #endregion

        #region Thread control

        /// <summary>
        /// Singal to stop processing and wait thread to finish
        /// </summary>
        private void Stop()
        {
            // stop file system watchers
            if (localFSWatcher != null)
            {
                localFSWatcher.EnableRaisingEvents = false;
                localFSWatcher.Changed -= FSEventHandler;
                localFSWatcher.Deleted -= FSEventHandler;
                localFSWatcher.Created -= FSEventHandler;
                localFSWatcher.Renamed -= FSRenamedHander;
                localFSWatcher.Error -= FSErrorHandler;
                localFSWatcher = null;
            }

            if (remoteFSWatcher != null)
            {
                remoteFSWatcher.EnableRaisingEvents = false;
                remoteFSWatcher.Changed -= FSEventHandler;
                remoteFSWatcher.Deleted -= FSEventHandler;
                remoteFSWatcher.Created -= FSEventHandler;
                remoteFSWatcher.Renamed -= FSRenamedHander;
                remoteFSWatcher.Error -= FSErrorHandler;
                remoteFSWatcher = null;
            }

            // signal to stop processing and thread to finish
            lock (waitTicker)
            {
                Monitor.Pulse(waitTicker);
            }
            lock (waitWorker)
            {
                Monitor.Pulse(waitWorker);
            }

            if (workerThread != null)
            {
                workerThread.Join();
                workerThread = null;
            }

            if (tickerThread != null)
            {
                tickerThread.Join();
                tickerThread = null;
            }
        }

        /// <summary>
        /// Start worker thread
        /// </summary>
        private void Start()
        {
            if (workerThread == null)
            {
                workerThread = new Thread(WorkerProcess);
                workerThread.Start();
            }

            if (tickerThread == null)
            {
                tickerThread = new Thread(TickerProcess);
                tickerThread.Start();
            }
        }

        #endregion

        #region Notify property changed

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void OnPropertyChanged(string info)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion

        #region Utitities

        // TODO: implement statistic reporting

        /// <summary>
        /// Remove metadata files
        /// </summary>
        public void ClearMetadata()
        {
            if (!IsEnabled)
            {
                try
                {
                    // cannot delete metadata while worker thread is active - could be dangerous

                    File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        Consts.Application.ProfileFolder, String.Format(Consts.Application.MetadataFileFormat, LocalGUID.ToString())));

                    File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        Consts.Application.ProfileFolder, String.Format(Consts.Application.MetadataFileFormat, RemoteGUID.ToString())));
                }
                catch (Exception ex)
                {
                    Log.Write(String.Format("Error deleting metadata files for pair [{0}]<=>[{1}]", LocalPath, RemotePath), ex);
                }
            }
        }

        #endregion

    }
}
