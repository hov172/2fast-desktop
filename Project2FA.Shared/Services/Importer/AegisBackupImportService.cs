using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using OtpNet;
using Project2FA.Core;
using Project2FA.Repository.Models;
using Project2FA.Repository.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UNOversal.Services.Serialization;

// based on
// https://github.com/stratumauth/app/blob/1f74c5fbfd88eb81a58b1ba71dc3423ac779dc06/Stratum.Core/src/Converter/AegisBackupConverter.cs
// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

namespace Project2FA.Services.Importer
{
    public class AegisBackupImportService : IAegisBackupImportService
    {
        ISerializationService SerializationService { get; }
        public AegisBackupImportService(ISerializationService serializationService)
        {
            SerializationService = serializationService;
        }
        public Task<(List<TwoFACodeModel> accountList, bool successful)> ImportBackup(string content, byte[] bytePassword)
        {
            if (bytePassword is null || bytePassword.Length == 0)
            {
                var model = SerializationService.Deserialize<AegisModel<AegisDecryptedDatabase>>(content);

                return Task.FromResult((CreateAccountCollection(model.Database.Entries), true));
            }

            AegisModel<string> encryptedModel = SerializationService.Deserialize<AegisModel<string>>(content);

            // search for password slot
            var slot = encryptedModel.Header.Slots?.FirstOrDefault(x => x.Type == AegisSlotType.Password);
            if (slot == null)
            {
                return Task.FromResult((new List<TwoFACodeModel>(), false));
            }

            // get data and initialisation vector
            byte[] databaseBytes = Convert.FromBase64String(encryptedModel.Database);
            byte[] ivBytes = Hex.Decode(encryptedModel.Header.Params.Nonce);
            byte[] macBytes = Hex.Decode(encryptedModel.Header.Params.Tag);

            var masterKey = DecryptSlot(slot, bytePassword, BackupCryptoHelper.AlgorithmDescription);
            // decrypt
            var decryptedBytes = DecryptAesGcm(masterKey, ivBytes, databaseBytes, macBytes, BackupCryptoHelper.AlgorithmDescription);
            var json = Encoding.UTF8.GetString(decryptedBytes);
            var database = SerializationService.Deserialize<AegisDecryptedDatabase>(json);

            return Task.FromResult((CreateAccountCollection(database.Entries), true));
        }

        private List<TwoFACodeModel> CreateAccountCollection(List<AegisEntry> entries)
        {
            List<TwoFACodeModel> accountList = new List<TwoFACodeModel>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Type == OTPType.totp.ToString() || entries[i].Type == OTPType.steam.ToString())
                {
                    var model = new TwoFACodeModel
                    {
                        Label = entries[i].Name,
                        TotpSize = entries[i].Info.Digits,
                        Issuer = entries[i].Issuer,
                        Period = entries[i].Info.Period,
                        HashMode = entries[i].Info.HashMode,
                        SelectedCategories = new System.Collections.ObjectModel.ObservableCollection<CategoryModel>(),
                        SecretByteArray = Base32Encoding.ToBytes(entries[i].Info.Secret),
                        AccountIconName = DataService.Instance.GetIconForLabel(entries[i].Name.ToLower()),
                    };
                    if (string.IsNullOrWhiteSpace(model.Issuer))
                    {
                        model.Issuer = entries[i].Name;
                    }
                    if (entries[i].Type == OTPType.steam.ToString())
                    {
                        model.OTPType = OTPType.steam.ToString();
                    }
                    accountList.Add(model);
                }
                else
                {
                    accountList.Add(new TwoFACodeModel
                    {
                        Label = entries[i].Name,
                        Issuer = entries[i].Issuer,
                        SelectedCategories = new System.Collections.ObjectModel.ObservableCollection<CategoryModel>(),
                        AccountIconName = DataService.Instance.GetIconForLabel(entries[i].Name.ToLower()),
                        IsEnabled = false,
                        IsChecked = false
                    });
                }
            }
            return accountList;
        }

        private byte[] DecryptSlot(AegisSlot slot, byte[] password, string algorithm)
        {
            var saltBytes = Hex.Decode(slot.Salt);
            var derivedKey = SCrypt.Generate(password, saltBytes, slot.N, slot.R, slot.P, BackupCryptoHelper.KeyLength);

            var ivBytes = Hex.Decode(slot.KeyParams.Nonce);
            var keyBytes = Hex.Decode(slot.Key);
            var macBytes = Hex.Decode(slot.KeyParams.Tag);

            return DecryptAesGcm(derivedKey, ivBytes, keyBytes, macBytes, algorithm);
        }

        private byte[] DecryptAesGcm(byte[] key, byte[] iv, byte[] data, byte[] mac, string algorithm)
        {
            var keyParameter = new ParametersWithIV(new KeyParameter(key), iv);
            var cipher = CipherUtilities.GetCipher(algorithm);
            cipher.Init(false, keyParameter);

            var authenticatedBytes = GetAuthenticatedBytes(data, mac);
            return cipher.DoFinal(authenticatedBytes);
        }


        private byte[] GetAuthenticatedBytes(byte[] payload, byte[] mac)
        {
            var result = new byte[payload.Length + mac.Length];
            System.Buffer.BlockCopy(payload, 0, result, 0, payload.Length);
            System.Buffer.BlockCopy(mac, 0, result, payload.Length, mac.Length);
            return result;
        }
    }
}
