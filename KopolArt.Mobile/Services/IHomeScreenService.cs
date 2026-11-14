namespace KopolArt.Mobile.Services
{
    public interface IHomeScreenService
    {
        bool IsPinWidgetSupported();
        Task<bool> RequestPinWidgetAsync();
        bool IsPinShortcutSupported();
        Task<bool> RequestPinShortcutAsync(string shortcutId, string label, string action);
        void NotifyWidgetUpdate();
    }
}
