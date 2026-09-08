using Project2FA.Core.Services.Crypto;
using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Project2FA.Services
{
    public class SerializationCryptoService : ISerializationCryptoService
    {
        /// <summary>
        /// JSON serializer settings.
        /// </summary>
        public JsonSerializerOptions Settings { get; }
        public SerializationCryptoService()
        {
            Settings = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                TypeInfoResolver = SerializationContext.Default
            };
        }

        private static readonly object CryptoGate = new();
        public T DeserializeDecrypt<T>(byte[] keyArray, byte[] initVector, string value, int encryptionVersion)
        {
            lock (CryptoGate)
            {
                CryptoService.Initialize(keyArray, initVector);
                try
                {
                    if (string.IsNullOrEmpty(value)) return default(T);
                    var typeInfo = Settings.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
                        ?? throw new InvalidOperationException("Unsupported serialization type.");
                    return JsonSerializer.Deserialize(value, typeInfo);
                }
                finally { CryptoService.Clear(); }
            }
        }
        public string SerializeEncrypt(byte[] keyArray, byte[] initVector, object parameter, int encryptionVersion)
        {
            lock (CryptoGate)
            {
                CryptoService.Initialize(keyArray, initVector);
                try
                {
                    var typeInfo = Settings.GetTypeInfo(parameter.GetType())
                        ?? throw new InvalidOperationException("Unsupported serialization type.");
                    return JsonSerializer.Serialize(parameter, typeInfo);
                }
                finally { CryptoService.Clear(); }
            }
        }
    }
}
