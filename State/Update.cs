using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Charr.Timers_BlishHUD.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Update {
        [JsonProperty("name")] public String name { get; set; } = "Hero Markers";
        [JsonProperty("createdAt")] public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.MinValue;
        [JsonProperty("url")] public Uri URL { get; set; }
        [JsonProperty("version")] public string Version { get; set; }
        [JsonProperty("supportedModuleVersion")] public string SupportedModuleVersion { get; set; }

        public void Initialize() {
            if (Version.IsNullOrEmpty()) {
                throw new ArgumentNullException();
            }
        }

    }
}
