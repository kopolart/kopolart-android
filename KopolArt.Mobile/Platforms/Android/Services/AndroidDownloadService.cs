#define DEBUG
 using System.Diagnostics;
 using System.Net;
 using System.Net.Http.Headers;
using System.Text;
using Android.Content;
using Android.Webkit;
using Android.Widget;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.Platforms.Android.Services;

public class AndroidDownloadService : IDownloadService
{
	private readonly ISettingsService _settingsService;

	private readonly ICustomAlertService _alertService;

	private readonly IDownloadHistoryService _downloadHistoryService;

	private readonly IDownloadFolderPickerService? _folderPicker;

	private readonly AndroidDownloadStorageService _storageService;

	private readonly AndroidDownloadNotificationService _notificationService;

	private static int _notificationId = 7000;

	private static readonly HttpClient SharedHttpClient = new HttpClient(new HttpClientHandler
	{
		AllowAutoRedirect = true,
		MaxAutomaticRedirections = 10
	})
	{
		Timeout = TimeSpan.FromMinutes(10L)
	};

	public AndroidDownloadService(ISettingsService settingsService, ICustomAlertService alertService, IDownloadHistoryService downloadHistoryService, IDownloadFolderPickerService? folderPicker = null)
	{
		_settingsService = settingsService;
		_alertService = alertService;
		_downloadHistoryService = downloadHistoryService;
		_folderPicker = folderPicker;
		Context context = global::Android.App.Application.Context;
		_storageService = new AndroidDownloadStorageService(context);
		_notificationService = new AndroidDownloadNotificationService(context);
	}

