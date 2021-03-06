﻿using System.Collections.Generic;
using System.Net;
using InstagramAPI.Classes.JsonConverters;
using InstagramAPI.Classes.User;
using InstagramAPI.Push;
using Newtonsoft.Json;
using InstagramAPI.Classes.Android;
using InstagramAPI.Classes.Challenge;

namespace InstagramAPI.Classes.Core
{
    public class UserSessionData
    {
        [JsonIgnore] public bool IsAuthenticated => LoggedInUser?.Pk > 0;

        [JsonIgnore] public string SessionName => IsAuthenticated ? LoggedInUser.Pk.ToString() : string.Empty;

        [JsonIgnore] public TwoFactorLoginInfo TwoFactorInfo { get; internal set; }

        [JsonIgnore] public ChallengeLoginInfo ChallengeInfo { get; internal set; }

        [JsonProperty]
        public string Username { get; set; }

        [JsonProperty]
        public string Password { get; set; }

        [JsonProperty]
        public BaseUser LoggedInUser { get; internal set; }

        /// <summary>
        ///     Only for facebook login
        /// </summary>
        [JsonProperty]
        public string FacebookUserId { get; internal set; }

        [JsonProperty]
        public string FacebookAccessToken { get; internal set; }

        [JsonProperty]
        public string AuthorizationToken { get; internal set; }

        [JsonProperty]
        [JsonConverter(typeof(CookieCollectionConverter))]
        public CookieCollection Cookies { get; internal set; }

        [JsonProperty]
        internal FbnsConnectionData PushData { get; }

        [JsonProperty]
        public AndroidDevice Device { get; }

        public UserSessionData()
        {
            Device = AndroidDevice.GetRandomAndroidDevice();
            PushData = new FbnsConnectionData();
        }
    }
}