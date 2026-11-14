using System.Text.Json.Serialization;

namespace KopolArt.Mobile.Models
{
    public class UpdateInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("updates")]
        public List<UpdateRelease> Updates { get; set; } = new();
    }

    public class UpdateRelease
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("releaseNotes")]
        public string ReleaseNotes { get; set; } = string.Empty;

        [JsonIgnore]
        public string PersianDate => Helpers.PersianFormatHelper.ToPersianDateString(Date);
    }
}
