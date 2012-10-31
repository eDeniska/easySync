using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace easySync
{
    public static class Parameters
    {
        private const String sMainWindowTop = "MainWindowTop";
        private const String sMainWindowLeft = "MainWindowLeft";
        private const String sMainWindowHeight = "MainWindowHeight";
        private const String sMainWindowWidth = "MainWindowWidth";
        private const String sMainWindowMaximized = "MainWindowMaximized";
        private const String sIntroductionShown = "IntroductionShown";
        private const String sDebugLog = "DebugLog";

        private const String sBranch = @"SOFTWARE\easySync";
        private const String sPairsBranch = @"SOFTWARE\easySync\Pairs";
        private const String sLanguage = "Language";
        private const String sLastUpdateDate = "LastUpdateDate";
        private const String sCheckForUpdates = "CheckForUpdates";
        private const String sBackupAtNight = "BackupAtNight";
        
        public static double MainWindowTop { get; set; }
        public static double MainWindowLeft { get; set; }
        public static double MainWindowHeight { get; set; }
        public static double MainWindowWidth { get; set; }
        public static bool MainWindowMaximized { get; set; }
        public static bool CheckForUpdates { get; set; }
        public static bool IntroductionShown { get; set; }
        public static bool DebugLog { get; set; }
        public static bool BackupAtNight { get; set; }
        public static String Language { get; set; }
        public static DateTime LastUpdateDate { get; set; }

        public static void Reset()
        {
            MainWindowTop = -1;
            MainWindowLeft = -1;
            MainWindowHeight = -1;
            MainWindowWidth = -1;
            MainWindowMaximized = false;
            CheckForUpdates = true;
            IntroductionShown = false;
            DebugLog = false;
            BackupAtNight = false;
            Language = String.Empty;
            LastUpdateDate = DateTime.MinValue;
        }

        static Parameters()
        {
            try
            {
                Reset();

                RegistryKey branch = Registry.CurrentUser.OpenSubKey(sBranch);
                if (branch != null)
                {
                    MainWindowTop = Convert.ToDouble(branch.GetValue(sMainWindowTop, -1));
                    MainWindowLeft = Convert.ToDouble(branch.GetValue(sMainWindowLeft, -1));
                    MainWindowHeight = Convert.ToDouble(branch.GetValue(sMainWindowHeight, -1));
                    MainWindowWidth = Convert.ToDouble(branch.GetValue(sMainWindowWidth, -1));
                    MainWindowMaximized = Boolean.Parse(branch.GetValue(sMainWindowMaximized, Boolean.FalseString).ToString());
                    CheckForUpdates = Boolean.Parse(branch.GetValue(sCheckForUpdates, Boolean.TrueString).ToString());
                    IntroductionShown = Boolean.Parse(branch.GetValue(sIntroductionShown, Boolean.FalseString).ToString());
                    DebugLog = Boolean.Parse(branch.GetValue(sDebugLog, Boolean.FalseString).ToString());
                    BackupAtNight = Boolean.Parse(branch.GetValue(sBackupAtNight, Boolean.FalseString).ToString());
                    Language = branch.GetValue(sLanguage, String.Empty).ToString();
                    LastUpdateDate = DateTime.Parse(branch.GetValue(sLastUpdateDate, DateTime.MinValue.ToString()).ToString());
                }
            }
            catch (Exception e)
            {
                Log.Write(e);
            }
        }

        public static ObservableCollection<SyncPair> LoadPairs()
        {
            object listLock = new object();
            try
            {
                RegistryKey pairsBranch = Registry.CurrentUser.OpenSubKey(sPairsBranch, true);

                ObservableCollection<SyncPair> pairs = new ObservableCollection<SyncPair>();

                if (pairsBranch != null)
                {
                    Parallel.ForEach(pairsBranch.GetValueNames(), key =>
                    {
                        try
                        {
                            SyncPair pair = new SyncPair(pairsBranch.GetValue(key, String.Empty).ToString());
                            lock (listLock) { pairs.Add(pair); }
                        }
                        catch (Exception ex)
                        {
                            Log.Write(ex);
                        }
                    });
                }

                return pairs;
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                return null;
            }
        }

        public static void SavePairs(IEnumerable<SyncPair> pairs)
        {
            try
            {
                RegistryKey pairsBranch = Registry.CurrentUser.OpenSubKey(sPairsBranch, true);
                if (pairsBranch == null)
                {
                    pairsBranch = Registry.CurrentUser.CreateSubKey(sPairsBranch);
                }

                if (pairsBranch != null)
                {
                    // remove all existing pairs
                    foreach (String key in pairsBranch.GetValueNames())
                    {
                        pairsBranch.DeleteValue(key);
                    }

                    foreach (SyncPair pair in pairs)
                    {
                        pairsBranch.SetValue(pair.LocalGUID.ToString(), pair.SaveAsXML(), RegistryValueKind.String);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }

        public static void Save()
        {
            try
            {
                RegistryKey branch = Registry.CurrentUser.OpenSubKey(sBranch, true);
                if (branch == null)
                {
                    branch = Registry.CurrentUser.CreateSubKey(sBranch);
                }

                if (branch != null)
                {
                    branch.SetValue(sMainWindowTop, Convert.ToInt64(MainWindowTop), RegistryValueKind.QWord);
                    branch.SetValue(sMainWindowLeft, Convert.ToInt64(MainWindowLeft), RegistryValueKind.QWord);
                    branch.SetValue(sMainWindowHeight, Convert.ToInt64(MainWindowHeight), RegistryValueKind.QWord);
                    branch.SetValue(sMainWindowWidth, Convert.ToInt64(MainWindowWidth), RegistryValueKind.QWord);
                    branch.SetValue(sMainWindowMaximized, MainWindowMaximized.ToString(), RegistryValueKind.String);
                    branch.SetValue(sCheckForUpdates, CheckForUpdates.ToString(), RegistryValueKind.String);
                    branch.SetValue(sIntroductionShown, IntroductionShown.ToString(), RegistryValueKind.String);
                    branch.SetValue(sLastUpdateDate, LastUpdateDate.ToString(), RegistryValueKind.String);
                    // branch.SetValue(sDebugLog, DebugLog.ToString(), RegistryValueKind.String);
                    branch.SetValue(sBackupAtNight, BackupAtNight.ToString(), RegistryValueKind.String);
                    branch.SetValue(sLanguage, Language, RegistryValueKind.String);
                }

            }
            catch (Exception e)
            {
                Log.Write(e);
            }
        }
    }
}
