namespace KopolArt.Mobile.Services
{
    public interface IAppInstaller
    {
        bool CanInstallDirectly();
        void RequestInstallPermission();
        void Install(string localFilePath);
    }
}
