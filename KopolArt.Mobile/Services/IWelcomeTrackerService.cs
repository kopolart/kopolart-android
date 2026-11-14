using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public interface IWelcomeTrackerService
    {
        /// <summary>
        /// Retrieves the welcome information for the current session (calculated on startup).
        /// </summary>
        WelcomeInfo GetWelcomeInfo();

        /// <summary>
        /// Gets or sets whether the welcome greeting has already been shown on the loading screen for this session.
        /// </summary>
        bool HasShownWelcomeOnLoading { get; set; }

        /// <summary>
        /// Records the current timestamp as the last visit in preferences.
        /// Should be called on app sleep, background, or termination.
        /// </summary>
        void RecordVisit();

        /// <summary>
        /// Gets the total cumulative usage duration (prior saved + current session).
        /// </summary>
        TimeSpan GetCurrentUsageDuration();

        /// <summary>
        /// Formats the current usage duration in Persian with live seconds.
        /// </summary>
        string GetFormattedUsageTime();

        /// <summary>
        /// Persists total accumulated usage seconds into Preferences.
        /// </summary>
        void SaveSessionUsage();
    }
}
