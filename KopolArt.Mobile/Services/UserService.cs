using System.Net;
using System.Text.Json;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public class UserService(ISettingsService? settingsService = null) : IUserService
    {
        public bool IsLoggedIn => GetCurrentUser() != null;

        public KopolUserInfo? GetCurrentUser()
        {
#if ANDROID
            try
            {
                var cookieManager = Android.Webkit.CookieManager.Instance;
                if (cookieManager == null)
                {
                    return null;
                }

                try
                {
                    cookieManager.Flush();
                }
                catch { }

                var candidateUrls = new List<string>
                {
                    Constants.Urls.Home,
                    "https://kopolart.ir",
                    "https://www.kopolart.ir",
                    "https://www.kopolart.ir/",
                    "http://kopolart.ir/",
                    "http://www.kopolart.ir/"
                };

                var lastVisited = settingsService?.Settings?.LastVisitedUrl;
                if (!string.IsNullOrWhiteSpace(lastVisited) && !candidateUrls.Contains(lastVisited, StringComparer.OrdinalIgnoreCase))
                {
                    candidateUrls.Add(lastVisited);
                }

                var cookiePrefix = $"{Constants.Keys.UserCookieName}=";

                foreach (var url in candidateUrls)
                {
                    var cookies = cookieManager.GetCookie(url);
                    if (string.IsNullOrWhiteSpace(cookies))
                    {
                        continue;
                    }

                    var parts = cookies.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var trimmed = part.Trim();
                        if (trimmed.StartsWith(cookiePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            var rawValue = trimmed.Substring(cookiePrefix.Length).Trim();
                            if (string.IsNullOrWhiteSpace(rawValue))
                            {
                                continue;
                            }

                            var userInfo = ParseUserInfo(rawValue);
                            if (userInfo != null && userInfo.UserId > 0)
                            {
                                return userInfo;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserService] Error reading user cookie: {ex.Message}");
            }
#endif
            return null;
        }

        private static KopolUserInfo? ParseUserInfo(string rawValue)
        {
            try
            {
                var decodedJson = rawValue.Trim();

                if (decodedJson.Length >= 2 && decodedJson.StartsWith("\"") && decodedJson.EndsWith("\""))
                {
                    decodedJson = decodedJson.Substring(1, decodedJson.Length - 2).Trim();
                }

                for (int i = 0; i < 3 && decodedJson.Contains('%'); i++)
                {
                    var next = WebUtility.UrlDecode(decodedJson);
                    if (next == decodedJson) break;
                    decodedJson = next;
                }

                if (string.IsNullOrWhiteSpace(decodedJson) || !decodedJson.StartsWith("{"))
                {
                    return null;
                }

                try
                {
                    var userInfo = JsonSerializer.Deserialize(decodedJson, AppJsonContext.Default.KopolUserInfo);
                    if (userInfo != null && userInfo.UserId > 0)
                    {
                        return userInfo;
                    }
                }
                catch
                {
                    // Fall back to JsonDocument
                }

                using var doc = JsonDocument.Parse(decodedJson);
                long uid = 0;
                string name = string.Empty;

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Name.Equals("userId", StringComparison.OrdinalIgnoreCase) ||
                        prop.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt64(out var idNum))
                        {
                            uid = idNum;
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.String && long.TryParse(prop.Value.GetString(), out var idParsed))
                        {
                            uid = idParsed;
                        }
                    }
                    else if (prop.Name.Equals("name", StringComparison.OrdinalIgnoreCase))
                    {
                        name = prop.Value.GetString() ?? string.Empty;
                    }
                }

                if (uid > 0)
                {
                    return new KopolUserInfo
                    {
                        UserId = uid,
                        Name = name
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserService] ParseUserInfo error: {ex.Message}");
            }

            return null;
        }
    }
}
