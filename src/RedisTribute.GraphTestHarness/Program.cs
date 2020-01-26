using LinqInfer.Text;
using LinqInfer.Text.Http;
using LinqInfer.Text.Analysis;
using LinqInfer.Data.Pipes;
using System;
using System.Threading;

namespace RedisTribute.GraphTestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            var dict = new EnglishDictionary();
            var pipe = new Uri("").CreateSource();

            using (pipe)
            {

            }
        }
    }
}
