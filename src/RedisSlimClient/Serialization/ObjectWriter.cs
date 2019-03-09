using System.Text;
using RedisSlimClient.Types;

namespace RedisSlimClient.Serialization
{
    class ObjectWriter : IObjectWriter
    {
        private readonly RedisArray _objectData;

        public ObjectWriter()
        {
            _objectData = new RedisArray(1);
        }

        public void WriteItem(string name, int level, object data)
        {
            _objectData.Items.Add(new RedisString(Encoding.UTF8.GetBytes(name)));
        }

        public RedisArray ToArray() => _objectData;
    }
}
