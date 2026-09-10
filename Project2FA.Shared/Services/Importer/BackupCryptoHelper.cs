using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using OtpNet;
using System;

namespace Project2FA.Services.Importer
{
    /// <summary>
    /// Scaffolding shared by the password-protected backup importers. Each format
    /// keeps its own digest, iteration count and payload layout — only the parts
    /// that are identical across formats live here.
    /// </summary>
    internal static class BackupCryptoHelper
    {
        internal const string BaseAlgorithm = "AES";
        internal const string Mode = "GCM";
        internal const string Padding = "NoPadding";
        internal const string AlgorithmDescription = BaseAlgorithm + "/" + Mode + "/" + Padding;
        internal const int KeyLength = 32;

        internal static KeyParameter DeriveKey(IDigest digest, byte[] passwordBytes, byte[] salt, int iterations)
        {
            var generator = new Pkcs5S2ParametersGenerator(digest);
            generator.Init(passwordBytes, salt, iterations);
            return (KeyParameter)generator.GenerateDerivedParameters(BaseAlgorithm, KeyLength * 8);
        }

        internal static OtpHashMode ToHashMode(string algorithm)
        {
            return algorithm switch
            {
                "SHA1" => OtpHashMode.Sha1,
                "SHA256" => OtpHashMode.Sha256,
                "SHA512" => OtpHashMode.Sha512,
                _ => throw new ArgumentException($"Algorithm '{algorithm}' not supported")
            };
        }
    }
}
