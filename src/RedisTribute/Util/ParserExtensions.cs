using System.Linq;
using System.Text;

namespace RedisTribute.Util
{
    static class ParserExtensions
    {
        public static string ToDecimalText(int encoding)
        {
            var buf = new StringBuilder();
            string next = null;

            foreach(var i in encoding.ToString().Skip(1))
            {
                next += i;

                if (next.Length == 2)
                {
                    var x = int.Parse(next) + 64;
                    buf.Append((char)(byte)x);
                    next = null;
                }
            }

            return buf.ToString();
        }
    }
}
