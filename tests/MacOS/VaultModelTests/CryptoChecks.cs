using System.Reflection;
using System.Security.Cryptography;
internal static class CryptoChecks
{
    internal static void Run(Assembly app)
    {
        var modelType = app.GetType("Project2FA.Repository.Models.TwoFACodeModel", true)!;
        var serviceType = app.GetType("Project2FA.Services.SerializationCryptoService", true)!;
        var encrypt = serviceType.GetMethod("SerializeEncrypt")!;
        var decrypt = serviceType.GetMethod("DeserializeDecrypt")!.MakeGenericMethod(modelType);
        Parallel.For(0, 40, i =>
        {
            var model = Activator.CreateInstance(modelType)!;
            byte[] seed = System.Text.Encoding.ASCII.GetBytes("synthetic-seed-for-account-" + i);
            modelType.GetProperty("SecretByteArray")!.SetValue(model, seed);
            modelType.GetProperty("Label")!.SetValue(model, "Synthetic " + i);
            var key = RandomNumberGenerator.GetBytes(32); var iv = RandomNumberGenerator.GetBytes(16);
            var keyCopy = key.ToArray(); var ivCopy = iv.ToArray();
            var service = Activator.CreateInstance(serviceType)!;
            var json = (string)encrypt.Invoke(service, new object[] { key, iv, model, 2 })!;
            var restored = decrypt.Invoke(service, new object[] { key, iv, json, 2 })!;
            if (!seed.SequenceEqual((byte[])modelType.GetProperty("SecretByteArray")!.GetValue(restored)!) ||
                !key.SequenceEqual(keyCopy) || !iv.SequenceEqual(ivCopy))
                throw new Exception("Concurrent crypto mixed data or changed caller key/IV.");
        });
        Console.WriteLine("Crypto regression: 40 concurrent encrypted round trips preserve payloads and caller key/IV arrays.");
    }
}
