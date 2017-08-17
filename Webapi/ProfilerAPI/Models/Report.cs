namespace Geeks.ProfilerAPI.Models
{
    public class Report
    {
        public string Key
        {
            get;
            private set;
        }

        public int Count
        {
            get;
            private set;
        }

        public long ElapsedTicks
        {
            get;
            private set;
        }

        public long ElapsedMilliseconds => ElapsedTicks / 10000;

        public Report(string key, int count, long elapsedTicks)
        {
            Key = key;
            Count = count;
            ElapsedTicks = elapsedTicks;
        }
    }
}