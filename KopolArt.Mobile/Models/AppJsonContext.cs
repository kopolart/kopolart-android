using System.Text.Json.Serialization;

namespace KopolArt.Mobile.Models
{
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(AppSettings))]
    [JsonSerializable(typeof(HistoryItem))]
    [JsonSerializable(typeof(List<HistoryItem>))]
    [JsonSerializable(typeof(DownloadItem))]
    [JsonSerializable(typeof(List<DownloadItem>))]
    [JsonSerializable(typeof(UsageRecord))]
    [JsonSerializable(typeof(List<UsageRecord>))]
    [JsonSerializable(typeof(BookmarkItem))]
    [JsonSerializable(typeof(List<BookmarkItem>))]
    [JsonSerializable(typeof(BackupDataModel))]
    [JsonSerializable(typeof(BackupPayload))]
    [JsonSerializable(typeof(UsageBackupData))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(KopolUserInfo))]
    [JsonSerializable(typeof(DownloadPromptResult))]
    [JsonSerializable(typeof(OnlineUsersInfo))]
    [JsonSerializable(typeof(OfflinePageItem))]
    [JsonSerializable(typeof(List<OfflinePageItem>))]
    [JsonSerializable(typeof(SearchSuggestionItem))]
    [JsonSerializable(typeof(List<SearchSuggestionItem>))]
    [JsonSerializable(typeof(UpdateInfo))]
    [JsonSerializable(typeof(List<UpdateRelease>))]
    [JsonSerializable(typeof(UpdateRelease))]
    public partial class AppJsonContext : JsonSerializerContext
    {
    }
}
