using System.Text.Json.Serialization;

namespace KopolArt.Mobile.Models
{
    public class KopolUserInfo
    {
        [JsonPropertyName("userId")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long UserId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
