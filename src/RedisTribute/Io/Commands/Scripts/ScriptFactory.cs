using System;
using System.IO;
using System.Linq;
using System.Text;

namespace RedisTribute.Io.Commands.Scripts
{
    static class ScriptFactory
    {
        public static string ReleaseLock => GetScript(nameof(ReleaseLock));

        static string GetScript(string name)
        {
            var asm = typeof(ScriptFactory).Assembly;
            var resource = asm.GetManifestResourceNames().First(n => n.EndsWith($"{name}.lua"));

            using var stream = asm.GetManifestResourceStream(resource) ?? throw new ArgumentException(name);

            using var ms = new MemoryStream();

            stream.CopyTo(ms);
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
