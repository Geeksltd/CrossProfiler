using System.Collections.Generic;
using System.Text.RegularExpressions;
using Geeks.ProfilerAPI.Models;

namespace Geeks.ProfilerAPI.Managers
{
    internal static class ReportManager
    {
        private static ICollection<Report> _reports = new List<Report>();

        public static void Save(string report)
        {
            var reports = new List<Report>();

            var reportLines = Regex.Split(report, "\r\n|\r|\n");

            foreach (var reportLine in reportLines)
            {
                if (string.IsNullOrEmpty(reportLine))
                    continue;

                var parts = reportLine.Split(',');
                var key = parts[0].Replace("\"", "");
                var count = int.Parse(parts[1].Replace("\"", ""));
                var elapsedTicks = long.Parse(parts[2].Replace("\"", ""));

                var currentReport = new Report(key, count, elapsedTicks);
                reports.Add(currentReport);
            }

            _reports = reports;
        }

        public static ICollection<Report> Get()
        {
            return _reports;
        }
    }
}