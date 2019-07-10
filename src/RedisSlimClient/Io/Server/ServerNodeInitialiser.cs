using RedisSlimClient.Configuration;

namespace RedisSlimClient.Io
{
    class ServerNodeInitialiser
    {
        private readonly ICommandPipeline _pipeline;
        private readonly IClientCredentials _clientCredentials;

        public ServerNodeInitialiser(ICommandPipeline pipeline, IClientCredentials clientCredentials)
        {
            _pipeline = pipeline;
            _clientCredentials = clientCredentials;
        }
    }
}