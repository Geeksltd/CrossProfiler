using Geeks.ProfilerAPI.Models;

namespace Geeks.ProfilerAPI.Managers
{
    internal static class CommandManager
    {
        private static Command _command = Command.Start;

        public static void Set(Command command)
        {
            _command = command;
        }

        public static Command Get()
        {
            return _command;
        }
    }
}