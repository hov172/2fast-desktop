// Fake only the OS boundary and app settings. Tests compile the production helper.
namespace Project2FA.Core { internal class Constants { } }
namespace UNOversal.Services.Serialization { internal interface ISerializationService { } }
namespace Project2FA.Services.Enums { public enum BiometricPreferEnum { None, No, Prefer } }
namespace Project2FA.Services
{
    public class SettingsService
    {
        public static SettingsService Instance { get; } = new();
        public string DataFilePath { get; set; } = "/synthetic/test.2fa";
        public string DataFilePasswordHash { get; set; } = "test-hash";
        public bool ActivateBiometricLogin { get; set; }
        public Enums.BiometricPreferEnum PreferBiometricLogin { get; set; }
    }
}
namespace BiometryService
{
    public enum BiometryExceptionReason { KeyInvalidated }
    public class BiometryException : Exception
    {
        public BiometryExceptionReason Reason => BiometryExceptionReason.KeyInvalidated;
    }
}
namespace Project2FA.Services.Desktop
{
    internal static class DesktopNative
    {
        internal static readonly Dictionary<string, string> Items = new();
        internal static readonly List<string> Deleted = new();
        internal static bool FailWrites;
        internal static void Write(string key, string value)
        {
            if (FailWrites) throw new InvalidOperationException("Simulated Keychain failure");
            Items[key] = value;
        }
        internal static string Read(string key) => Items.TryGetValue(key, out var value) ? value : throw new BiometryService.BiometryException();
        internal static void Delete(string key) { Deleted.Add(key); Items.Remove(key); }
    }
}
