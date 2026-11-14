using System.Text;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Helpers
{
    public static class PersianFormatHelper
    {
        private static readonly char[] PersianDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];

        /// <summary>
        /// Converts ASCII digits (0-9) to Persian digits (۰-۹).
        /// </summary>
        public static string ToPersianDigits(this string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var sb = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                if (ch >= '0' && ch <= '9')
                {
                    sb.Append(PersianDigits[ch - '0']);
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts integer to Persian string representation.
        /// </summary>
        public static string ToPersianDigits(this int number) => number.ToString().ToPersianDigits();

        /// <summary>
        /// Converts long to Persian string representation.
        /// </summary>
        public static string ToPersianDigits(this long number) => number.ToString().ToPersianDigits();

        /// <summary>
        /// Formats elapsed time since last visit in Persian according to:
        /// - Less than 1 minute: "-- ثانیه پیش" or "چند لحظه پیش"
        /// - Less than 1 hour: "-- دقیقه و -- ثانیه پیش"
        /// - 1 hour to 24 hours: "-- ساعت و -- دقیقه پیش"
        /// - More than 24 hours: "-- روز و -- ساعت پیش" (or "-- روز پیش")
        /// </summary>
        public static string FormatTimeSinceLastVisit(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
            {
                elapsed = TimeSpan.Zero;
            }

            var totalDays = (int)elapsed.TotalDays;
            var totalHours = (int)elapsed.TotalHours;
            var totalMinutes = (int)elapsed.TotalMinutes;

            // More than 24 hours -> Show days ("روز")
            if (totalDays >= 1)
            {
                var remainingHours = elapsed.Hours;
                if (remainingHours > 0)
                {
                    return $"{totalDays.ToPersianDigits()} روز و {remainingHours.ToPersianDigits()} ساعت پیش";
                }
                return $"{totalDays.ToPersianDigits()} روز پیش";
            }

            // More than 1 hour -> Show hours ("ساعت")
            if (totalHours >= 1)
            {
                var remainingMinutes = elapsed.Minutes;
                if (remainingMinutes > 0)
                {
                    return $"{totalHours.ToPersianDigits()} ساعت و {remainingMinutes.ToPersianDigits()} دقیقه پیش";
                }
                return $"{totalHours.ToPersianDigits()} ساعت پیش";
            }

            // Between 1 minute and 59 minutes -> Show "-- دقیقه و -- ثانیه"
            if (totalMinutes >= 1)
            {
                var remainingSeconds = elapsed.Seconds;
                if (remainingSeconds > 0)
                {
                    return $"{totalMinutes.ToPersianDigits()} دقیقه و {remainingSeconds.ToPersianDigits()} ثانیه پیش";
                }
                return $"{totalMinutes.ToPersianDigits()} دقیقه پیش";
            }

            // Less than 1 minute
            var seconds = elapsed.Seconds;
            if (seconds > 5)
            {
                return $"{seconds.ToPersianDigits()} ثانیه پیش";
            }

            return "چند لحظه پیش";
        }

        /// <summary>
        /// Formats total app usage time in Persian according to:
        /// - Under 1 minute: "-- ثانیه"
        /// - Under 1 hour: "-- دقیقه و -- ثانیه" (or "-- دقیقه")
        /// - 1 hour or more (under 1 day): "-- ساعت و -- دقیقه" (or "-- ساعت")
        /// - 1 day or more with remaining hour at least 1: "-- روز و -- ساعت"
        /// - 1 day or more with remaining hour under 1: "-- روز و -- دقیقه" (or "-- روز")
        /// </summary>
        public static string FormatUsageTime(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
            {
                duration = TimeSpan.Zero;
            }

            var totalDays = (int)duration.TotalDays;
            var totalHours = (int)duration.TotalHours;
            var totalMinutes = (int)duration.TotalMinutes;

            var seconds = duration.Seconds;
            var minutes = duration.Minutes;
            var hours = duration.Hours;

            // 1 day or more
            if (totalDays >= 1)
            {
                if (hours >= 1)
                {
                    return $"{totalDays.ToPersianDigits()} روز و {hours.ToPersianDigits()} ساعت";
                }

                // hour < 1
                if (minutes > 0)
                {
                    return $"{totalDays.ToPersianDigits()} روز و {minutes.ToPersianDigits()} دقیقه";
                }
                return $"{totalDays.ToPersianDigits()} روز";
            }

            // 1 hour or more -> Show hours and minutes only (no seconds)
            if (totalHours >= 1)
            {
                if (minutes > 0)
                {
                    return $"{totalHours.ToPersianDigits()} ساعت و {minutes.ToPersianDigits()} دقیقه";
                }
                return $"{totalHours.ToPersianDigits()} ساعت";
            }

            // Under 1 hour -> Show minutes and seconds
            if (totalMinutes >= 1)
            {
                if (seconds > 0)
                {
                    return $"{totalMinutes.ToPersianDigits()} دقیقه و {seconds.ToPersianDigits()} ثانیه";
                }
                return $"{totalMinutes.ToPersianDigits()} دقیقه";
            }

            // Under 1 minute -> Show seconds
            return $"{seconds.ToPersianDigits()} ثانیه";
        }

        /// <summary>
        /// Returns Persian time-of-day greeting based on hour (0-23):
        /// - 00:00 to 04:59 -> شبت بخیر
        /// - 05:00 to 11:59 -> صبحت بخیر
        /// - 12:00 to 15:59 -> ظهرت بخیر
        /// - 16:00 to 20:59 -> عصرت بخیر
        /// - 21:00 to 23:59 -> شبت بخیر
        /// </summary>
        public static string GetTimeOfDayGreeting(int hour)
        {
            if (hour < 5 || hour >= 20)
                return "شبت بخیر";
            if (hour < 12)
                return "صبحت بخیر";
            if (hour < 16)
                return "ظهرت بخیر";
            return "عصرت بخیر";
        }

        /// <summary>
        /// Returns Persian time-of-day greeting based on the current local device time.
        /// </summary>
        public static string GetTimeOfDayGreeting() => GetTimeOfDayGreeting(DateTime.Now.Hour);

        /// <summary>
        /// Formats raw byte count into human-readable Persian representation with units
        /// (بایت, کیلوبایت, مگابایت, گیگابایت).
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            if (bytes < 0) bytes = 0;

            if (bytes < 1024)
            {
                return $"{bytes.ToPersianDigits()} بایت";
            }

            double kb = bytes / 1024.0;
            if (kb < 1024)
            {
                return $"{kb:F1}".ToPersianDigits() + " کیلوبایت";
            }

            double mb = kb / 1024.0;
            if (mb < 1024)
            {
                return $"{mb:F1}".ToPersianDigits() + " مگابایت";
            }

            double gb = mb / 1024.0;
            return $"{gb:F2}".ToPersianDigits() + " گیگابایت";
        }

        /// <summary>
        /// Converts a UTC DateTime to a Persian Solar Hijri (Shamsi) formatted string with time.
        /// Example: "۱۴۰۳/۰۶/۱۷ - ۱۱:۳۰"
        /// </summary>
        public static string FormatPersianDate(DateTime utcDateTime)
        {
            try
            {
                var local = utcDateTime.ToLocalTime();
                var pc = new System.Globalization.PersianCalendar();
                int year = pc.GetYear(local);
                int month = pc.GetMonth(local);
                int day = pc.GetDayOfMonth(local);
                int hour = local.Hour;
                int minute = local.Minute;

                return $"{year:0000}/{month:00}/{day:00} - {hour:00}:{minute:00}".ToPersianDigits();
            }
            catch
            {
                return utcDateTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm").ToPersianDigits();
            }
        }

        /// <summary>
        /// Formats relative time from UTC date in Persian (e.g. "همین الان", "۱۰ دقیقه پیش", "۲ ساعت پیش", "دیروز").
        /// </summary>
        public static string FormatRelativeDate(DateTime utcDateTime)
        {
            var elapsed = DateTime.UtcNow - utcDateTime;
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

            if (elapsed.TotalMinutes < 1)
                return "همین الان";
            if (elapsed.TotalMinutes < 60)
                return $"{(int)elapsed.TotalMinutes} دقیقه پیش".ToPersianDigits();
            if (elapsed.TotalHours < 24)
                return $"{(int)elapsed.TotalHours} ساعت پیش".ToPersianDigits();
            if (elapsed.TotalDays < 2)
                return "دیروز";
            if (elapsed.TotalDays < 7)
                return $"{(int)elapsed.TotalDays} روز پیش".ToPersianDigits();

            return FormatPersianDate(utcDateTime);
        }

        private static readonly string[] PersianMonthNames =
        [
            "فروردین", "اردیبهشت", "خرداد",
            "تیر", "مرداد", "شهریور",
            "مهر", "آبان", "آذر",
            "دی", "بهمن", "اسفند"
        ];

        private static readonly string[] ShortPersianMonthNames =
        [
            "فرو", "ارد", "خرد",
            "تیر", "مرد", "شهر",
            "مهر", "آبا", "آذر",
            "دی", "بهم", "اسف"
        ];

        /// <summary>
        /// Returns the Persian month name for a Solar Hijri month (1-12).
        /// </summary>
        public static string GetPersianMonthName(int month)
            => month >= 1 && month <= 12 ? PersianMonthNames[month - 1] : string.Empty;

        /// <summary>
        /// Returns the abbreviated Persian month name for a Solar Hijri month (1-12).
        /// </summary>
        public static string GetShortPersianMonthName(int month)
            => month >= 1 && month <= 12 ? ShortPersianMonthNames[month - 1] : string.Empty;

        /// <summary>
        /// Converts a date string (e.g. "09/26/2026", "2026-09-26", "2026/09/26") or DateTime
        /// into Persian Solar Hijri (Shamsi) date with Persian month name (e.g. "۵ مهر ۱۴۰۵").
        /// </summary>
        public static string ToPersianDateString(string? rawDate, bool includeMonthName = true)
        {
            if (string.IsNullOrWhiteSpace(rawDate))
                return string.Empty;

            string[] formats = ["MM/dd/yyyy", "M/d/yyyy", "yyyy/MM/dd", "yyyy-MM-dd", "yyyy.MM.dd", "O", "s"];
            if (DateTime.TryParseExact(rawDate.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt) ||
                DateTime.TryParse(rawDate.Trim(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt))
            {
                return ToPersianDateString(dt, includeMonthName);
            }

            return rawDate.ToPersianDigits();
        }

        /// <summary>
        /// Converts a DateTime into Persian Solar Hijri (Shamsi) date with Persian month name (e.g. "۵ مهر ۱۴۰۵").
        /// </summary>
        public static string ToPersianDateString(DateTime date, bool includeMonthName = true)
        {
            try
            {
                var pc = new System.Globalization.PersianCalendar();
                int year = pc.GetYear(date);
                int month = pc.GetMonth(date);
                int day = pc.GetDayOfMonth(date);

                if (includeMonthName && month >= 1 && month <= 12)
                {
                    return $"{day.ToPersianDigits()} {GetPersianMonthName(month)} {year.ToPersianDigits()}";
                }

                return $"{year:0000}/{month:00}/{day:00}".ToPersianDigits();
            }
            catch
            {
                return date.ToString("yyyy/MM/dd").ToPersianDigits();
            }
        }

        /// <summary>
        /// Maps standard DayOfWeek to Persian weekday name.
        /// </summary>
        public static string GetPersianDayOfWeekName(DayOfWeek dow) => dow switch
        {
            DayOfWeek.Saturday => "شنبه",
            DayOfWeek.Sunday => "یکشنبه",
            DayOfWeek.Monday => "دوشنبه",
            DayOfWeek.Tuesday => "سه‌شنبه",
            DayOfWeek.Wednesday => "چهارشنبه",
            DayOfWeek.Thursday => "پنجشنبه",
            DayOfWeek.Friday => "جمعه",
            _ => string.Empty
        };

        /// <summary>
        /// Maps standard DayOfWeek to its single-letter Persian abbreviation.
        /// </summary>
        public static string GetShortPersianDayOfWeekName(DayOfWeek dow) => dow switch
        {
            DayOfWeek.Saturday => "ش",
            DayOfWeek.Sunday => "ی",
            DayOfWeek.Monday => "د",
            DayOfWeek.Tuesday => "س",
            DayOfWeek.Wednesday => "چ",
            DayOfWeek.Thursday => "پ",
            DayOfWeek.Friday => "ج",
            _ => string.Empty
        };

        /// <summary>
        /// Returns today's Persian date in format: "امروز {روز هفته} {روز} {ماه}"
        /// Example: "امروز دوشنبه ۲۲ فروردین"
        /// </summary>
        public static string GetPersianTodayDateString(DateTime? localDate = null)
        {
            try
            {
                var dt = localDate ?? DateTime.Now;
                var pc = new System.Globalization.PersianCalendar();
                int month = pc.GetMonth(dt);
                int day = pc.GetDayOfMonth(dt);
                string dayOfWeek = GetPersianDayOfWeekName(dt.DayOfWeek);
                string monthName = GetPersianMonthName(month);

                return $"امروز {dayOfWeek} {day.ToPersianDigits()} {monthName}".Trim();
            }
            catch
            {
                return "امروز";
            }
        }

        /// <summary>
        /// Determines the date group period for a given UTC timestamp based on device local date.
        /// </summary>
        public static DateGroupPeriod GetDateGroupPeriod(DateTime utcDateTime)
        {
            var localDate = utcDateTime.ToLocalTime().Date;
            var today = DateTime.Today;

            if (localDate >= today)
                return DateGroupPeriod.Today;
            if (localDate == today.AddDays(-1))
                return DateGroupPeriod.Yesterday;
            if (localDate >= today.AddDays(-7))
                return DateGroupPeriod.LastWeek;

            return DateGroupPeriod.Older;
        }

        /// <summary>
        /// Returns the Persian title and icon glyph for the given date group period.
        /// </summary>
        public static (string Title, string Icon) GetDateGroupInfo(DateGroupPeriod period) => period switch
        {
            DateGroupPeriod.Today => ("امروز", KopolArtFaIcons.Calendar),
            DateGroupPeriod.Yesterday => ("دیروز", KopolArtFaIcons.Clock),
            DateGroupPeriod.LastWeek => ("هفته گذشته", KopolArtFaIcons.Calendar),
            _ => ("قدیمی‌تر", KopolArtFaIcons.Hourglass)
        };
    }
}

