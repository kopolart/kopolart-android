#define DEBUG
using System.Diagnostics;
using System.Text.Json;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services;

public class BackupService(ISettingsService settingsService, IBookmarkService bookmarkService, IHistoryService historyService, IDownloadHistoryService downloadHistoryService, IUsageTrackerService usageTrackerService, IUsageMilestoneService usageMilestoneService, ICustomAlertService customAlertService, IOfflinePageService? offlinePageService = null, ISearchHistoryService? searchHistoryService = null) : IBackupService
{
	public async Task<bool> ExportBackupAsync()
	{
		try
		{
			AppSettings settings = settingsService.Settings;
			List<BookmarkItem> bookmarks = await bookmarkService.GetBookmarksAsync();
			List<HistoryItem> history = await historyService.GetHistoryAsync();
			List<DownloadItem> downloads = await downloadHistoryService.GetDownloadsAsync();
			List<UsageRecord> sessions = await usageTrackerService.GetUsageSessionsAsync();
			long totalUsageSeconds = Preferences.Get("AppTotalUsageSeconds", 0L);
			List<OfflinePageItem> list = ((offlinePageService == null) ? null : (await offlinePageService.GetOfflinePagesAsync()));
			List<OfflinePageItem> offlinePages = list;
			List<string>? recentSearches = searchHistoryService != null ? await searchHistoryService.GetRecentSearchesAsync() : null;
			BackupDataModel backupModel = new BackupDataModel
			{
				SchemaVersion = 1,
				AppName = "KopolArt.Mobile",
				AppVersion = AppInfo.Current.VersionString,
				ExportedAtUtc = DateTime.UtcNow,
				Data = new BackupPayload
				{
					Settings = new AppSettings
					{
						ShowLoadingOnNavigation = settings.ShowLoadingOnNavigation,
						OpenDashboardAtStartup = settings.OpenDashboardAtStartup,
						RestoreLastPageAtStartup = settings.RestoreLastPageAtStartup,
						AutoClearCacheOnExit = settings.AutoClearCacheOnExit,
						AppThemeMode = settings.AppThemeMode,
						LastVisitedUrl = settings.LastVisitedUrl,
						LastRunAppVersion = settings.LastRunAppVersion,
						HasCompletedOnboarding = settings.HasCompletedOnboarding,
						CustomDownloadFolderUri = settings.CustomDownloadFolderUri,
						CustomDownloadFolderName = settings.CustomDownloadFolderName,
						DontShowDownloadPromptAgain = settings.DontShowDownloadPromptAgain,
						WidgetOpacity = settings.WidgetOpacity,
						NotifyNewContent = settings.NotifyNewContent
					},
					Bookmarks = bookmarks,
					History = history,
					Downloads = downloads,
					Usage = new UsageBackupData
					{
						TotalUsageSeconds = totalUsageSeconds,
						Sessions = sessions,
						AwardedMilestones = usageMilestoneService.GetAwardedMilestones()
					},
					OfflinePages = offlinePages,
					RecentSearches = recentSearches
				}
			};
			string json = JsonSerializer.Serialize(backupModel, AppJsonContext.Default.BackupDataModel);
			string tempPath = Path.Combine(path2: $"kopolart_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json", path1: FileSystem.CacheDirectory);
			await File.WriteAllTextAsync(tempPath, json);
			await Share.Default.RequestAsync(new ShareFileRequest
			{
				Title = "ذخیره فایل پشتیبان کپل\u200cآرت",
				File = new ShareFile(tempPath, "application/json")
			});
			return true;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("[BackupService] Export error: " + ex.Message);
			await customAlertService.ShowAlertAsync("خطا در پشتیبان\u200cگیری", "هنگام ایجاد فایل پشتیبان خطایی رخ داد. لطفا\u064b دوباره تلاش کنید.", "تأیید", AlertType.Error);
			return false;
		}
	}

