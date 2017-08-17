using System.IO;

namespace Geeks.Profiler
{
    internal class Cloner
    {
        public void Clone(string sourceDirName, string destDirName)
        {
            var dir = new DirectoryInfo(sourceDirName);

            var dirs = dir.GetDirectories();
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, overwrite: true);
            }

            foreach (var subdir in dirs)
            {
                var temppath = Path.Combine(destDirName, subdir.Name);
                Clone(subdir.FullName, temppath);
            }
        }
    }
}
