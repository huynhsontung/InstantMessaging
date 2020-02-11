﻿using InstagramAPI.Classes.User;

namespace InstagramAPI.Classes.Responses
{
    public class LoginResponse
    {
        [JsonProperty("status")] public string Status { get; set; }

        [JsonProperty("logged_in_user")] public UserShort User { get; set; }
    }
}