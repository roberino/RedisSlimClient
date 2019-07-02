using System.Text;
using RedisSlimClient.Serialization;

namespace RedisSlimClient.Configuration
{
    public interface ISerializerSettings
    {
        Encoding Encoding { get; set; }
        IObjectSerializerFactory SerializerFactory { get; set; }
    }
}