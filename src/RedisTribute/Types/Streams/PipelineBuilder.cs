using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Configuration;
using RedisTribute.Types.Pipelines;

namespace RedisTribute.Types.Streams
{
    public interface IPipelineBuilder
    {
        IPipelineBuilder AddObserver<TInput, TOutput>(Action<TransformationResult<TInput, TOutput>> observer);
        IPipelineBuilder AddTransformation<TInput, TOutput>(Func<TInput, TOutput> transformation);
        IPipelineBuilder AddSink<TInput, TOutput>(Func<TInput, Task> sink);
        Task RunAsync(CancellationToken cancellation = default);
    }

    class PipelineBuilder : IPipelineBuilder
    {
        readonly IRedisStreamClient _streamClient;
        readonly PipelineOptions _options;
        readonly IList<Func<CancellationToken, Task>> _executables;
        readonly IDictionary<string, IList<Action<object>>> _observers;

        public PipelineBuilder(IRedisStreamClient streamClient, PipelineOptions options)
        {
            _streamClient = streamClient;
            _options = options;
            _executables = new List<Func<CancellationToken, Task>>();
            _observers = new Dictionary<string, IList<Action<object>>>();
        }

        public IPipelineBuilder AddObserver<TInput, TOutput>(Action<TransformationResult<TInput, TOutput>> observer)
        {
            var sig = Sig<TInput, TOutput>();

            if (!_observers.TryGetValue(sig, out var items))
            {
                _observers[sig] = items = new List<Action<object>>();
            }

            items.Add(x => { observer((TransformationResult<TInput, TOutput>) x); });

            return this;
        }

        public IPipelineBuilder AddTransformation<TInput, TOutput>(Func<TInput, TOutput> transformation)
        {
            return AddComponent(transformation, true);
        }

        public IPipelineBuilder AddSink<TInput, TOutput>(Func<TInput, Task> sink)
        {
            return AddComponent(sink, false);
        }

        public Task RunAsync(CancellationToken cancellation = default)
        {
            return Task.WhenAll(_executables.Select(x => x(cancellation)));
        }

        PipelineBuilder AddComponent<TInput, TOutput>(Func<TInput, TOutput> component, bool forward)
        {
            var sig = Sig<TInput, TOutput>();

            async Task CreatePipeline(CancellationToken cancellation)
            {
                IRedisStream<TOutput> fwdStream = null;

                var inputKey = KeySpace.Default.GetStreamKey($"{_options.Namespace}/{typeof(TInput).Name}");
                var inputStream = await _streamClient.GetStream<TInput>(inputKey);

                if (forward)
                {
                    var outputKey = KeySpace.Default.GetStreamKey($"{_options.Namespace}/{typeof(TOutput).Name}");
                    fwdStream = await _streamClient.GetStream<TOutput>(outputKey);
                }

                await inputStream.ReadAsync(async kv =>
                {
                    Exception err = null;
                    TOutput tout = default;

                    try
                    {
                        tout = component(kv.Value);

                        if (fwdStream != null)
                        {
                            await fwdStream.WriteAsync(tout, cancellation);
                        }

                        if (tout is Task t)
                        {
                            await t;
                        }
                    }
                    catch (Exception ex)
                    {
                        err = ex;
                    }


                    if (_observers.TryGetValue(sig, out var observers))
                    {
                        foreach (var obs in observers)
                        {
                            obs.Invoke(new TransformationResult<TInput, TOutput>(kv.Key, kv.Value, tout, err));
                        }
                    }

                }, _options.StartFrom, cancellation: cancellation);
            }

            _executables.Add(CreatePipeline);

            return this;
        }

        string Sig<TInput, TOutput>() => $"{typeof(TInput).FullName}/{typeof(TOutput).FullName}";
    }
}
