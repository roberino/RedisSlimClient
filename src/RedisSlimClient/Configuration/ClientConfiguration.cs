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

        public ClientConfiguration(string connectionOptions)
        {
            Id = Interlocked.Increment(ref _idCounter);
            SslConfiguration = new SslConfiguration();
            Parse(connectionOptions);
            ClientName = $"RSC{Process.GetCurrentProcess().Id}-{Environment.MachineName}-{_idCounter}";
        }

        public int Id { get; }

        public string ClientName { get; private set; }

        public string Password { get; private set; }

        public IWorkScheduler Scheduler { get; set; } = ThreadPoolScheduler.Instance;

        public SslConfiguration SslConfiguration { get; }

        public Uri[] ServerEndpoints { get; private set; }

        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public PipelineMode PipelineMode { get; set; } = PipelineMode.Default;

        public int ConnectionPoolSize { get; set; } = 1;

        public int ReadBufferSize { get; set; } = 1024;

        public int WriteBufferSize { get; set; } = 1024;

        public IObjectSerializerFactory SerializerFactory { get; set; } = Serialization.SerializerFactory.Instance;

        public ITelemetryWriter TelemetryWriter { get; set; }

        void Parse(string connectionOptions)
        {
            var endPoints = string.Empty;

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
                                break;
                            case nameof(Password):
                                Password = kv[1];
                                break;
                            case nameof(Encoding):
                                Encoding = Encoding.GetEncoding(kv[1]);
                                break;
                            case nameof(DefaultTimeout):
                                DefaultTimeout = TimeSpan.Parse(kv[1]);
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
                        }
                    }
                }
            }

            ServerEndpoints = endPoints.Split(';').Select(ParseUri).ToArray();

            if (SslConfiguration.UseSsl && string.IsNullOrEmpty(SslConfiguration.SslHost))
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

                ClientName = parts[0];
            }
        }

        Uri ParseUri(string uri)
        {
            if (Uri.TryCreate(uri, UriKind.Absolute, out var result))
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