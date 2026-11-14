using System.Globalization;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public class UsageAnalyticsService : IUsageAnalyticsService
    {
        private const double MaxBarHeightDp = 120.0;
        private const double MinBarHeightDp = 6.0;

        private readonly IUsageTrackerService _usageTrackerService;

        public UsageAnalyticsService(IUsageTrackerService usageTrackerService)
        {
            _usageTrackerService = usageTrackerService;
        }

        public async Task<UsageAnalyticsSummary> BuildAnalyticsAsync(int daysCount)
        {
            var sessions = await _usageTrackerService.GetUsageSessionsAsync();
            var today = DateTime.Today;
            var persianCal = new PersianCalendar();

            var summary = new UsageAnalyticsSummary();

            if (sessions == null || sessions.Count == 0)
            {
                summary.IsEmpty = true;
                summary.SmartInsight = "هنوز سابقه وب‌گردی در این بازه زمانی ثبت نشده است.";
                return summary;
            }

            // Aggregate sessions into day/month buckets
            if (daysCount >= 365)
            {
                return BuildYearlyMonthlyAnalytics(sessions, today, persianCal);
            }

            return BuildDailyAnalytics(sessions, daysCount, today, persianCal);
        }

        private UsageAnalyticsSummary BuildDailyAnalytics(
            List<UsageRecord> sessions,
            int daysCount,
            DateTime today,
            PersianCalendar persianCal)
        {
            var summary = new UsageAnalyticsSummary();
            var bars = new List<UsageAnalyticsBarItem>();

            // Map sessions by local date key: yyyy-MM-dd
            var dayBuckets = new Dictionary<string, (long Seconds, long Bytes)>();
            foreach (var session in sessions)
            {
                var localDate = session.StartTimeUtc.ToLocalTime().Date;
                var key = localDate.ToString("yyyy-MM-dd");

                if (!dayBuckets.TryGetValue(key, out var current))
                {
                    current = (0L, 0L);
                }

                dayBuckets[key] = (current.Seconds + session.DurationSeconds, current.Bytes + session.DataBytesUsed);
            }

            long maxSeconds = 0;
            long totalSeconds = 0;
            long totalBytes = 0;
            int activeDaysCount = 0;

            for (int i = daysCount - 1; i >= 0; i--)
            {
                var dt = today.AddDays(-i);
                var key = dt.ToString("yyyy-MM-dd");

                dayBuckets.TryGetValue(key, out var bucket);

                if (bucket.Seconds > maxSeconds)
                {
                    maxSeconds = bucket.Seconds;
                }

                totalSeconds += bucket.Seconds;
                totalBytes += bucket.Bytes;

                if (bucket.Seconds > 0)
                {
                    activeDaysCount++;
                }

                string dayName = GetPersianDayOfWeekName(dt.DayOfWeek);
                string shortDayName = GetShortPersianDayOfWeekName(dt.DayOfWeek);
                int pMonth = persianCal.GetMonth(dt);
                int pDay = persianCal.GetDayOfMonth(dt);
                string dayNumber = PersianFormatHelper.ToPersianDigits(pDay);
                string monthName = GetPersianMonthName(pMonth);
                string pYear = PersianFormatHelper.ToPersianDigits(persianCal.GetYear(dt));
                string fullDateStr = $"{dayName}، {dayNumber} {monthName} {pYear}";

                bars.Add(new UsageAnalyticsBarItem
                {
                    Date = dt,
                    DateKey = key,
                    DurationSeconds = bucket.Seconds,
                    DataBytesUsed = bucket.Bytes,
                    DayName = dayName,
                    ShortDayName = daysCount <= 7 ? shortDayName : dayNumber,
                    DayNumber = dayNumber,
                    FullDateString = fullDateStr,
                    FormattedDuration = PersianFormatHelper.FormatUsageTime(TimeSpan.FromSeconds(bucket.Seconds)),
                    FormattedData = PersianFormatHelper.FormatBytes(bucket.Bytes),
                    IsToday = (dt == today)
                });
            }

            // Find peak bar and calculate scaled heights
            UsageAnalyticsBarItem? peakBar = null;
            if (maxSeconds > 0)
            {
                peakBar = bars.Where(b => b.DurationSeconds == maxSeconds).FirstOrDefault();
                if (peakBar != null)
                {
                    peakBar.IsPeak = true;
                }
            }

            foreach (var bar in bars)
            {
                if (maxSeconds > 0 && bar.DurationSeconds > 0)
                {
                    bar.BarHeightDp = Math.Max(MinBarHeightDp, (bar.DurationSeconds / (double)maxSeconds) * MaxBarHeightDp);
                }
                else
                {
                    bar.BarHeightDp = 4.0; // Subtle baseline dot for empty days
                }
            }

            // Select the most recent active bar by default, or today
            var defaultSelect = bars.LastOrDefault(b => b.HasUsage) ?? bars.LastOrDefault();
            if (defaultSelect != null)
            {
                defaultSelect.IsSelected = true;
            }

            summary.Bars = bars;
            summary.PeakBar = peakBar;
            summary.TotalDurationSeconds = totalSeconds;
            summary.FormattedTotalDuration = PersianFormatHelper.FormatUsageTime(TimeSpan.FromSeconds(totalSeconds));
            summary.AverageDurationSeconds = daysCount > 0 ? totalSeconds / daysCount : 0;
            summary.FormattedAverageDuration = PersianFormatHelper.FormatUsageTime(TimeSpan.FromSeconds(summary.AverageDurationSeconds));
            summary.TotalDataBytes = totalBytes;
            summary.FormattedTotalData = PersianFormatHelper.FormatBytes(totalBytes);
            summary.ActiveDaysCount = activeDaysCount;
            summary.IsEmpty = totalSeconds == 0;
            summary.SmartInsight = GenerateSmartInsight(summary, daysCount);

            return summary;
        }

        private UsageAnalyticsSummary BuildYearlyMonthlyAnalytics(
            List<UsageRecord> sessions,
            DateTime today,
            PersianCalendar persianCal)
        {
            var summary = new UsageAnalyticsSummary();
            var bars = new List<UsageAnalyticsBarItem>();

            int curPYear = persianCal.GetYear(today);
            int curPMonth = persianCal.GetMonth(today);

            var monthSums = new Dictionary<(int Year, int Month), (long Seconds, long Bytes)>();
            var monthMeta = new List<(int Year, int Month)>();

            for (int m = 0; m < 12; m++)
            {
                int offset = 11 - m;
                int totalMonthIndex = (curPYear * 12 + (curPMonth - 1)) - offset;
                int targetYear = totalMonthIndex / 12;
                int targetMonth = (totalMonthIndex % 12) + 1;

                monthSums[(targetYear, targetMonth)] = (0L, 0L);
                monthMeta.Add((targetYear, targetMonth));
            }

            foreach (var session in sessions)
            {
                var localDate = session.StartTimeUtc.ToLocalTime();
                try
                {
                    int py = persianCal.GetYear(localDate);
                    int pm = persianCal.GetMonth(localDate);
                    if (monthSums.TryGetValue((py, pm), out var cur))
                    {
                        monthSums[(py, pm)] = (cur.Seconds + session.DurationSeconds, cur.Bytes + session.DataBytesUsed);
                    }
                }
                catch
                {
                    // Ignore date conversion anomalies
                }
            }

            long maxSeconds = 0;
            long totalSeconds = 0;
            long totalBytes = 0;
            int activeMonthsCount = 0;

            foreach (var (year, month) in monthMeta)
            {
                var (sec, bytes) = monthSums[(year, month)];
                if (sec > maxSeconds) maxSeconds = sec;
                totalSeconds += sec;
                totalBytes += bytes;
                if (sec > 0) activeMonthsCount++;

                string monthName = GetPersianMonthName(month);
                string shortMonthName = GetShortPersianMonthName(month);
                string yearStr = PersianFormatHelper.ToPersianDigits(year);
                bool isCurrent = (year == curPYear && month == curPMonth);

                bars.Add(new UsageAnalyticsBarItem
                {
                    DateKey = $"{year:D4}-{month:D2}",
                    DurationSeconds = sec,
                    DataBytesUsed = bytes,
                    DayName = monthName,
                    ShortDayName = shortMonthName,
                    DayNumber = yearStr,
                    FullDateString = $"{monthName} {yearStr}",
                    FormattedDuration = PersianFormatHelper.FormatUsageTime(TimeSpan.FromSeconds(sec)),
                    FormattedData = PersianFormatHelper.FormatBytes(bytes),
                    IsToday = isCurrent
                });
            }

            UsageAnalyticsBarItem? peakBar = null;
            if (maxSeconds > 0)
            {
                peakBar = bars.Where(b => b.DurationSeconds == maxSeconds).FirstOrDefault();
                if (peakBar != null)
                {
                    peakBar.IsPeak = true;
                }
            }

            foreach (var bar in bars)
            {
                if (maxSeconds > 0 && bar.DurationSeconds > 0)
                {
                    bar.BarHeightDp = Math.Max(MinBarHeightDp, (bar.DurationSeconds / (double)maxSeconds) * MaxBarHeightDp);
                }
                else
                {
                    bar.BarHeightDp = 4.0;
                }
            }

            var defaultSelect = bars.LastOrDefault(b => b.HasUsage) ?? bars.LastOrDefault();
            if (defaultSelect != null)
            {
                defaultSelect.IsSelected = true;
            }

            summary.Bars = bars;
            summary.PeakBar = peakBar;
            summary.TotalDurationSeconds = totalSeconds;
            summary.FormattedTotalDuration = PersianFormatHelper.FormatUsageTime(TimeSpan.FromSeconds(totalSeconds));
            summary.AverageDurationSeconds = totalSeconds / 12;
            summary.FormattedAverageDuration = PersianFormatHelper.FormatUsageTime(TimeSpan.FromSeconds(summary.AverageDurationSeconds));
            summary.TotalDataBytes = totalBytes;
            summary.FormattedTotalData = PersianFormatHelper.FormatBytes(totalBytes);
            summary.ActiveDaysCount = activeMonthsCount;
            summary.IsEmpty = totalSeconds == 0;
            summary.SmartInsight = GenerateSmartInsight(summary, 365);

            return summary;
        }

        private static string GenerateSmartInsight(UsageAnalyticsSummary summary, int daysCount)
        {
            if (summary.IsEmpty || summary.TotalDurationSeconds == 0)
            {
                return "هنوز سابقه استفاده‌ای در این بازه زمانی ثبت نشده است.";
            }

            if (summary.PeakBar != null && summary.PeakBar.DurationSeconds > 0)
            {
                if (daysCount >= 365)
                {
                    return $"بیشترین میزان وب‌گردی شما در ماه {summary.PeakBar.FullDateString} با {summary.PeakBar.FormattedDuration} بوده است.";
                }

                return $"اوج مصرف شما در {summary.PeakBar.FullDateString} با {summary.PeakBar.FormattedDuration} گردش ثبت شده است.";
            }

            return $"میانگین وب‌گردی روزانه شما در این بازه حدود {summary.FormattedAverageDuration} است.";
        }

        public static string GetPersianMonthName(int month) => PersianFormatHelper.GetPersianMonthName(month);

        public static string GetShortPersianMonthName(int month) => PersianFormatHelper.GetShortPersianMonthName(month);

        public static string GetPersianDayOfWeekName(DayOfWeek dow) => PersianFormatHelper.GetPersianDayOfWeekName(dow);

        public static string GetShortPersianDayOfWeekName(DayOfWeek dow) => PersianFormatHelper.GetShortPersianDayOfWeekName(dow);
    }
}
