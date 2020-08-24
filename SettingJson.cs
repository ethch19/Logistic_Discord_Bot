using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Logistic_Bot
{
    public struct SettingJson
    {
        [JsonProperty("spreadsheetId")]
        public string SpreadsheetId { get; set; }
        [JsonProperty("sheet")]
        public string Sheet { get; set; }
        [JsonProperty("trainingWebhookId")]
        public ulong? TrainingWebhookId { get; set; }
        [JsonProperty("attendanceWebhookId")]
        public ulong? AttendWebHookId { get; set; }
        [JsonProperty("range")]
        public string Range { get; set; }
        [JsonProperty("appendNumber")]
        public int? AppendNumber { get; set; }
        [JsonProperty("totalColumn")]
        public string TotalColumn { get; set; }
        [JsonProperty("rtColumn")]
        public string RTColumn { get; set; }
        [JsonProperty("ptColumn")]
        public string PTColumn { get; set; }
        [JsonProperty("ctColumn")]
        public string CTColumn { get; set; }
        [JsonProperty("atColumn")]
        public string ATColumn { get; set; }
        [JsonProperty("ltColumn")]
        public string LTColumn { get; set; }
        [JsonProperty("patrolColumn")]
        public string PatrolColumn { get; set; }
        [JsonProperty("inspectColumn")]
        public string InspectColumn { get; set; }
        [JsonProperty("cohostColumn")]
        public string CohostColumn { get; set; }
        [JsonProperty("superColumn")]
        public string SuperColumn { get; set; }
        [JsonProperty("attendColumn")]
        public string AttendColumn { get; set; }
    }
}
