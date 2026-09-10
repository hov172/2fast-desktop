using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using OtpNet;
using Project2FA.Repository.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UNOversal.Services.Logging;
using UNOversal.Services.Serialization;

// based on
// https://github.com/stratumauth/app/blob/f79d5f3349ce223232d490ebca3959ea5f72cc71/Stratum.Core/src/Converter/TwoFasBackupConverter.cs
// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace Project2FA.Services.Importer
{
    public class TwoFASBackupImportService : ITwoFASBackupImportService
    {
        private const string AlgorithmDescription = BackupCryptoHelper.AlgorithmDescription;

        private const int Iterations = 10000;

        ISerializationService SerializationService { get; }
        private ILoggingService LoggingService { get; }
        public TwoFASBackupImportService(ILoggingService loggingService, ISerializationService serializationService)
        {
            LoggingService = loggingService;
            SerializationService = serializationService;
        }

        public async Task<(List<TwoFACodeModel> accountList, bool successful)> ImportBackup(string content, byte[] bytePassword)
        {
            List<TwoFACodeModel> accountList = new List<TwoFACodeModel>();
            var backup = SerializationService.Deserialize<TwoFASBackup>(content);

            if (backup.ServicesEncrypted != null)
            {
                if (bytePassword is null || bytePassword.Length == 0)
                {
                    //throw new ArgumentException("Password required but not provided");
                    return (new List<TwoFACodeModel>(), false);
                }

                var decryptedContent = DecryptServices(backup.ServicesEncrypted, bytePassword);
                if (!string.IsNullOrWhiteSpace(decryptedContent))
                {
                    backup.Services = SerializationService.Deserialize<List<TwoFASServiceModel>>(decryptedContent);

                    for (int i = 0; i < backup.Services.Count; i++)
                    {
                        if (backup.Services[i].Otp.TokenType == "TOTP")
                        {
                            OtpHashMode algorithm;
                            try
                            {
                                algorithm = BackupCryptoHelper.ToHashMode(backup.Services[i].Otp.Algorithm);
                            }
                            catch (Exception exc)
                            {
                                await LoggingService.LogException(exc, SettingsService.Instance.LoggingSetting);
                                throw;
                            }


                            var model = new TwoFACodeModel
                            {
                                Label = backup.Services[i].Name,
                                Issuer = backup.Services[i].Otp.Issuer,
                                Period = backup.Services[i].Otp.Period,
                                TotpSize = backup.Services[i].Otp.Digits,
                                HashMode = algorithm,
                                SecretByteArray = Base32Encoding.ToBytes(backup.Services[i].Secret),
                                AccountIconName = DataService.Instance.GetIconForLabel(backup.Services[i].Name.ToLower())
                            };
                            accountList.Add(model);
                        }
                        else
                        {
                            accountList.Add(new TwoFACodeModel
                            {
                                Label = backup.Services[i].Name,
                                Issuer = backup.Services[i].Otp.Issuer,
                                AccountIconName = DataService.Instance.GetIconForLabel(backup.Services[i].Name.ToLower()),
                                IsEnabled = false,
                                IsChecked = false
                            });
                        }

                    }
                    return (accountList, true);
                }
                else
                {
                    return (new List<TwoFACodeModel>(), false);
                }
            }
            else
            {
                // without password
            }
            return (new List<TwoFACodeModel>(), false);
        }

        private string DecryptServices(string payload, byte[] bytePassword)
        {
            var parts = payload.Split(":", 3);

            if (parts.Length < 3)
            {
                throw new ArgumentException("Invalid payload format");
            }

            var encryptedData = Convert.FromBase64String(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var iv = Convert.FromBase64String(parts[2]);

            var key = DeriveKey(bytePassword, salt);
            var keyParameter = new ParametersWithIV(key, iv);
            var cipher = CipherUtilities.GetCipher(AlgorithmDescription);
            cipher.Init(false, keyParameter);

            byte[] decryptedBytes;

            try
            {
                decryptedBytes = cipher.DoFinal(encryptedData);
                var decryptedJson = Encoding.UTF8.GetString(decryptedBytes);
                return decryptedJson;
            }
            catch (InvalidCipherTextException e)
            {
                //throw new BackupPasswordException("The password is incorrect", e);
                return string.Empty;
            }
        }

        private KeyParameter DeriveKey(byte[] bytePassword, byte[] salt)
            => BackupCryptoHelper.DeriveKey(new Sha256Digest(), bytePassword, salt, Iterations);
    }
}
