using RedisTribute.Io.Scheduling;
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

        public ClientConfiguration(string connectionOptions, NetworkConfiguration networkConfiguration = null)
        {
            Id = Interlocked.Increment(ref _idCounter);
            SslConfiguration = new SslConfiguration();
            ClientName = $"RSC{Process.GetCurrentProcess().Id}-{Environment.MachineName}-{Id}";
            NetworkConfiguration = networkConfiguration ?? new NetworkConfiguration();

            Parse(connectionOptions);
        }

        public int Id { get; }

        public NetworkConfiguration NetworkConfiguration { get; }

        public string ClientName { get; private set; }

        public string Password { get; set; }

        readonly NonNullable<IWorkScheduler> _scheduler = ThreadPoolScheduler.Instance;
        public IWorkScheduler Scheduler { get => _scheduler.Value; set => _scheduler.Value = value; }


        readonly NonNullable<IObjectSerializerFactory> _serializerFactory = Serialization.SerializerFactory.Instance;
        public IObjectSerializerFactory SerializerFactory { get => _serializerFactory.Value; set => _serializerFactory.Value = value; }

        readonly NonNullable<ITelemetryWriter> _telemetryWriter = new NonNullable<ITelemetryWriter>(NullWriter.Instance);
        public ITelemetryWriter TelemetryWriter { get => _telemetryWriter.Value; set => _telemetryWriter.Value = value; }

        public SslConfiguration SslConfiguration { get; }

        public Uri[] ServerEndpoints { get; private set; }

        public FallbackStrategy FallbackStrategy { get; set; } = FallbackStrategy.Retry;

        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan DefaultOperationTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public PipelineMode PipelineMode { get; set; } = PipelineMode.Default;

        public int ConnectionPoolSize { get; set; } = 1;

        public int ReadBufferSize { get; set; } = 1024;

        public int WriteBufferSize { get; set; } = 1024;

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
            return new ClientConfiguration(ToString(), NetworkConfiguration.Clone())
            {
                ServerEndpoints = newEndpoints.ToArray(),
                TelemetryWriter = TelemetryWriter,
                SerializerFactory = SerializerFactory,
                Encoding = Encoding
            };
        }

        void Parse(string connectionOptions)
        {
            var endPoints = string.Empty;
            var clientNameSet = false;

            foreach (var item in connectionOptions.Split(',', ';'))
            {
                var kv = item.Split('=');

                if (kv.Length == 1)
                {
                    if (string.IsNullOrEmpty(kv[0]))
                    {
                        continue;
                    }

                    endPoints = kv[0];
                }
                else
                {
                    if (kv.Length == 2)
                    {
                        switch (kv[0])
                        {
                            case nameof(ClientName):
                                ClientName = ValidateSpaceFree(kv[1], nameof(ClientName));
                                clientNameSet = true;
                                break;
                            case nameof(Password):
                                Password = kv[1];
                                break;
                            case nameof(Encoding):
                                Encoding = Encoding.GetEncoding(kv[1]);
                                break;
                            case nameof(DefaultOperationTimeout):
                                DefaultOperationTimeout = TimeSpan.Parse(kv[1]);
                                break;
                            case nameof(ConnectTimeout):
                                ConnectTimeout = TimeSpan.Parse(kv[1]);
                                break;
                            case nameof(ConnectionPoolSize):
                                ConnectionPoolSize = int.Parse(kv[1]);
                                break;
                            case nameof(ReadBufferSize):
                                ReadBufferSize = int.Parse(kv[1]);
                                break;
                            case nameof(WriteBufferSize):
                                WriteBufferSize = int.Parse(kv[1]);
                                break;
                            case nameof(FallbackStrategy):
                                FallbackStrategy = ParseEnum<FallbackStrategy>(kv[1]);
                                break;
                            case nameof(SslConfiguration.SslHost):
                                SslConfiguration.SslHost = kv[1];
                                break;
                            case nameof(SslConfiguration.UseSsl):
                                SslConfiguration.UseSsl = bool.Parse(kv[1].ToLower());
                                break;
                            case nameof(SslConfiguration.CertificatePath):
                                SslConfiguration.CertificatePath = kv[1];
                                break;
                            case nameof(PipelineMode):
                                PipelineMode = ParseEnum<PipelineMode>(kv[1]);
                                break;
                            case nameof(NetworkConfiguration.PortMappings):
                                NetworkConfiguration.PortMappings.Import(kv[1]);
                                break;
                        }
                    }
                }
            }

            ServerEndpoints = endPoints.Split('|').Select(ParseUri).ToArray();

            if (!ServerEndpoints.Any())
            {
                throw new ArgumentException("No host supplied");
            }

            if (string.IsNullOrEmpty(SslConfiguration.SslHost))
            {
                SslConfiguration.SslHost = ServerEndpoints.FirstOrDefault()?.Host;
            }

            if (string.IsNullOrEmpty(Password))
            {
                var userNamePwd = ServerEndpoints.FirstOrDefault()?.UserInfo;

                var parts = userNamePwd.Split(':');

                if (parts.Length == 2)
                {
                    Password = parts[1];
                }

                if (!clientNameSet && !string.IsNullOrEmpty(parts[0]))
                {
                    ClientName = parts[0];
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