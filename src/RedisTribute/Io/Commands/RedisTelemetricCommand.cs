using RedisTribute.Types;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RedisTribute.Io.Commands
{
    class RedisTelemetricCommand : ICommandIdentity
    {
        Action<CommandState> _stateChanged;
        Stopwatch _sw;

        protected RedisTelemetricCommand(string commandText, bool requireMaster, RedisKey key = default)
        {
            RequireMaster = requireMaster;
            CommandText = commandText;
            Key = key;
        }

        protected void BeginTimer()
        {
            if (_sw == null)
            {
                _sw = new Stopwatch();
                _sw.Start();
            }
        }

        public Func<object[], Task> OnExecute { get; set; }

        public Action<CommandState> OnStateChanged
        {
            set
            {
                _stateChanged = value;
                BeginTimer();
            }
        }

        public TimeSpan Elapsed => _sw?.Elapsed ?? TimeSpan.Zero;

        public Uri AssignedEndpoint { get; set; } = EndpointConstants.UnassignedEndpoint;

        public virtual RedisKey Key { get; }

        public string CommandText { get; }

        public int AttemptSequence { get; set; }

        public bool RequireMaster { get; }

        protected void FireStateChange(CommandStatus status)
        {
            _stateChanged?.Invoke(new CommandState(Elapsed, status, this));
        }
    }
}
