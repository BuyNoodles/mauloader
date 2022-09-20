using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace mauloader_bot.Entities
{
    internal class Joke
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("value")]
        public string Content { get; set; }
    }
}
