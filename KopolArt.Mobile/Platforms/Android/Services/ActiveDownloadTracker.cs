using System.Collections.Concurrent;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Platforms.Android.Services
{
    public static class ActiveDownloadTracker
    {
        private static readonly ConcurrentDictionary<int, ActiveDownloadSession> Sessions = new();

        public static void Register(ActiveDownloadSession session) => Sessions[session.Id] = session;
        public static void Unregister(int id) => Sessions.TryRemove(id, out _);
        public static ActiveDownloadSession? Get(int id) => Sessions.TryGetValue(id, out var s) ? s : null;

        public static void Pause(int id)
        {
            if (Sessions.TryGetValue(id, out var session))
            {
                session.Pause();
            }
        }

        public static void Resume(int id)
        {
            if (Sessions.TryGetValue(id, out var session))
            {
                session.Resume();
            }
        }

        public static void Cancel(int id)
        {
            if (Sessions.TryGetValue(id, out var session))
            {
                session.Cancel();
            }
        }
    }
}
