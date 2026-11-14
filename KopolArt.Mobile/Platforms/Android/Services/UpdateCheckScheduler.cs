using Android.App.Job;
using Android.Content;

namespace KopolArt.Mobile.Platforms.Android
{
    public static class UpdateCheckScheduler
    {
        public const int JobId = 2001;

        /// <summary>
        /// The background job runs daily. The user-visible notification cadence is NOT tied to this
        /// value: new-post notifications stay capped at one per 72 hours (see RssFeedService).
        /// </summary>
        public const int DefaultIntervalHours = 24;

        /// <summary>
        /// Schedules a periodic background check using Android JobScheduler every intervalHours (default 24).
        /// Requires internet connection and persists across device reboots.
        /// </summary>
        public static void SchedulePeriodicCheck(Context context, int intervalHours = DefaultIntervalHours)
        {
            try
            {
                var jobScheduler = (JobScheduler?)context.GetSystemService(Context.JobSchedulerService);
                if (jobScheduler == null)
                {
                    System.Diagnostics.Debug.WriteLine("[UpdateCheckScheduler] JobScheduler service not available.");
                    return;
                }

                // Check if already scheduled
                var pendingJobs = jobScheduler.AllPendingJobs;
                if (pendingJobs != null)
                {
                    foreach (var job in pendingJobs)
                    {
                        if (job.Id == JobId)
                        {
                            System.Diagnostics.Debug.WriteLine($"[UpdateCheckScheduler] Job {JobId} is already scheduled.");
                            return;
                        }
                    }
                }

                var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(UpdateCheckJobService)));
                long intervalMillis = (long)TimeSpan.FromHours(intervalHours).TotalMilliseconds;

#pragma warning disable CS8602
                var builder = new JobInfo.Builder(JobId, componentName)
                    .SetPeriodic(intervalMillis)
                    .SetRequiredNetworkType(NetworkType.Any)
                    .SetPersisted(true);

                var jobInfo = builder?.Build();
#pragma warning restore CS8602
                if (jobInfo != null)
                {
                    var result = jobScheduler.Schedule(jobInfo);
                    System.Diagnostics.Debug.WriteLine($"[UpdateCheckScheduler] Job {JobId} scheduled every {intervalHours}h. Result: {result}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateCheckScheduler] Failed to schedule periodic update check: {ex}");
            }
        }

        /// <summary>
        /// Cancels the periodic background check.
        /// </summary>
        public static void CancelPeriodicCheck(Context context)
        {
            try
            {
                var jobScheduler = (JobScheduler?)context.GetSystemService(Context.JobSchedulerService);
                jobScheduler?.Cancel(JobId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateCheckScheduler] Failed to cancel job {JobId}: {ex}");
            }
        }
    }

    [BroadcastReceiver(
        Name = "ir.kopolart.app.receivers.BootReceiver",
        Enabled = true,
        Exported = true)]
    [global::Android.App.IntentFilter([Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced])]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null) return;

            System.Diagnostics.Debug.WriteLine($"[BootReceiver] Received intent: {intent?.Action}");
            UpdateCheckScheduler.SchedulePeriodicCheck(context, UpdateCheckScheduler.DefaultIntervalHours);
        }
    }
}
