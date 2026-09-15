using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Project2FA.Repository.Models;
using UNOversal.Services.Logging;
using System.Collections.Generic;
using Project2FA.Shared.Models;

namespace Project2FA.Services.Importer
{
    public class BackupImporterService : IBackupImporterService
    {
        private ILoggingService LoggingService { get; }
        private IAegisBackupImportService AegisBackupService { get; }
        private IAndOTPBackupImportService AndOTPBackupService { get; }
        private ITwoFASBackupImportService TwoFASBackupImportService { get; }

        public ITwofastBackupImportService TwofastBackupImportService { get; }
        public BackupImporterService(
            ILoggingService loggingService, 
            IAegisBackupImportService aegisBackupService,
            IAndOTPBackupImportService andOTPBackupService,
            ITwoFASBackupImportService twoFASBackupImportService,
            ITwofastBackupImportService twofastBackupImportService) 
        {
            LoggingService = loggingService;
            AegisBackupService = aegisBackupService;
            AndOTPBackupService = andOTPBackupService;
            TwoFASBackupImportService = twoFASBackupImportService;
            TwofastBackupImportService = twofastBackupImportService;
        }

        /// <summary>
        /// Gets the file content as string.
        /// </summary>
        /// <param name="storageFile"></param>
        /// <returns></returns>
        private async Task<string> GetFileContent(StorageFile storageFile)
        {
            IRandomAccessStreamWithContentType randomStream = await storageFile.OpenReadAsync();
            using StreamReader streamReader = new StreamReader(randomStream.AsStreamForRead());
            return await streamReader.ReadToEndAsync();
        }

        /// <summary>
        /// Imports the Aegis backup file.
        /// </summary>
        /// <param name="storageFile"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<(List<TwoFACodeModel> accountList,bool successful, Exception? exc)> ImportBackup(StorageFile storageFile, string password, BackupServiceEnum backupServiceEnum)
        {
            try
            {
                Func<string, byte[], Task<(List<TwoFACodeModel> accountList, bool successful)>> import = backupServiceEnum switch
                {
                    BackupServiceEnum.Aegis => AegisBackupService.ImportBackup,
                    BackupServiceEnum.AndOTP => AndOTPBackupService.ImportBackup,
                    BackupServiceEnum.TwoFAS => TwoFASBackupImportService.ImportBackup,
                    BackupServiceEnum.Twofast => TwofastBackupImportService.ImportBackup,
                    _ => null
                };
                if (import is null) return (new List<TwoFACodeModel>(), false, null);

                var (accountList, successful) = await import(await GetFileContent(storageFile), Encoding.UTF8.GetBytes(password));

                return successful
                    ? (accountList, true, null)
                    : (new List<TwoFACodeModel>(), false, null);
            }
            catch (Exception exc)
            {
                // TODO surface a wrong-password hint for InvalidCipherTextException
                await LoggingService.LogException(exc, SettingsService.Instance.LoggingSetting);
                return (new List<TwoFACodeModel>(), false, exc);
            }
        }
    }
}
