﻿using System;
using System.Text;
using YaR.Clouds.Base.Requests;
using YaR.Clouds.Base.Requests.Types;

namespace YaR.Clouds.Base.Repos.MailRuCloud.WebM1.Requests
{
    internal class FolderInfoRequest : BaseRequestJson<FolderInfoResult>
    {
        private readonly string _path;
        private readonly bool _isWebLink;
        private readonly int _offset;
        private readonly int _limit;

        public FolderInfoRequest(HttpCommonSettings settings, IAuth auth, string path, bool isWebLink = false, int offset = 0, int limit = int.MaxValue) 
            : base(settings, auth)
        {
            _path = path;
            _isWebLink = isWebLink;
            _offset = offset;
            _limit = limit;
        }

        protected override string RelationalUri => $"/api/m1/folder?access_token={Auth.AccessToken}&offset={_offset}&limit={_limit}";


        //protected override string RelationalUri
        //{
        //    get
        //    {
        //        var url = $"/api/v2/folder?offset={_offset}&limit={_limit}";
        //        if (!Auth.IsAnonymous)
        //            url += $"access_token={Auth.AccessToken}";

        //        return url;
        //        // $"/api/m1/folder?access_token={Auth.AccessToken}&offset={_offset}&limit={_limit}";
        //    }
        //}

        protected override byte[] CreateHttpContent()
        {
            // path sended using POST cause of unprintable Unicode charactes may exists
            // https://github.com/yar229/WebDavMailRuCloud/issues/54
            var data = _isWebLink
                ? $"weblink={_path}"
                : $"home={Uri.EscapeDataString(_path)}";
            return Encoding.UTF8.GetBytes(data);
        }
    }
}
