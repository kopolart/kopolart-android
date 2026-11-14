using System.Text.Json;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public class UsageMilestoneService : IUsageMilestoneService
    {
        private readonly ICustomAlertService _customAlertService;
        private readonly SemaphoreSlim _evalLock = new(1, 1);
        private readonly HashSet<string> _awardedMilestones = [];
        private bool _isLoaded;

        public UsageMilestoneService(ICustomAlertService customAlertService)
        {
            _customAlertService = customAlertService;
            LoadAwardedMilestones();
        }

        private void LoadAwardedMilestones()
        {
            if (_isLoaded) return;

            try
            {
                var json = Preferences.Get(Constants.Keys.AppAwardedMilestones, string.Empty);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var list = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListString);
                    if (list != null)
                    {
                        foreach (var id in list)
                        {
                            _awardedMilestones.Add(id);
                        }
                    }
                }
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UsageMilestoneService] Load error: {ex.Message}");
                _isLoaded = true;
            }
        }

        private void SaveAwardedMilestones()
        {
            try
            {
                var list = _awardedMilestones.ToList();
                var json = JsonSerializer.Serialize(list, AppJsonContext.Default.ListString);
                Preferences.Set(Constants.Keys.AppAwardedMilestones, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UsageMilestoneService] Save error: {ex.Message}");
            }
        }

        public List<string> GetAwardedMilestones()
        {
            lock (_awardedMilestones)
            {
                return _awardedMilestones.ToList();
            }
        }

        public void SetAwardedMilestones(IEnumerable<string> milestoneIds)
        {
            lock (_awardedMilestones)
            {
                _awardedMilestones.Clear();
                foreach (var id in milestoneIds)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        _awardedMilestones.Add(id.Trim());
                    }
                }
                SaveAwardedMilestones();
            }
        }

        public void ResetMilestones()
        {
            lock (_awardedMilestones)
            {
                _awardedMilestones.Clear();
                Preferences.Remove(Constants.Keys.AppAwardedMilestones);
            }
        }

        public async Task CheckMilestonesAsync(long totalUsageSeconds)
        {
            if (totalUsageSeconds < 600) return; // Minimum milestone is 10 minutes

            if (!await _evalLock.WaitAsync(0))
            {
                return; // Evaluation currently in progress, skip this tick
            }

            try
            {
                LoadAwardedMilestones();

                var candidates = GetEligibleMilestones(totalUsageSeconds);
                var unawarded = new List<MilestoneDefinition>();

                lock (_awardedMilestones)
                {
                    foreach (var m in candidates)
                    {
                        if (!_awardedMilestones.Contains(m.Id))
                        {
                            unawarded.Add(m);
                        }
                    }

                    if (unawarded.Count == 0) return;

                    // Mark all newly reached milestones as awarded so we never trigger them again
                    foreach (var m in unawarded)
                    {
                        _awardedMilestones.Add(m.Id);
                    }
                    SaveAwardedMilestones();
                }

                // Celebrate the highest newly reached milestone
                var highest = unawarded.OrderByDescending(x => x.ThresholdSeconds).First();

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        await _customAlertService.ShowAlertAsync(
                            highest.Title,
                            highest.Message,
                            "سپاسگزارم",
                            AlertType.Success);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UsageMilestoneService] Alert error: {ex.Message}");
                    }
                });
            }
            finally
            {
                _evalLock.Release();
            }
        }

        private static List<MilestoneDefinition> GetEligibleMilestones(long totalUsageSeconds)
        {
            var list = new List<MilestoneDefinition>
            {
                new(
                    "10m",
                    600,
                    "همراهی ۱۰ دقیقه‌ای شما 🎉",
                    "از اینکه ۱۰ دقیقه از زمان ارزشمند خود را با کپل‌آرت گذراندید سپاسگزارم! امیدوارم تجربه‌ای لذت‌بخش و مفید برای شما باشد."),

                new(
                    "30m",
                    1800,
                    "نیم ساعت همراهی ارزشمند 🌟",
                    "تبریک! شما ۳۰ دقیقه همراه کپل‌آرت بوده‌اید. حضور گرم و نگاه پرمهر شما مایه افتخار و دلگرمی من است."),

                new(
                    "1h",
                    3600,
                    "۱ ساعت همراهی درخشان 💎",
                    "تبریک! شما به رکورد ۱ ساعت استفاده از برنامه رسیدید. صمیمانه از اعتماد و همراهی پرارزش شما سپاسگزارم."),

                new(
                    "5h",
                    18000,
                    "۵ ساعت وفاداری و همراهی 🚀",
                    "فوق‌العاده است! شما بیش از ۵ ساعت در کپل‌آرت به گشت‌وگذار و خلق ایده‌ها پرداخته‌اید. از این همراهی پرشور و وفادارانه قدردانی می‌کنم."),

                new(
                    "10h",
                    36000,
                    "۱۰ ساعت همراهی طلایی 👑",
                    "شما یکی از همراهان ویژه و وفادار کپل‌آرت هستید. ثبت رکورد ۱۰ ساعت حضور شما در برنامه، افتخاری بزرگ و ماندگار برای من است!"),

                new(
                    "1d",
                    86400,
                    "رکورد ۲۴ ساعت همراهی بی‌نظیر 🏆",
                    "تبریک صمیمانه! شما یک شبانه‌روز کامل (۲۴ ساعت) را در دنیای کپل‌آرت سپری کرده‌اید. این سطح از همراهی استثنایی و ستودنی است!")
            };

            // Dynamic daily milestones for 2 days and beyond (48h, 72h, 96h, ...)
            var days = (int)(totalUsageSeconds / 86400);
            for (int d = 2; d <= days; d++)
            {
                var dayDigits = PersianFormatHelper.ToPersianDigits(d.ToString());
                list.Add(new MilestoneDefinition(
                    $"{d}d",
                    (long)d * 86400,
                    $"رکورد {dayDigits} روز همراهی استثنایی 🎖️",
                    $"شگفت‌انگیز است! شما به رکورد {dayDigits} روز استفاده کامل از کپل‌آرت رسیدید. با تمام وجود از این همراهی بی‌نظیر و پایدار قدردانی می‌کنم."));
            }

            return list.Where(x => x.ThresholdSeconds <= totalUsageSeconds).ToList();
        }
    }
}