	public async Task<bool> ImportBackupAsync()
	{
		try
		{
			FileResult? pickResult = await FilePicker.Default.PickAsync(new PickOptions
			{
				PickerTitle = "انتخاب فایل پشتیبان کپل\u200cآرت (JSON)"
			});
			if (pickResult == null)
			{
				return false;
			}
			string json;
			using (Stream stream = await ((FileBase)pickResult).OpenReadAsync())
			{
				using StreamReader reader = new StreamReader(stream);
				json = await reader.ReadToEndAsync();
			}
			BackupDataModel backup = null;
			try
			{
				backup = JsonSerializer.Deserialize(json, AppJsonContext.Default.BackupDataModel);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("[BackupService] Deserialize error: " + ex.Message);
			}
			if (backup == null || backup.AppName != "KopolArt.Mobile" || backup.Data == null)
			{
				await customAlertService.ShowAlertAsync("فایل نامعتبر", "این فایل معتبر نیست یا ساختار استاندارد پشتیبان کپل\u200cآرت را ندارد.", "متوجه شدم", AlertType.Error);
				return false;
			}
			string persianDate = PersianFormatHelper.FormatPersianDate(backup.ExportedAtUtc).ToPersianDigits();
			string bCount = (backup.Data.Bookmarks?.Count ?? 0).ToString().ToPersianDigits();
			string hCount = (backup.Data.History?.Count ?? 0).ToString().ToPersianDigits();
			string dCount = (backup.Data.Downloads?.Count ?? 0).ToString().ToPersianDigits();
			string sCount = (backup.Data.Usage?.Sessions?.Count).GetValueOrDefault().ToString().ToPersianDigits();
			string oCount = (backup.Data.OfflinePages?.Count ?? 0).ToString().ToPersianDigits();
			string rCount = (backup.Data.RecentSearches?.Count ?? 0).ToString().ToPersianDigits();
			string message = $"فایل پشتیبان با مشخصات زیر پیدا شد:\n\n• تاریخ پشتیبان: {persianDate}\n• نشان\u200cشده\u200cها: {bCount} مورد\n• تاریخچه صفحات: {hCount} مورد\n• سوابق دانلود: {dCount} مورد\n• جلسات و لاگ\u200cها: {sCount} جلسه\n• صفحات آفلاین: {oCount} مورد\n• جستجوهای اخیر: {rCount} مورد\n\nنحوه بازگردانی اطلاعات را انتخاب کنید:";
			string choice = await customAlertService.ShowChoiceAsync("بازیابی فایل پشتیبان", message, "ادغام هوشمند (Smart Merge)", "جایگزینی کامل (Overwrite)");
			if (string.IsNullOrEmpty(choice))
			{
				return false;
			}
			bool overwrite = choice == "secondary";
			if (backup.Data.Settings != null)
			{
				settingsService.Settings.ShowLoadingOnNavigation = backup.Data.Settings.ShowLoadingOnNavigation;
				settingsService.Settings.OpenDashboardAtStartup = backup.Data.Settings.OpenDashboardAtStartup;
				settingsService.Settings.RestoreLastPageAtStartup = backup.Data.Settings.RestoreLastPageAtStartup;
				settingsService.Settings.AutoClearCacheOnExit = backup.Data.Settings.AutoClearCacheOnExit;
				settingsService.Settings.AppThemeMode = backup.Data.Settings.AppThemeMode;
				settingsService.Settings.HasCompletedOnboarding = backup.Data.Settings.HasCompletedOnboarding;
				settingsService.Settings.CustomDownloadFolderUri = backup.Data.Settings.CustomDownloadFolderUri;
				settingsService.Settings.CustomDownloadFolderName = backup.Data.Settings.CustomDownloadFolderName;
				settingsService.Settings.DontShowDownloadPromptAgain = backup.Data.Settings.DontShowDownloadPromptAgain;
				settingsService.Settings.WidgetOpacity = backup.Data.Settings.WidgetOpacity;
				settingsService.Settings.NotifyNewContent = backup.Data.Settings.NotifyNewContent;
				if (!string.IsNullOrEmpty(backup.Data.Settings.LastVisitedUrl))
				{
					settingsService.Settings.LastVisitedUrl = backup.Data.Settings.LastVisitedUrl;
				}
				await settingsService.SaveSettingsAsync(settingsService.Settings);
				settingsService.ApplyTheme(settingsService.Settings.AppThemeMode);
			}
			if (backup.Data.Bookmarks != null)
			{
				await bookmarkService.ImportBookmarksAsync(backup.Data.Bookmarks, overwrite);
			}
			if (backup.Data.History != null)
			{
				await historyService.ImportHistoryAsync(backup.Data.History, overwrite);
			}
			if (backup.Data.Downloads != null)
			{
				await downloadHistoryService.ImportDownloadsAsync(backup.Data.Downloads, overwrite);
			}
			if (backup.Data.Usage != null)
			{
				await usageTrackerService.ImportSessionsAsync(backup.Data.Usage.Sessions ?? new List<UsageRecord>(), backup.Data.Usage.TotalUsageSeconds, overwrite);
				if (backup.Data.Usage.AwardedMilestones != null && backup.Data.Usage.AwardedMilestones.Count > 0)
				{
					if (overwrite)
					{
						usageMilestoneService.SetAwardedMilestones(backup.Data.Usage.AwardedMilestones);
					}
					else
					{
						IEnumerable<string> merged = usageMilestoneService.GetAwardedMilestones().Union(backup.Data.Usage.AwardedMilestones);
						usageMilestoneService.SetAwardedMilestones(merged);
					}
				}
			}
			if (backup.Data.OfflinePages != null && offlinePageService != null)
			{
				await offlinePageService.ImportOfflinePagesAsync(backup.Data.OfflinePages);
			}
			if (backup.Data.RecentSearches != null && searchHistoryService != null)
			{
				await searchHistoryService.ImportRecentSearchesAsync(backup.Data.RecentSearches, overwrite);
			}
			string modeText = (overwrite ? "جایگزینی کامل" : "ادغام هوشمند");
			await customAlertService.ShowAlertAsync("بازیابی موفقیت\u200cآمیز", "اطلاعات فایل پشتیبان با موفقیت به روش «" + modeText + "» بازیابی شد.", "بسیار عالی", AlertType.Success);
			return true;
		}
		catch (Exception ex2)
		{
			Debug.WriteLine("[BackupService] Import error: " + ex2.Message);
			await customAlertService.ShowAlertAsync("خطا در بازیابی", "هنگام بازیابی اطلاعات خطایی رخ داد. لطفا\u064b فایل را بررسی کرده و دوباره امتحان کنید.", "تأیید", AlertType.Error);
			return false;
		}
	}
}
