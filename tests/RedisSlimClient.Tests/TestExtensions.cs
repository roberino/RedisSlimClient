using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.UnitTests
{
    static class TestExtensions
    {
        public static void RunOnBackgroundThread(this Func<Task> work)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                work().Wait();
            });
        }

        public static TextReader OpenStringResource(this string fileName)
        {
            return new StreamReader(OpenBinaryResource(fileName));
        }

        public static byte[] OpenBinaryResourceBytes(this string fileName)
        {
            using (var ms = new MemoryStream())
            using (var resource = OpenBinaryResource(fileName))
            {
                resource.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public static Stream OpenBinaryResource(this string fileName)
        {
            var asm = typeof(TestExtensions).Assembly;
            var resourceName = asm.GetManifestResourceNames().First(x => x.EndsWith(fileName));
            var resource = asm.GetManifestResourceStream(resourceName);
            return resource;
        }
    }
}