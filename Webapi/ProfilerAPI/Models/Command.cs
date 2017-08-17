namespace Geeks.ProfilerAPI.Models
{
    public sealed class Command
    {
        public static readonly Command Discard = new Command("Discard");
        public static readonly Command Start = new Command("Start");
        public static readonly Command GetResults = new Command("GetResults");
        private readonly string _command;

        public Command(string command)
        {
            _command = command;
        }

        public override string ToString()
        {
            return _command;
        }

        public override bool Equals(object obj)
        {
            var command = obj as Command;
            if (command != null)
            {
                return Equals(command);
            }

            return base.Equals(obj);
        }

        public bool Equals(Command command)
        {
            return command.ToString().Equals(_command);
        }
    }
}