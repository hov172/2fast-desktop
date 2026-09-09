using System;
using System.Threading;
using System.Threading.Tasks;
using UNOversal.Services.File;
using Windows.Storage;

namespace UNOversal.Services.Logging
{
    public class LoggingService : ILoggingService
    {
        private const string LogName = "AppLog.safe.log";
        private readonly SemaphoreSlim accessSemaphore = new SemaphoreSlim(1);
        public SemaphoreSlim AccessSemaphore => accessSemaphore;
        private IFileService FileService { get; }

        public LoggingService(IFileService fileService) { FileService = fileService; }

        public Task Log(string message, LoggingPreferEnum preference) =>
            preference == LoggingPreferEnum.Full ? Append("Application event\n") : Task.CompletedTask;

        public Task LogException(Exception error, LoggingPreferEnum preference) =>
            preference == LoggingPreferEnum.Simple || preference == LoggingPreferEnum.Full
                ? Append(SafeLogRecord.Format(error)) : Task.CompletedTask;

        private async Task Append(string record)
        {
            await AccessSemaphore.WaitAsync();
            try
            {
                // Remove only this app's legacy unredacted diagnostic file.
                if (await FileService.FileExistsAsync("AppLog.log", ApplicationData.Current.LocalFolder))
                    await (await ApplicationData.Current.LocalFolder.GetFileAsync("AppLog.log")).DeleteAsync();
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(LogName, CreationCollisionOption.OpenIfExists);
                await FileIO.AppendTextAsync(file, DateTimeOffset.UtcNow.ToString("O") + " " + record);
            }
            catch { /* Diagnostics must not break authentication or vault operations. */ }
            finally { AccessSemaphore.Release(); }
        }
    }
}
