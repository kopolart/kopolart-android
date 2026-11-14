namespace KopolArt.Mobile.Services
{
    public interface IBackupService
    {
        Task<bool> ExportBackupAsync();
        Task<bool> ImportBackupAsync();
    }
}
