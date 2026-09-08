using System.Reflection;
using System.Runtime.Loader;
var output = Path.GetFullPath(args[0]);
var nativeOutput = args.Length > 2 ? Path.GetFullPath(args[2]) : output;
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var path = Path.Combine(output, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
AssemblyLoadContext.Default.ResolvingUnmanagedDll += (_, name) =>
{
    var path = Path.Combine(output, name.EndsWith(".dylib") ? name : name + ".dylib");
    if (!File.Exists(path)) path = Path.Combine(output, "../MacOS", Path.GetFileName(path));
    if (!File.Exists(path)) path = Path.Combine(nativeOutput, Path.GetFileName(path));
    return File.Exists(path) ? System.Runtime.InteropServices.NativeLibrary.Load(path) : IntPtr.Zero;
};
var app = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(output, "Project2FA.Uno.dll"));
if (args.Length > 1 && args[1] == "--layout-only") { GeneratedCardChecks.Run(app); return; }
var type = app.GetType("Project2FA.Repository.Models.TwoFACodeModel", true)!;
var model = Activator.CreateInstance(type)!;
void Set(string property, object value) => type.GetProperty(property)!.SetValue(model, value);
Set("MobileIdDeviceId", "synthetic-device-identity"); Set("OTPType", "mobileid"); Set("MobileIdChecksum", true); Set("OcraSuite", "OCRA-1:HOTP-SHA1-6:QN08-T1M"); Set("Period", 60);
Set("Label", "Synthetic"); Set("Issuer", "Test"); Set("SecretByteArray", System.Text.Encoding.ASCII.GetBytes("12345678901234567890"));
var serviceType = app.GetType("Project2FA.Services.SerializationCryptoService", true)!;
var service = Activator.CreateInstance(serviceType)!;
var key = new byte[32]; var iv = new byte[16];
var json = (string)serviceType.GetMethod("SerializeEncrypt")!.Invoke(service, new object[] { key, iv, model, 2 })!;
var restored = serviceType.GetMethod("DeserializeDecrypt")!.MakeGenericMethod(type).Invoke(service, new object[] { key, iv, json, 2 })!;
foreach (var target in new[] { restored, type.GetMethod("Clone")!.Invoke(model, null)! })
{
    foreach (var property in new[] { "OTPType", "OcraSuite", "Period", "MobileIdChecksum", "MobileIdDeviceId" })
        if (!Equals(type.GetProperty(property)!.GetValue(model), type.GetProperty(property)!.GetValue(target)))
            throw new Exception("OCRA metadata lost: " + property);
    if (!((byte[])type.GetProperty("SecretByteArray")!.GetValue(target)!).SequenceEqual((byte[])type.GetProperty("SecretByteArray")!.GetValue(model)!))
        throw new Exception("Seed changed during round trip");
}
Console.WriteLine("Compiled app: encrypted OCRA model round trip and clone passed (12 assertions).");

NavigationChecks.Run(app);

if (args.Length > 1) WorkflowChecks.Run(app, Path.GetFullPath(args[1]));

GeneratedCardChecks.Run(app);

CryptoChecks.Run(app);

VaultV4Checks.Run(app, model);
