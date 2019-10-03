using RedisTribute.Io.Server;
using RedisTribute.Io.Server.Transactions;
using RedisTribute.Telemetry;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace RedisTribute.Io
{
    class TransactionContainer : IDisposable
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly ITelemetryWriter _telemetry;
        private readonly Guid _id;

        public TransactionContainer(ICommandExecutor commandExecutor, ITelemetryWriter telemetry)
        {
            _commandExecutor = commandExecutor;
            _telemetry = telemetry;
            _id = Guid.NewGuid();
        }

        public async Task Transact(Func<ICommandExecutor, Task> work, CancellationToken cancellationToken)
        {
            var notify = new Notification();
            var tx = Transaction.Current;

            if (tx != null)
            {
                tx.EnlistDurable(_id, notify, EnlistmentOptions.None);
            }

            var beginTrans = new BeginTransactionCommand();

            if (await _commandExecutor.Execute(beginTrans, cancellationToken))
            {
                var commit = false;

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await work(_commandExecutor);
                    commit = !notify.RolledBackRequested;

                    if (commit)
                    {
                        await _commandExecutor.Execute(new ExecuteTransactionCommand(), cancellationToken);
                    }
                }
                finally
                {
                    if (!commit)
                    {
                        try
                        {
                            await _commandExecutor.Execute(new DiscardTransactionCommand());
                        }
                        catch
                        {
                        }
                    }

                    notify.Enlistment?.Done();
                }
            }
        }

        public void Dispose()
        {

        }

        class Notification : IEnlistmentNotification
        {
            readonly object _lock = new object();
            int _completedFlag;

            public bool RolledBackRequested { get; private set; }

            public bool CommitRequested { get; private set; }

            public Enlistment Enlistment { get; private set; }

            public void End()
            {
                if (Interlocked.Exchange(ref _completedFlag, 1) == 0)
                {
                    Enlistment?.Done();
                }
            }

            public void Commit(Enlistment enlistment)
            {
                if (Interlocked.Exchange(ref _completedFlag, 1) == 1)
                {
                    enlistment.Done();
                    return;
                }
                Enlistment = enlistment;
            }

            public void InDoubt(Enlistment enlistment)
            {
                Rollback(enlistment);
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
            }

            public void Rollback(Enlistment enlistment)
            {
                RolledBackRequested = true;

                if (Interlocked.Exchange(ref _completedFlag, 1) == 1)
                {
                    enlistment.Done();
                    return;
                }

                Enlistment = enlistment;
            }
        }
    }
}
