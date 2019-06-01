using System;
using System.Collections.Generic;
using System.Text;

namespace RedisSlimClient.Io.Commands
{
    class DeleteCommand : StringCommand
    {
        public DeleteCommand(string key) : base("DEL", key)
        {
        }
    }
}