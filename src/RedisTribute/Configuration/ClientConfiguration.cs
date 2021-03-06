﻿using RedisTribute.Io.Scheduling;
using RedisTribute.Serialization;
using RedisTribute.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace RedisTribute.Configuration
{
    public class ClientConfiguration : IReadWriteBufferSettings, ISerializerSettings, IClientCredentials
    {
        static int _idCounter = 1;

        readonly TelemetryAggregateWriter _telemetryAggregateWriter;

        public ClientConfiguration(string connectionOptions, NetworkConfiguration networkConfiguration = null)
        {
            Id = Interlocked.Increment(ref _idCounter);
            SslConfiguration = new SslConfiguration();
            ClientName = $"RSC{Process.GetCurrentProcess().Id}-{Environment.MachineName}-{Id}";
            NetworkConfiguration = networkConfiguration ?? new NetworkConfiguration();

            var parser = CreateParser();

            parser.Parse(connectionOptions, this);

            PostParse(false);

            _telemetryAggregateWriter = new TelemetryAggregateWriter();
        }

        public static implicit operator ClientConfiguration(string configString) => new ClientConfiguration(configString);

        public int Id { get; }

        public int Database { get; private set; }

        public NetworkConfiguration NetworkConfiguration { get; }

        public string ClientName { get; private set; }


        readonly NonNullable<IPasswordManager> _passwords = new PasswordManager();
        public IPasswordManager PasswordManager { get => _passwords.Value; set => _passwords.Value = value; }

        readonly NonNullable<IWorkScheduler> _scheduler = ThreadPoolScheduler.Instance;
        public IWorkScheduler Scheduler { get => _scheduler.Value; set => _scheduler.Value = value; }


        readonly NonNullable<IObjectSerializerFactory> _serializerFactory = Serialization.SerializerFactory.Instance;
        public IObjectSerializerFactory SerializerFactory { get => _serializerFactory.Value; set => _serializerFactory.Value = value; }


        internal ITelemetryWriter TelemetryWriter => _telemetryAggregateWriter;

        public ITelemetrySinkCollection TelemetrySinks => _telemetryAggregateWriter;

        public SslConfiguration SslConfiguration { get; }

        public Uri[] ServerEndpoints { get; private set; }

        public FallbackStrategy FallbackStrategy { get; set; } = FallbackStrategy.Retry;
        public TimeSpan RetryBackoffTime { get; set; } = TimeSpan.FromMilliseconds(150);

        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan DefaultOperationTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan OptimisticOperationTimeout { get; set; } = TimeSpan.FromMilliseconds(250);

        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        readonly NonNullable<Encoding> _encoding = new NonNullable<Encoding>(Encoding.UTF8);
        public Encoding Encoding { get => _encoding.Value; set => _encoding.Value = value; }

        public PipelineMode PipelineMode { get; set; } = PipelineMode.Default;

        public LockStrategy LockStrategy { get; set; } = LockStrategy.Local;

        public int ConnectionPoolSize { get; set; } = 1;

        public int ReadBufferSize { get; set; } = 4096;

        public int WriteBufferSize { get; set; } = 4096;

        public override string ToString()
        {
            var props = GetType()
                .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .Where(p => p.CanRead && (p.PropertyType.IsValueType || p.PropertyType == typeof(string)));

            var str = new StringBuilder();

            foreach (var uri in ServerEndpoints)
            {
                if (str.Length > 0)
                {
                    str.Append("|");
                }

                str.Append($"{uri}");
            }

            str.Append(";");

            foreach (var prop in props)
            {
                var val = prop.GetValue(this);

                if (val == null)
                {
                    continue;
                }

                str.Append($"{prop.Name}={val};");
            }

            str.Append($"{nameof(Encoding)}={Encoding.WebName};");

            if (SslConfiguration.UseSsl)
            {
                str.Append($"{nameof(SslConfiguration.UseSsl)}={SslConfiguration.UseSsl};{nameof(SslConfiguration.SslHost)}={SslConfiguration.SslHost}");
            }

            return str.ToString();
        }

        internal ClientConfiguration Clone(IEnumerable<Uri> newEndpoints)
        {
            var conf = new ClientConfiguration(ToString(), NetworkConfiguration.Clone())
            {
                ServerEndpoints = newEndpoints.ToArray(),
                SerializerFactory = SerializerFactory,
                Encoding = Encoding
            };

            foreach(var writer in _telemetryAggregateWriter)
            {
                conf.TelemetrySinks.Add(writer);
            }

            return conf;
        }

        ConfigurationParser<ClientConfiguration> CreateParser()
        {
            var parser = new ConfigurationParser<ClientConfiguration>();

            parser.RegisterDefault<string>((i, v) => i.ServerEndpoints = v.Split('|').Select(ParseUri).ToArray());
            parser.Register<string>(nameof(NetworkConfiguration.PortMappings), (i, v) => NetworkConfiguration.PortMappings.Import(v));
            parser.Register<string>(nameof(SslConfiguration.SslHost), (i, v) => SslConfiguration.SslHost = v);
            parser.Register<string>(nameof(SslConfiguration.CertificatePath), (i, v) => SslConfiguration.CertificatePath = v);
            parser.Register<bool>(nameof(SslConfiguration.UseSsl), (i, v) => SslConfiguration.UseSsl = v);
            parser.Register<int>(nameof(Database), (i, v) => Database = v);
            parser.Register<string>("password", (i, v) => {
                if (i.PasswordManager is PasswordManager pwds)
                {
                    pwds.SetDefaultPassword(v);
                    return;
                }

                throw new NotSupportedException(PasswordManager.GetType().FullName);
            });
            parser.RegisterAlias("ssl", nameof(SslConfiguration.UseSsl));

            return parser;
        }

        void PostParse(bool clientNameSet)
        {
            if (!ServerEndpoints.Any())
            {
                throw new ArgumentException("No host supplied");
            }

            if (string.IsNullOrEmpty(SslConfiguration.SslHost))
            {
                SslConfiguration.SslHost = ServerEndpoints.FirstOrDefault()?.Host;
            }

            if (PasswordManager is PasswordManager pwds)
            {
                foreach (var endpoint in ServerEndpoints)
                {
                    var userNamePwd = endpoint.UserInfo;

                    var parts = userNamePwd.Split(':');

                    if (parts.Length == 2)
                    {
                        pwds.SetPassword(endpoint.Host, endpoint.Port, parts[1]);
                    }

                    if (!clientNameSet && !string.IsNullOrEmpty(parts[0]))
                    {
                        ClientName = parts[0];
                    }
                }
            }
        }

        static T ParseEnum<T>(string value) where T : struct
        {
            if (Enum.TryParse(value, true, out T result))
            {
                return result;
            }
            throw new ArgumentException(value);
        }

        Uri ParseUri(string uri)
        {
            if (Uri.TryCreate(uri, UriKind.Absolute, out var result) && !string.IsNullOrEmpty(result.Host))
            {
                return result;
            }

            if (!uri.Contains(':'))
            {
                uri += $":{SslConfiguration.DefaultPort}";
            }

            return new Uri($"redis://{uri}");
        }

        static string ValidateSpaceFree(string value, string argName)
        {
            if (value == null)
            {
                throw new ArgumentException(argName);
            }

            if (value.Any(x => char.IsWhiteSpace(x) || char.IsControl(x)))
            {
                throw new ArgumentException(argName);
            }

            return value;
        }
    }
}