using System;
using System.Collections.ObjectModel;

namespace easySync
{
    public static class SyncBackup
    {
        public const String BACKUP_NOBACKUPS = "NOBACKUPS";
        public const String BACKUP_KEEPWEEK = "KEEPWEEK";
        public const String BACKUP_KEEPMONTH = "KEEPMONTH";
        public const String BACKUP_KEEPWEEKLY = "KEEPWEEKLY";
        public const String BACKUP_KEEPYEAR = "KEEPYEAR";
        public const String BACKUP_KEEPMONTHLY = "KEEPMONTHLY";

        private static ObservableCollection<SyncBackupMode> list = new ObservableCollection<SyncBackupMode>();

        public static ObservableCollection<SyncBackupMode> BackupModes
        {
            get { return list; }
        }

        static SyncBackup()
        {
            list.Add(new SyncBackupMode(BACKUP_NOBACKUPS, Properties.Resources.SyncBackupNoBackups, false, -1, -1, -1));
            list.Add(new SyncBackupMode(BACKUP_KEEPWEEK, Properties.Resources.SyncBackupKeepWeek, true, 7, -1, 1));
            list.Add(new SyncBackupMode(BACKUP_KEEPMONTH, Properties.Resources.SyncBackupKeepMonth, true, 31, -1, 1));
            list.Add(new SyncBackupMode(BACKUP_KEEPWEEKLY, Properties.Resources.SyncBackupKeepWeekly, true, 31, -1, 7));
            list.Add(new SyncBackupMode(BACKUP_KEEPYEAR, Properties.Resources.SyncBackupKeepYear, true, 365, -1, 7));
            list.Add(new SyncBackupMode(BACKUP_KEEPMONTHLY, Properties.Resources.SyncBackupKeepMonthly, true, 365, -1, 31));
        }
    }

    public class SyncBackupMode
    {
        public String ID { get; private set; }
        public String Title { get; private set; }
        public bool KeepArchives { get; private set; }
        public int KeepDays { get; private set; }
        public int KeepNumber { get; private set; }
        public int CreateArchiveDays { get; private set; }

        public SyncBackupMode(String id, String title, bool keepArchives, int keepDays, int keepNumber, int createArchiveDays)
        {
            ID = id;
            Title = title;
            KeepArchives = keepArchives;
            KeepDays = keepDays;
            KeepNumber = keepNumber;
            CreateArchiveDays = createArchiveDays;
        }

        public override string ToString()
        {
            return Title;
        }
    }
}
