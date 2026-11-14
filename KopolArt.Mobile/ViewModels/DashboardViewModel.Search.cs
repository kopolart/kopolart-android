using KopolArt.Mobile.Helpers;

using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.ViewModels
{
    public partial class DashboardViewModel
    {
        private void InitializeSearchCategories()
        {
            _searchCategoryOptions.Clear();

            _searchCategoryOptions.Add(new SearchCategoryOption
            {
                Type = SearchCategoryType.Posts,
                Title = "پست‌ها",
                ShortTitle = "پست‌ها",
                SearchUrl = Constants.Urls.PostsSearch,
                IconGlyph = KopolArtFaIcons.Newspaper,
                Placeholder = "جستجو در آرت‌ورک‌ها و آموزش‌ها...",
                AccentColorLight = CategoryPalette.Posts.AccentLight,
                AccentColorDark = CategoryPalette.Posts.AccentDark
            });

            _searchCategoryOptions.Add(new SearchCategoryOption
            {
                Type = SearchCategoryType.Palettes,
                Title = "پالت‌های رنگی",
                ShortTitle = "پالت‌ها",
                SearchUrl = Constants.Urls.ColorPalettesSearch,
                IconGlyph = KopolArtFaIcons.Palette,
                Placeholder = "جستجو در پالت‌های رنگی...",
                AccentColorLight = CategoryPalette.Palettes.AccentLight,
                AccentColorDark = CategoryPalette.Palettes.AccentDark
            });

            _searchCategoryOptions.Add(new SearchCategoryOption
            {
                Type = SearchCategoryType.Colors,
                Title = "رنگ‌ها",
                ShortTitle = "رنگ‌ها",
                SearchUrl = Constants.Urls.ColorsSearch,
                IconGlyph = KopolArtFaIcons.Sun,
                Placeholder = "جستجو در کدها و نام رنگ‌ها...",
                AccentColorLight = CategoryPalette.Colors.AccentLight,
                AccentColorDark = CategoryPalette.Colors.AccentDark
            });

            _searchCategoryOptions.Add(new SearchCategoryOption
            {
                Type = SearchCategoryType.Wallpapers,
                Title = "والپیپرها",
                ShortTitle = "والپیپرها",
                SearchUrl = Constants.Urls.WallpapersSearch,
                IconGlyph = KopolArtFaIcons.Image,
                Placeholder = "جستجو در والپیپرهای باکیفیت...",
                AccentColorLight = CategoryPalette.Wallpapers.AccentLight,
                AccentColorDark = CategoryPalette.Wallpapers.AccentDark
            });

            var savedCategory = Preferences.Default.Get(Constants.Keys.LastSelectedSearchCategory, SearchCategoryType.Posts.ToString());
            if (Enum.TryParse<SearchCategoryType>(savedCategory, out var savedType))
            {
                _selectedSearchCategory = _searchCategoryOptions.FirstOrDefault(c => c.Type == savedType) ?? _searchCategoryOptions[0];
            }
            else
            {
                _selectedSearchCategory = _searchCategoryOptions[0];
            }
        }

        private async Task SelectSearchCategoryAsync()
        {
            var options = _searchCategoryOptions.Select(c => new AlertOptionItem(
                c.Title,
                c.IconGlyph,
                c.Type.ToString(),
                Constants.Fonts.KopolArtFaAlias
            )).ToList();

            var selectedKey = await _customAlertService.ShowGridOptionsAsync(
                "دسته‌بندی جستجو",
                "بخش مورد نظر برای جستجو در کپل‌آرت را انتخاب کنید:",
                options,
                "انصراف",
                AlertType.Info,
                columns: 2);

            if (!string.IsNullOrEmpty(selectedKey) && Enum.TryParse<SearchCategoryType>(selectedKey, out var selectedType))
            {
                var found = _searchCategoryOptions.FirstOrDefault(c => c.Type == selectedType);
                if (found != null)
                {
                    SelectedSearchCategory = found;
                    Preferences.Default.Set(Constants.Keys.LastSelectedSearchCategory, selectedType.ToString());

                    var clean = _searchQuery?.Trim();
                    if (!string.IsNullOrWhiteSpace(clean) && clean.Length >= 2)
                    {
                        TriggerSearchSuggestionsDebounced();
                    }
                }
            }
        }

        private async Task ExecuteSearchAsync()
        {
            CancelSearchDebounce();
            ShowSuggestions = false;
            KeyboardHelper.HideKeyboard();

            var query = SearchQuery?.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                await _customAlertService.ShowAlertAsync(
                    "جستجو در کپل‌آرت",
                    "لطفاً عبارت مورد نظر خود را برای جستجو وارد کنید.",
                    "متوجه شدم",
                    AlertType.Info);
                return;
            }

            AddRecentSearch(query);

            var encoded = Uri.EscapeDataString(query);
            var targetUrl = $"{SelectedSearchCategory.SearchUrl}?query={encoded}";
            await NavigateToBrowserAsync(targetUrl);
        }

        private void CancelSearchDebounce()
        {
            if (_searchDebounceCts != null)
            {
                try
                {
                    _searchDebounceCts.Cancel();
                    _searchDebounceCts.Dispose();
                }
                catch { }
                _searchDebounceCts = null;
            }
        }

        private void TriggerSearchSuggestionsDebounced()
        {
            CancelSearchDebounce();

            var clean = _searchQuery?.Trim();
            if (string.IsNullOrWhiteSpace(clean) || clean.Length < 2)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Suggestions.Clear();
                    ShowSuggestions = false;
                    HasNoSuggestions = false;
                    IsSearchingSuggestions = false;
                });
                return;
            }

            _searchDebounceCts = new CancellationTokenSource();
            var token = _searchDebounceCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(350, token);
                    if (token.IsCancellationRequested) return;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        IsSearchingSuggestions = true;
                        ShowSuggestions = true;
                        HasNoSuggestions = false;
                    });

                    var categoryType = SelectedSearchCategory?.Type ?? SearchCategoryType.Posts;
                    var results = await _searchSuggestionService.GetSuggestionsAsync(clean, categoryType, token);

                    if (token.IsCancellationRequested) return;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Suggestions.Clear();
                        foreach (var item in results)
                        {
                            Suggestions.Add(item);
                        }
                        HasNoSuggestions = results.Count == 0;
                        IsSearchingSuggestions = false;
                        ShowSuggestions = true;
                    });
                }
                catch (OperationCanceledException)
                {
                    // Debounce cancelled by next keystroke
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] Suggestions error: {ex.Message}");
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        IsSearchingSuggestions = false;
                    });
                }
            }, token);
        }

        private async Task OpenSuggestionAsync(SearchSuggestionItem? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.FullTargetUrl)) return;
            CancelSearchDebounce();
            ShowSuggestions = false;
            KeyboardHelper.HideKeyboard();

            var queryToSave = !string.IsNullOrWhiteSpace(SearchQuery) ? SearchQuery.Trim() : item.Label?.Trim();
            if (!string.IsNullOrWhiteSpace(queryToSave))
            {
                AddRecentSearch(queryToSave);
            }

            await NavigateToBrowserAsync(item.FullTargetUrl);
        }

    }
}
