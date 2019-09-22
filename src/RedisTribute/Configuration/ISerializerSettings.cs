using System.Text;
using RedisTribute.Serialization;

namespace RedisTribute.Configuration
{
    public interface ISerializerSettings
    {
        Encoding Encoding { get; set; }
        IObjectSerializerFactory SerializerFactory { get; set; }
    }
}