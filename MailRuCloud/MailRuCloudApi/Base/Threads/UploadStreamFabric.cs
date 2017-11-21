﻿using System;
using System.IO;
using System.Threading.Tasks;
using YaR.MailRuCloud.Api.XTSSharp;
using YaR.WebDavMailRu.CloudStore.XTSSharp;

namespace YaR.MailRuCloud.Api.Base.Threads
{
    public class UploadStreamFabric
    {
        private readonly MailRuCloud _cloud;

        public UploadStreamFabric(MailRuCloud cloud)
        {
            _cloud = cloud;
        }

        public async Task<Stream> Create(File file, FileUploadedDelegate onUploaded = null, bool discardEncryption = false)
        {
            if (!(await _cloud.GetItem(file.Path, MailRuCloud.ItemType.Folder) is Folder folder))
                throw new DirectoryNotFoundException(file.Path);

            Stream stream;

            if (folder.CryptRequired && !discardEncryption)
            {
                if (!_cloud.CloudApi.Account.Credentials.CanCrypt)
                    throw new Exception($"Cannot upload {file.FullPath} to crypt folder without additional password!");
                stream = GetCryptoStream(file, onUploaded);
            }
            else
            {
                stream = GetPlainStream(file, onUploaded);
            }

            return stream;
        }

        private Stream GetPlainStream(File file, FileUploadedDelegate onUploaded)
        {
            var stream = new SplittedUploadStream(file.FullPath, _cloud, file.Size);
            if (onUploaded != null) stream.FileUploaded += onUploaded;
            return stream;
        }

        private Stream GetCryptoStream(File file, FileUploadedDelegate onUploaded)
        {
            var info = CryptoUtil.GetCryptoKeyAndSalt(_cloud.CloudApi.Account.Credentials.PasswordCrypt);
            var xts = XtsAes256.Create(info.Key, info.IV);

            file.ServiceInfo.CryptInfo = new CryptInfo
            {
                PublicKey = new CryptoKeyInfo {Salt = info.Salt, IV = info.IV},
                AlignBytes = (uint) (file.Size % XTSWriteOnlyStream.BlockSize != 0
                    ? XTSWriteOnlyStream.BlockSize - file.Size % XTSWriteOnlyStream.BlockSize
                    : 0)
            };

            var size = file.OriginalSize % XTSWriteOnlyStream.BlockSize == 0
                ? file.OriginalSize.DefaultValue
                : (file.OriginalSize / XTSWriteOnlyStream.BlockSize + 1) * XTSWriteOnlyStream.BlockSize;

            var ustream = new SplittedUploadStream(file.FullPath, _cloud, size, false, file.ServiceInfo.CryptInfo);
            if (onUploaded != null) ustream.FileUploaded += onUploaded;
            var encustream = new XTSWriteOnlyStream(ustream, xts, XTSWriteOnlyStream.DefaultSectorSize);

            return encustream;
        }
    }
}