	public async Task<bool> DownloadFileAsync(string url, string? fileName = null, string? mimeType = null, string? contentDisposition = null, string? userAgent = null, bool isRetry = false)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return false;
		}
		if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
		{
			return await DownloadDataUriAsync(url, fileName, mimeType);
		}
		if (!(await EnsureDownloadLocationConfirmedAsync()))
		{
			return false;
		}
		// Android 8/9 need the legacy storage permission to write into public Downloads.
		// If it is denied the storage layer falls back to the app's own Downloads folder.
		await MainActivity.RequestLegacyStoragePermissionAsync();
		MainActivity.RequestNotificationPermission((bool _) =>
		{
		});
		ShowToast(isRetry ? "شروع دانلود مجدد..." : "شروع دانلود...");
		return await Task.Run(async () =>
		{
			int notifId = Interlocked.Increment(ref _notificationId);
			string initialFileName = ((!string.IsNullOrWhiteSpace(fileName)) ? DownloadFileNameResolver.SanitizeFileName(fileName) : DownloadFileNameResolver.ResolveFileName(url, null, mimeType));
			try
			{
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
				try
				{
					CookieManager? cookieManager = CookieManager.Instance;
					string? cookies = ((cookieManager != null) ? cookieManager.GetCookie(url) : null);
					if (!string.IsNullOrWhiteSpace(cookies))
					{
						request.Headers.TryAddWithoutValidation("Cookie", cookies);
					}
				}
				catch
				{
				}
				string ua = ((!string.IsNullOrWhiteSpace(userAgent)) ? userAgent : ("Mozilla/5.0 (Linux; Android 10; Mobile) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36 " + Constants.Network.AppUserAgent));
				request.Headers.TryAddWithoutValidation("User-Agent", ua);
				_notificationService.ShowProgressNotification(notifId, initialFileName, -1);
				using HttpResponseMessage response = await SharedHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
				response.EnsureSuccessStatusCode();
				string finalMime = ((!string.IsNullOrWhiteSpace(mimeType)) ? mimeType : (response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream"));
				string finalFileName = initialFileName;
				ContentDispositionHeaderValue cd = response.Content.Headers.ContentDisposition;
				if (!string.IsNullOrWhiteSpace(cd?.FileNameStar))
				{
					finalFileName = DownloadFileNameResolver.SanitizeFileName(cd.FileNameStar.Trim('"'));
				}
				else if (!string.IsNullOrWhiteSpace(cd?.FileName))
				{
					finalFileName = DownloadFileNameResolver.SanitizeFileName(cd.FileName.Trim('"'));
				}
				else if (string.IsNullOrWhiteSpace(fileName))
				{
					finalFileName = DownloadFileNameResolver.ResolveFileName(url, response.Content.Headers.ContentDisposition?.ToString(), finalMime);
				}
				long total = response.Content.Headers.ContentLength.GetValueOrDefault();
				ActiveDownloadSession session = new ActiveDownloadSession(notifId, finalFileName, (int pct, bool isPaused) =>
				{
					_notificationService.ShowProgressNotification(notifId, finalFileName, pct, isPaused);
				});
				ActiveDownloadTracker.Register(session);
				try
				{
					bool result;
					await using (Stream stream = await response.Content.ReadAsStreamAsync(session.CancellationToken))
					{
						string savedPathOrUri = await _storageService.SaveFileAsync(stream, finalFileName, finalMime, total, (int pct) =>
						{
							session.CurrentProgress = pct;
							if (!session.IsPaused)
							{
								_notificationService.ShowProgressNotification(notifId, finalFileName, pct);
							}
							return Task.CompletedTask;
						}, allowApk: true, session, session.CancellationToken);
						if (!string.IsNullOrWhiteSpace(savedPathOrUri))
						{
							_notificationService.ShowCompleteNotification(notifId, finalFileName, savedPathOrUri, finalMime);
							ShowToast("دانلود با موفقیت انجام شد: " + finalFileName);
							await _downloadHistoryService.AddDownloadAsync(finalFileName, url, finalMime, total, savedPathOrUri);
							result = true;
						}
						else
						{
							_notificationService.CancelNotification(notifId);
							ShowToast("دانلود ناموفق: خطا در ذخیره فایل.");
							await _downloadHistoryService.AddDownloadAsync(finalFileName, url, finalMime, total, string.Empty, "Failed");
							result = false;
						}
					}
					return result;
				}
				finally
				{
					ActiveDownloadTracker.Unregister(notifId);
				}
			}
			catch (OperationCanceledException)
			{
				_notificationService.CancelNotification(notifId);
				ShowToast("دانلود لغو شد.");
				return false;
			}
			catch (Exception value)
			{
				_notificationService.CancelNotification(notifId);
				Debug.WriteLine($"[AndroidDownloadService] Download failed: {value}");
				ShowToast("خطا در دانلود فایل. لطفا\u064b اتصال اینترنت خود را بررسی کنید.");
				await _downloadHistoryService.AddDownloadAsync(initialFileName, url, mimeType ?? "application/octet-stream", 0L, string.Empty, "Failed");
				return false;
			}
		});
	}

	private async Task<bool> EnsureDownloadLocationConfirmedAsync()
	{
		if (_settingsService.Settings.DontShowDownloadPromptAgain)
		{
			return true;
		}
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		MainThread.BeginInvokeOnMainThread((Action)(async () =>
		{
			try
			{
				string currentDisplay = ((string.IsNullOrWhiteSpace(_settingsService.Settings.CustomDownloadFolderName) || _settingsService.Settings.CustomDownloadFolderName.Equals("پوشه دانلودهای سیستم (پیش\u200cفرض)", StringComparison.OrdinalIgnoreCase)) ? "پوشه دانلودهای سیستم (پیش\u200cفرض)" : StoragePathHelper.FormatFriendlyPath(_settingsService.Settings.CustomDownloadFolderName));
				DownloadPromptResult result = await _alertService.ShowDownloadLocationPromptAsync(currentDisplay);
				if (result == null || result.Action == DownloadPromptAction.Cancel)
				{
					tcs.TrySetResult(result: false);
				}
				else
				{
					if (result.Action == DownloadPromptAction.ChangeFolder && _folderPicker != null)
					{
						(string? UriString, string? DisplayName)? picked = await _folderPicker.PickFolderAsync();
						if (picked.HasValue && !string.IsNullOrWhiteSpace(picked.Value.UriString))
						{
							_settingsService.Settings.CustomDownloadFolderUri = picked.Value.UriString;
							_settingsService.Settings.CustomDownloadFolderName = StoragePathHelper.FormatFriendlyPath(picked.Value.DisplayName);
						}
					}
					if (result.DontShowAgain)
					{
						_settingsService.Settings.DontShowDownloadPromptAgain = true;
					}
					await _settingsService.SaveSettingsAsync(_settingsService.Settings);
					tcs.TrySetResult(result: true);
				}
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Debug.WriteLine("[AndroidDownloadService] Location prompt error: " + ex2.Message);
				tcs.TrySetResult(result: true);
			}
		}));
		return await tcs.Task;
	}

	public async Task<bool> DownloadDataUriAsync(string dataUri, string? fileName = null, string? mimeType = null)
	{
		try
		{
			int commaIndex = dataUri.IndexOf(',');
			if (commaIndex < 0)
			{
				ShowToast("فرمت آدرس داده نامعتبر است.");
				return false;
			}
			if (!(await EnsureDownloadLocationConfirmedAsync()))
			{
				return false;
			}
			await MainActivity.RequestLegacyStoragePermissionAsync();
			string meta = dataUri.Substring(0, commaIndex);
			string dataPart = dataUri.Substring(commaIndex + 1);
			bool isBase64 = meta.Contains(";base64", StringComparison.OrdinalIgnoreCase);
			string detectedMime = mimeType;
			if (string.IsNullOrWhiteSpace(detectedMime) && meta.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
			{
				string temp = meta.Substring(5);
				int semicolonIndex = temp.IndexOf(';');
				if (semicolonIndex >= 0)
				{
					temp = temp.Substring(0, semicolonIndex);
				}
				if (!string.IsNullOrWhiteSpace(temp))
				{
					detectedMime = temp;
				}
			}
			byte[] bytes = (isBase64 ? Convert.FromBase64String(dataPart) : Encoding.UTF8.GetBytes(WebUtility.UrlDecode(dataPart)));
			string finalFileName = ((!string.IsNullOrWhiteSpace(fileName)) ? DownloadFileNameResolver.SanitizeFileName(fileName) : DownloadFileNameResolver.ResolveFileName("data:file", null, detectedMime));
			string finalMime = ((!string.IsNullOrWhiteSpace(detectedMime)) ? detectedMime : "application/octet-stream");
			int notifId = Interlocked.Increment(ref _notificationId);
			_notificationService.ShowProgressNotification(notifId, finalFileName, -1);
			using MemoryStream stream = new MemoryStream(bytes);
			string savedPathOrUri = await _storageService.SaveFileAsync(stream, finalFileName, finalMime, bytes.Length, null, allowApk: false);
			if (!string.IsNullOrWhiteSpace(savedPathOrUri))
			{
				_notificationService.ShowCompleteNotification(notifId, finalFileName, savedPathOrUri, finalMime);
				ShowToast("دانلود با موفقیت انجام شد: " + finalFileName);
				await _downloadHistoryService.AddDownloadAsync(finalFileName, "data:inline", finalMime, bytes.Length, savedPathOrUri);
				return true;
			}
			_notificationService.CancelNotification(notifId);
			ShowToast("دانلود ناموفق: خطا در ذخیره فایل.");
			return false;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Debug.WriteLine("[AndroidDownloadService] Data URI save error: " + ex2.Message);
			ShowToast("خطا در ذخیره داده.");
			return false;
		}
	}

	private void ShowToast(string message)
	{
		MainThread.BeginInvokeOnMainThread((Action)(() =>
		{
			try
			{
				Toast val = Toast.MakeText(global::Android.App.Application.Context, message, (ToastLength)0);
				if (val != null)
				{
					val.Show();
				}
			}
			catch
			{
			}
		}));
	}
}
