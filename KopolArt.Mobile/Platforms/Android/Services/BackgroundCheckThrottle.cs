using System.Globalization;

namespace KopolArt.Mobile.Platforms.Android
{
    /// <summary>
    /// Guards the daily background job. The JobScheduler job is registered with a 24 hour period,
    /// but boot / app-update re-registration can execute it earlier, so the job itself also keeps
    /// a "last check" timestamp and skips runs that happen too soon after the previous one.
    /// This throttle only limits how often we poll; the user-visible notification cadence stays
    /// capped by RssFeedService (at most one new-post notification every 72 hours).
    /// </summary>
    internal static class BackgroundCheckThrottle
    {
        /// <summary>
        /// Slightly under 24 hours so normal JobScheduler jitter can never skip a whole day.
        /// </summary>
        private const double MinimumHoursBetweenChecks = 20.0;

        public static bool ShouldRun()
        {
            string lastCheckUtcText = Preferences.Get(Constants.Keys.RssLastCheckUtc, string.Empty);
            if (!string.IsNullOrWhiteSpace(lastCheckUtcText) &&
                DateTime.TryParse(lastCheckUtcText, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var lastCheckUtc))
            {
                if ((DateTime.UtcNow - lastCheckUtc).TotalHours < MinimumHoursBetweenChecks)
                {
                    return false;
                }
            }

            return true;
        }

        public static void MarkRan()
        {
            Preferences.Set(Constants.Keys.RssLastCheckUtc,
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        }
    }
}
