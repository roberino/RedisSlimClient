﻿using RedisSlimClient.Configuration;
using RedisSlimClient.Serialization;
using RedisSlimClient.Serialization.Protocol;
using RedisSlimClient.Types;
using System;
using System.IO;

namespace RedisSlimClient.Io.Commands
{
    class ObjectGetCommand<T> : RedisCommand<T>
    {
        private readonly string _key;
        private readonly ISerializerSettings _configuration;
        private readonly IObjectSerializer<T> _serializer;

        public ObjectGetCommand(string key, ISerializerSettings config) : base("GET")
        {
            _key = key;
            _configuration = config;
            _serializer = config.SerializerFactory.Create<T>();
        }

        protected override T TranslateResult(RedisObject result)
        {
            if (result is RedisString strData)
            {
                var byteSeq = new ArraySegmentToRedisObjectReader(new StreamIterator(strData.ToStream()));
                var objReader = new ObjectReader(byteSeq, _configuration.Encoding, null, _configuration.SerializerFactory);

                return _serializer.ReadData(objReader, default);
            }

            throw new ArgumentException($"{result.Type}");
        }

        public void Write(Stream commandWriter)
        {
            commandWriter.Write(GetArgs());
        }

        public override object[] GetArgs() => new object[] { CommandText, _key };
    }
}