using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mauloader_bot.Entities
{
    public class TikTok
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("author_name")]
        public string AuthorName { get; set; }
        [JsonPropertyName("create_time")]
        public string CreatedTime { get; set; }
        [JsonPropertyName("comment_count")]
        public int CommentCount { get; set; }
        [JsonPropertyName("like_count")]
        public int LikeCount { get; set; }
        [JsonPropertyName("share_count")]
        public int ShareCount { get; set; }
        [JsonIgnore]
        public string DownloadLink { get; set; }

    }
}
