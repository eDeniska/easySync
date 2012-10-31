using System;
using System.Collections.ObjectModel;

namespace easySync
{
    public static class SyncMonitoring
    {
        public const String STRATEGY_ONSTART = "ONSTART";
        public const String STRATEGY_HOURS6 = "HOURS6";
        public const String STRATEGY_HOURS3 = "HOURS3";
        public const String STRATEGY_HOURS1 = "HOURS1";
        public const String STRATEGY_IMMEDIATE = "IMMEDIATE";

        private static ObservableCollection<SyncMonitoringStrategy> list = new ObservableCollection<SyncMonitoringStrategy>();

        public static ObservableCollection<SyncMonitoringStrategy> Strategies
        {
            get { return list; }
        }

        static SyncMonitoring()
        {
            list.Add(new SyncMonitoringStrategy(STRATEGY_ONSTART, Properties.Resources.SyncStrategyOnStart, false, false, 0, true));
            list.Add(new SyncMonitoringStrategy(STRATEGY_HOURS6, Properties.Resources.SyncStrategyHours6, false, true, 6, false));
            list.Add(new SyncMonitoringStrategy(STRATEGY_HOURS3, Properties.Resources.SyncStrategyHours3, false, true, 3, false));
            list.Add(new SyncMonitoringStrategy(STRATEGY_HOURS1, Properties.Resources.SyncStrategyHours1, false, true, 1, false));
            list.Add(new SyncMonitoringStrategy(STRATEGY_IMMEDIATE, Properties.Resources.SyncStrategyImmediately, true, false, 0, false));
        }
    }

    public class SyncMonitoringStrategy
    {
        public String ID { get; private set; }
        public String Title { get; private set; }
        public bool AutoSync { get; private set; }
        public bool Periodic { get; private set; }
        public int PeriodHours { get; private set; }
        public bool OnStart { get; private set; }

        public SyncMonitoringStrategy(String id, String title, bool autoSync, bool periodic, int periodHours, bool onStart)
        {
            ID = id;
            Title = title;
            AutoSync = autoSync;
            Periodic = periodic;
            PeriodHours = periodHours;
            OnStart = onStart;
        }

        public override string ToString()
        {
            return Title;
        }
    }

}
