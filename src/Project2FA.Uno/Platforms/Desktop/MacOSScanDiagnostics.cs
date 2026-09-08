#if TWOFAST_DESKTOP
using System.Diagnostics;
using System.Text;

namespace Project2FA.Services.MacOS;

internal static class MacOSScanDiagnostics
{
    internal static void Record(string stage, Exception error)
    {
        // Never record exception messages/Data: either may contain token input.
        try
        {
            var report = new StringBuilder().AppendLine(DateTimeOffset.UtcNow.ToString("O")).AppendLine(stage);
            for (Exception? current = error; current != null; current = current.InnerException)
            {
                report.AppendLine(current.GetType().FullName);
                foreach (var frame in new StackTrace(current, false).GetFrames())
                {
                    var method = frame.GetMethod();
                    report.AppendLine(method?.DeclaringType?.FullName + "." + method?.Name);
                }
            }
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Project2FA");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "last-qr-error.txt"), report.ToString());
        }
        catch { /* Diagnostics must not obscure the original failure. */ }
    }
}
#endif
