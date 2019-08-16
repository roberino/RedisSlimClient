using RedisSlimClient.Io.Scheduling;
using RedisSlimClient.Serialization;
using RedisSlimClient.Telemetry;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace RedisSlimClient.Configuration
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

        public string Password { get; private set; }


        readonly NonNullable<IWorkScheduler> _scheduler = ThreadPoolScheduler.Instance;
        public IWorkScheduler Scheduler { get => _scheduler.Value; set => _scheduler.Value = value; }


        readonly NonNullable<IObjectSerializerFactory> _serializerFactory = Serialization.SerializerFactory.Instance;
        public IObjectSerializerFactory SerializerFactory { get => _serializerFactory.Value; set => _serializerFactory.Value = value; }

        readonly NonNullable<ITelemetryWriter> _telemetryWriter = new NonNullable<ITelemetryWriter>(NullWriter.Instance);
        public ITelemetryWriter TelemetryWriter { get => _telemetryWriter.Value; set => _telemetryWriter.Value = value; }

        public SslConfiguration SslConfiguration { get; }

        public Uri[] ServerEndpoints { get; private set; }

        public TimeSpan DefaultOperationTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public PipelineMode PipelineMode { get; set; } = PipelineMode.Default;

        public int ConnectionPoolSize { get; set; } = 1;

        public int ReadBufferSize { get; set; } = 1024;

        public int WriteBufferSize { get; set; } = 1024;

        void Parse(string connectionOptions)
        {
            var endPoints = string.Empty;
            var clientNameSet = false;

            foreach (var item in connectionOptions.Split(',', ';'))
            {
                var kv = item.Split('=');

                if (kv.Length == 1)
                {
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
                            case nameof(ConnectionPoolSize):
                                ConnectionPoolSize = int.Parse(kv[1]);
                                break;
                            case nameof(ReadBufferSize):
                                ReadBufferSize = int.Parse(kv[1]);
                                break;
                            case nameof(WriteBufferSize):
                                WriteBufferSize = int.Parse(kv[1]);
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
                                PipelineMode = (PipelineMode)Enum.Parse(typeof(PipelineMode), kv[1], true);
                                break;
                            case nameof(NetworkConfiguration.PortMappings):
                                NetworkConfiguration.PortMappings.Import(kv[1]);
                                break;
                        }
                    }
                }
            }

            ServerEndpoints = endPoints.Split(';').Select(ParseUri).ToArray();

            if (string.IsNullOrEmpty(SslConfiguration.SslHost))
            {
                SslConfiguration.SslHost = ServerEndpoints.SingleOrDefault()?.Host;
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