using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Logistic_Bot.Commands
{
    public struct AdminCacheJson
    {
        [JsonProperty("AdminRoleName")]
        public string adminRoleName { get; set; }
        [JsonProperty("SetUp")]
        public bool setUp { get; set; }
        [JsonProperty("Enabled")]
        public bool enabled { get; set; }
    }
}
