using System;
using System.Collections.Generic;
using System.Linq;

namespace RedisSlimClient.Io.Pipelines
{
    class ErrorBroadcaster
    {
        readonly IList<Exception> _errors = new List<Exception>();

        public event Action<Exception> Error;

        public void RegisterError(Exception exception)
        {
            _errors.Add(exception);
        }

        public void Broadcast()
        {
            var errors = _errors.ToArray();

            _errors.Clear();

            if (errors.Any())
            {
                if (errors.Length == 1)
                {
                    Error?.Invoke(errors[0]);
                    return;
                }

                Error?.Invoke(new AggregateException(errors));
            }
        }
    }
}