using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using OtpNet;
using Project2FA.Repository.Models;
using Project2FA.Repository.Models.Enums;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UNOversal.Services.Logging;
using UNOversal.Services.Serialization;

// based on
// https://github.com/stratumauth/app/blob/48db7ed40cefa6e3d20b32172cb18da29da78503/Stratum.Core/src/Converter/AndOtpBackupConverter.cs
// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace Project2FA.Services.Importer
{
    public class AndOTPBackupImportService : IAndOTPBackupImportService
    {
        private const string AlgorithmDescription = BackupCryptoHelper.AlgorithmDescription;

        private const int IterationsLength = 4;
        private const int SaltLength = 12;
        private const int IvLength = 12;

        // Above this the payload is not a valid andOTP backup, not a slow one.
        private const uint MaxIterations = 500000;

        ISerializationService SerializationService { get; }
        private ILoggingService LoggingService { get; }
        public AndOTPBackupImportService(ILoggingService loggingService, ISerializationService serializationService)
        {
            LoggingService = loggingService;
            SerializationService = serializationService;
        }

        public async Task<(List<TwoFACodeModel> accountList, bool successful)> ImportBackup(string content, byte[] bytePassword)
        {
            string json = bytePassword is null || bytePassword.Length == 0
                ? content
                : Decrypt(Encoding.UTF8.GetBytes(content), bytePassword);

            if (string.IsNullOrWhiteSpace(json))
            {
                return (new List<TwoFACodeModel>(), false);
            }

            List<AndOTPModel<string>> decryptedModel = SerializationService.Deserialize<List<AndOTPModel<string>>>(json);
            List<TwoFACodeModel> accountList = new List<TwoFACodeModel>();

            for (int i = 0; i < decryptedModel.Count; i++)
            {
                // check if the authentication methode is supported
                if (decryptedModel[i].Type == OTPType.totp.ToString().ToUpper() || decryptedModel[i].Type == OTPType.steam.ToString().ToUpper())
                {
                    OtpHashMode algorithm;
                    try
                    {
                        algorithm = BackupCryptoHelper.ToHashMode(decryptedModel[i].Algorithm);

                    }
                    catch (Exception exc)
                    {
                        await LoggingService.LogException(exc, SettingsService.Instance.LoggingSetting);
                        throw;
                    }

                    var model = new TwoFACodeModel
                    {
                        Label = decryptedModel[i].Label,
                        TotpSize = decryptedModel[i].Digits,
                        Issuer = decryptedModel[i].Issuer,
                        Period = decryptedModel[i].Period,
                        HashMode = algorithm,
                        SelectedCategories = new System.Collections.ObjectModel.ObservableCollection<CategoryModel>(),
                        SecretByteArray = Base32Encoding.ToBytes(decryptedModel[i].Secret),
                        AccountIconName = DataService.Instance.GetIconForLabel(decryptedModel[i].Label.ToLower())
                    };
                    if (string.IsNullOrWhiteSpace(model.Issuer))
                    {
                        model.Issuer = decryptedModel[i].Label;
                    }
                    if (decryptedModel[i].Type == OTPType.steam.ToString().ToUpper())
                    {
                        model.OTPType = OTPType.steam.ToString();
                    }
                    accountList.Add(model);
                }
                else
                {
                    accountList.Add(new TwoFACodeModel
                    {
                        Label = decryptedModel[i].Label,
                        Issuer = decryptedModel[i].Issuer,
                        AccountIconName = DataService.Instance.GetIconForLabel(decryptedModel[i].Label.ToLower()),
                        SelectedCategories = new System.Collections.ObjectModel.ObservableCollection<CategoryModel>(),
                        IsEnabled = false,
                        IsChecked = false
                    });
                }
            }
            return (accountList, true);
        }

        private KeyParameter DeriveKey(byte[] passwordBytes, byte[] salt, uint iterations)
            => BackupCryptoHelper.DeriveKey(new Sha1Digest(), passwordBytes, salt, (int)iterations);

        private string Decrypt(byte[] data, byte[] passwordBytes)
        {
            var iterations = BinaryPrimitives.ReadUInt32BigEndian(data.Take(IterationsLength).ToArray());
            var salt = data.Skip(IterationsLength).Take(SaltLength).ToArray();
            var iv = data.Skip(IterationsLength + SaltLength).Take(IvLength).ToArray();
            var payload = data.Skip(IterationsLength + SaltLength + IvLength).ToArray();
            if (iterations > MaxIterations)
            {
                // assume that backup format is incorrect and iterations are too high
                return string.Empty;
            }

            var key = DeriveKey(passwordBytes, salt, iterations);

            var keyParameter = new ParametersWithIV(key, iv);
            var cipher = CipherUtilities.GetCipher(AlgorithmDescription);
            cipher.Init(false, keyParameter);

            return Encoding.UTF8.GetString(cipher.DoFinal(payload));
        }
    }
}
