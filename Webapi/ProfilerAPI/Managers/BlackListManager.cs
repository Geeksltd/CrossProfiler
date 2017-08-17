using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Geeks.ProfilerAPI.Managers
{
    internal static class BlackListManager
    {
        private const string FileName = "RemovedKeys.txt";

        internal static ICollection<string> GetAll(HttpServerUtilityBase server)
        {
            if (File.Exists(GetFilePath(server)))
            {
                return File.ReadAllLines(GetFilePath(server));
            }
            return new string[] { };
        }

        internal static void Add(HttpServerUtilityBase server, string key)
        {
            File.AppendAllLines(GetFilePath(server), new[] { key });
        }

        internal static void Reset(HttpServerUtilityBase server)
        {
            File.WriteAllLines(GetFilePath(server), Enumerable.Empty<string>());
        }

        private static string GetFilePath(HttpServerUtilityBase server)
        {
            return server.MapPath("~/" + FileName);
        }
    }
}