using System.Text.Json.Serialization;

namespace KopolArt.Mobile.Models
{
    /// <summary>
    /// Model representing online users returned by https://kopolart.ir/api/topbar-info.
    /// Extraneous fields like 'text' are ignored by design.
    /// </summary>
    public class OnlineUsersInfo
    {
        [JsonPropertyName("online")]
        public int Online { get; set; }
    }
}
