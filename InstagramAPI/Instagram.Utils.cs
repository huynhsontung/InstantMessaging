﻿using System;
using System.Net;
using Windows.Networking.Connectivity;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InstagramAPI
{
    public partial class Instagram
    {
        private void ValidateLoggedIn()
        {
            if (!IsUserAuthenticated)
            {
                throw new ArgumentException("user must be authenticated");
            }
        }

        private static string GenerateRandomString(int length)
        {
            var rand = new Random();
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            var result = "";
            for (var i = 0; i < length; i++)
            {
                result += pool[rand.Next(0, pool.Length)];
            }

            return result;
        }

        private static string GetRetryContext()
        {
            return new JObject
            {
                {"num_step_auto_retry", 0},
                {"num_reupload", 0},
                {"num_step_manual_retry", 0}
            }.ToString(Formatting.None);
        }

        public static void SetAppCookies(CookieCollection cookies)
        {
            var filter = new HttpBaseProtocolFilter();
            var cookieManager = filter.CookieManager;

            foreach (Cookie netCookie in cookies)
            {
                var cookie = new HttpCookie(netCookie.Name, netCookie.Domain, netCookie.Path)
                {
                    Value = netCookie.Value,
                    HttpOnly = netCookie.HttpOnly,
                    Secure = netCookie.Secure
                };

                if (netCookie.Expires != DateTime.MinValue)
                {
                    cookie.Expires = netCookie.Expires;
                }

                cookieManager.SetCookie(cookie);
            }
        }

        public static bool InternetAvailable()
        {
            var internetProfile = NetworkInformation.GetInternetConnectionProfile();
            return internetProfile != null;
        }

        public static void StartAppCenter()
        {
#if !DEBUG
            AppCenter.Start(Secrets.APPCENTER_SECRET, typeof(Analytics), typeof(Crashes));
#endif
        }
    }
}
