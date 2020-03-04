using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RedisTribute.IntegrationTests.Data
{
    public static class TextSample
    {
        public static TextReader OpenResource(string file = "TextSample1.txt")
        {
            var asm = typeof(TextSample).Assembly;
            var name = asm.GetManifestResourceNames().First(n => n.EndsWith(file));
            var resource = asm.GetManifestResourceStream(name);
            var reader = new StreamReader(resource);

            return reader;
        }

        public static IEnumerable<string> Words(string file = "TextSample1.txt")
        {
            using(var reader = OpenResource(file))
            {
                var buffer = new StringBuilder();
                var skipNext = false;

                while (true)
                {
                    var line = reader.ReadLine();

                    if (line == null)
                    {
                        yield break;
                    }

                    foreach(var c in line)
                    {
                        if (!skipNext && (char.IsLetterOrDigit(c) || c == '\''))
                        {
                            buffer.Append(c);
                        }
                        else
                        {
                            if (char.IsWhiteSpace(c) || c == ',' || c == '.')
                            {
                                if (buffer.Length > 0)
                                {
                                    if (!skipNext)
                                    {
                                        var next = buffer.ToString().ToLowerInvariant();

                                        if (buffer[buffer.Length - 1] == '.')
                                        {
                                            yield return next.Substring(0, next.Length - 1);
                                        }
                                        else
                                        {
                                            yield return next;
                                        }

                                        if (c == '.')
                                            yield return ".";
                                    }

                                    buffer.Clear();
                                }
                                skipNext = false;
                            }
                            else
                            {
                                skipNext = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
