using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.ViewModels
{
    public partial class BrowserViewModel
    {
        public async Task ShowPageContextMenuAsync(string currentUrl, string pageTitle)
        {
            if (string.IsNullOrWhiteSpace(currentUrl) ||
                currentUrl.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
                currentUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            try
            {
                bool isBookmarked = await _bookmarkService.IsBookmarkedAsync(currentUrl);
                string findOptionText = "جستجو در صفحه";
                string saveOfflineOptionText = "ذخیره برای مطالعه آفلاین";
                string bookmarkOptionText = isBookmarked ? "حذف از نشان‌شده‌ها" : "نشان‌کردن این صفحه";
                string shareOptionText = "اشتراک‌گذاری این صفحه";
                string copyOptionText = "کپی پیوند صفحه";
                string reloadOptionText = "بارگذاری مجدد صفحه";
                string returnToDashboardOptionText = "بازگشت به داشبورد";
                string openInBrowserOptionText = "باز کردن در مرورگر گوشی";
                var options = new List<AlertOptionItem>
                {
                    new("جستجو در صفحه", KopolArtFaIcons.Search, findOptionText),
                    new("ذخیره آفلاین", KopolArtFaIcons.DocTextInv, saveOfflineOptionText),
                    new(isBookmarked ? "حذف نشان" : "نشان‌کردن",
                        isBookmarked ? KopolArtFaIcons.BookmarkEmpty : KopolArtFaIcons.Bookmark,
                        bookmarkOptionText),

                    new("اشتراک‌گذاری", KopolArtFaIcons.ShareSquared, shareOptionText),
                    new("کپی پیوند", KopolArtFaIcons.Copy, copyOptionText),
                    new("تازه‌سازی", KopolArtFaIcons.Refresh, reloadOptionText),
                    new("داشبورد", KopolArtFaIcons.ThLarge, returnToDashboardOptionText),
                    new("مرورگر گوشی", KopolArtFaIcons.ExportAlt, openInBrowserOptionText)
                };

                string? action = null;
                if (_customAlertService != null)
                {
                    action = await _customAlertService.ShowGridOptionsAsync(
                        "عملیات صفحه",
                        "عملیات مورد نظر برای این صفحه را انتخاب نمایید:",
                        options,
                        "انصراف",
                        AlertType.Question,
                        columns: 2);
                }
                else
                {
                    var page = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
                    if (page != null)
                    {
                        action = await page.DisplayActionSheetAsync(
                            "عملیات صفحه",
                            "انصراف",
                            null,
                            options.Select(o => o.ReturnValue ?? o.Text).ToArray());
                    }
                }
                if (string.IsNullOrWhiteSpace(action) || action == "انصراف")
                {
                    return;
                }
                if (action == findOptionText || action == "جستجو در صفحه")
                {
                    RequestActivateFindInPage?.Invoke(this, EventArgs.Empty);
                    return;
                }
                if (action == saveOfflineOptionText || action == "ذخیره آفلاین" || action == "ذخیره برای مطالعه آفلاین")
                {
                    RequestSavePageForOffline?.Invoke(this, (currentUrl, pageTitle));
                    return;
                }
                if (action == bookmarkOptionText || action == "نشان‌کردن" || action == "حذف نشان")
                {
                    if (isBookmarked)
                    {
                        await _bookmarkService.RemoveBookmarkAsync(currentUrl);
                        bool showAll = false;
                        if (_customAlertService != null)
                        {
                            showAll = await _customAlertService.ShowConfirmAsync(
                                "حذف از نشان‌شده‌ها",
                                "این صفحه از نشان‌شده‌ها حذف شد.",
                                "مشاهده نشان‌شده‌ها",
                                "ادامه",
                                AlertType.Warning);
                        }
                        if (showAll && Shell.Current != null)
                        {
                            await Shell.Current.GoToAsync("BookmarksPage");
                        }
                    }
                    else
                    {
                        await _bookmarkService.AddBookmarkAsync(pageTitle, currentUrl);
                        bool showAll = false;
                        if (_customAlertService != null)
                        {
                            showAll = await _customAlertService.ShowConfirmAsync(
                                "نشان‌گذاری صفحه",
                                "این صفحه به نشان‌شده‌ها اضافه شد.",
                                "مشاهده نشان‌شده‌ها",
                                "ادامه",
                                AlertType.Success);
                        }
                        if (showAll && Shell.Current != null)
                        {
                            await Shell.Current.GoToAsync("BookmarksPage");
                        }
                    }
                }
                else if (action == shareOptionText || action == "اشتراک‌گذاری")
                {
                    await Share.Default.RequestAsync(new ShareTextRequest
                    {
                        Title = pageTitle,
                        Text = pageTitle,
                        Uri = currentUrl
                    });
                }
                else if (action == copyOptionText || action == "کپی پیوند")
                {
                    await Clipboard.Default.SetTextAsync(currentUrl);
#if ANDROID
                    Android.Widget.Toast.MakeText(Android.App.Application.Context, "پیوند صفحه کپی شد", Android.Widget.ToastLength.Short)?.Show();
#endif
                }
                else if (action == reloadOptionText || action == "تازه‌سازی")
                {
                    RaiseReloadRequest();
                }
                else if (action == returnToDashboardOptionText || action == "داشبورد")
                {
                    UpdateLastVisitedUrl(currentUrl);
                    if (Shell.Current != null)
                    {
                        await Shell.Current.GoToAsync("..");
                    }
                }
                else if (action == openInBrowserOptionText || action == "مرورگر گوشی")
                {
                    if (Uri.TryCreate(currentUrl, UriKind.Absolute, out var uri))
                    {
                        await Launcher.Default.OpenAsync(uri);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserViewModel] ShowPageContextMenu error: {ex.Message}");
            }
        }
        public async Task ToggleBookmarkAsync(string currentUrl, string pageTitle)
        {
            if (string.IsNullOrWhiteSpace(currentUrl) ||
                currentUrl.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
                currentUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            try
            {
                bool isBookmarked = await _bookmarkService.IsBookmarkedAsync(currentUrl);
                if (!isBookmarked)
                {
                    await _bookmarkService.AddBookmarkAsync(pageTitle, currentUrl);
                    bool showAll = await _customAlertService.ShowConfirmAsync(
                        "نشان‌گذاری صفحه",
                        "این صفحه به نشان‌شده‌ها اضافه شد.",
                        "مشاهده نشان‌شده‌ها",
                        "ادامه",
                        AlertType.Success);
                    if (showAll)
                    {
                        await Shell.Current.GoToAsync("BookmarksPage");
                    }
                }
                else
                {
                    await _bookmarkService.RemoveBookmarkAsync(currentUrl);
                    bool showAll = await _customAlertService.ShowConfirmAsync(
                        "حذف از نشان‌شده‌ها",
                        "این صفحه از نشان‌شده‌ها حذف شد.",
                        "مشاهده نشان‌شده‌ها",
                        "ادامه",
                        AlertType.Warning);
                    if (showAll)
                    {
                        await Shell.Current.GoToAsync("BookmarksPage");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserViewModel] ToggleBookmark error: {ex.Message}");
            }
        }
    }
}
