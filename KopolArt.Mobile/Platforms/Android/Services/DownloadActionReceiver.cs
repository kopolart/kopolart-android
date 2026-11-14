using Android.App;
using Android.Content;

namespace KopolArt.Mobile.Platforms.Android.Services
{
    [BroadcastReceiver(Enabled = true, Exported = false)]
    [IntentFilter([ActionPause, ActionResume, ActionCancel])]
    public class DownloadActionReceiver : BroadcastReceiver
    {
        public const string ActionPause = "ir.kopolart.app.ACTION_DOWNLOAD_PAUSE";
        public const string ActionResume = "ir.kopolart.app.ACTION_DOWNLOAD_RESUME";
        public const string ActionCancel = "ir.kopolart.app.ACTION_DOWNLOAD_CANCEL";
        public const string ExtraDownloadId = "extra_download_id";

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent == null) return;
            int id = intent.GetIntExtra(ExtraDownloadId, -1);
            if (id <= 0) return;

            if (intent.Action == ActionPause)
            {
                ActiveDownloadTracker.Pause(id);
            }
            else if (intent.Action == ActionResume)
            {
                ActiveDownloadTracker.Resume(id);
            }
            else if (intent.Action == ActionCancel)
            {
                ActiveDownloadTracker.Cancel(id);
            }
        }
    }
}
