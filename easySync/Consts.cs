using System;

namespace easySync
{
    public abstract class Consts
    {
        public abstract class Application
        {
            public const String Version = "0.1";
            public const String VersionCodeName = "...";
            public const String UpdateCheckURL = "http://sites.google.com/site/dtazetdinov/easysync/current-release.txt";
            public const String SiteURL = "http://easysync-backup.googlecode.com";
            public const String TrackerURL = "http://code.google.com/p/easysync-backup/issues/list";
            public const String ProfileFolder = "easySync";
            public const String LogFileName = "easySync.log";
            public const String MetadataFileFormat = "easySync-{0}.metadata";
            public const String VersionInfoFormat = "{0} ({1})";
            public const String BuildInfoFormat = "{0} (built on {1:d}, {2})";
        }

        public abstract class Backup
        {
            public const int NightTimeStartHour = 1;
            public const int NightTimeEndHour = 6;
            public const String SubfolderIncompleteFormat = "backup-{0:yyyy-MM-dd}-incomplete";
            public const String SubfolderCompleteFormat = "backup-{0:yyyy-MM-dd}";
        }

        public abstract class FileSystem
        {
            public const String WatcherFilter = "*.*";
        }

        public abstract class Threading
        {
            public const int TickInterval = 300000; // 5 mins
            public const int InitInterval = 1000;
        }

        public abstract class Log
        {
            public const String ExceptionFormat = "{0}: {1} (thrown by {2}.{3})";
            public const String MethodFormat = "{0}.{1}()";
            public const String MethodGotExceptionFormat = "{0}.{1}() got {2}";
            public const String LogMessageFormat = "[{0:s}] {1}";
            public static readonly String ExceptionFullFormat = "{0}: {1} (thrown by {2}.{3})" + Environment.NewLine + "{4}";
        }

        public abstract class Updater
        {
            public const int AutoUpdateCheckDuration = 7;
            public static readonly String[] FieldDivider = { "|" };
            public static readonly String[] WordDividers = { " ", ",", ".", ";" };
            public const String URLPathDivider = "/";
            public const String ErrorFormat = "UpdateHelper.UpdateDownloaded: URL=[{0}], LocalFile=[{1}]";
            public const String ExceptionPrefix = "UpdateHelper.UpdateDownloaded: AsyncCompletedEventArgs object contents:";
        }
    }
}
