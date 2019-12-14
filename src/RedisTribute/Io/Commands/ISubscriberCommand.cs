﻿using RedisTribute.Types;
using System;
using System.Threading.Tasks;

namespace RedisTribute.Io.Commands
{
    interface ISubscriberCommand
    {
        bool HasFinished { get; }

        bool CanReceive(IRedisObject message);

        Task ReceiveAsync(IRedisObject message);

        void Abandon(Exception ex);
    }
}
