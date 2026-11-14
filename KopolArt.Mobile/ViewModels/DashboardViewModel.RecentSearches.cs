using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.ViewModels
{
    public partial class DashboardViewModel
    {
        private RecentSearchChipItem CreateChipItem(string query) => new()
        {
            Query = query,
            SelectCommand = new Command(async () =>
            {
                SearchQuery = query;
                await ExecuteSearchAsync();
            }),
            RemoveCommand = new Command(() => RemoveRecentSearch(query))
        };

        public void LoadRecentSearches()
        {
            if (!MainThread.IsMainThread)
            {
                MainThread.BeginInvokeOnMainThread(LoadRecentSearches);
                return;
            }
            try
            {
                RecentSearches.Clear();
                var items = _searchHistoryService.GetRecentSearches();
                foreach (var item in items)
                {
                    RecentSearches.Add(CreateChipItem(item));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadRecentSearches error: {ex.Message}");
            }
            finally
            {
                OnPropertyChanged(nameof(HasRecentSearches));
                OnPropertyChanged(nameof(ShowRecentSearches));
            }
        }

        public void AddRecentSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;
            if (!MainThread.IsMainThread)
            {
                MainThread.BeginInvokeOnMainThread(() => AddRecentSearch(query));
                return;
            }
            try
            {
                _searchHistoryService.AddRecentSearch(query);
                LoadRecentSearches();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] AddRecentSearch error: {ex.Message}");
            }
            finally
            {
                OnPropertyChanged(nameof(HasRecentSearches));
                OnPropertyChanged(nameof(ShowRecentSearches));
            }
        }

        public void RemoveRecentSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;
            if (!MainThread.IsMainThread)
            {
                MainThread.BeginInvokeOnMainThread(() => RemoveRecentSearch(query));
                return;
            }
            try
            {
                _searchHistoryService.RemoveRecentSearch(query);
                LoadRecentSearches();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] RemoveRecentSearch error: {ex.Message}");
            }
            finally
            {
                OnPropertyChanged(nameof(HasRecentSearches));
                OnPropertyChanged(nameof(ShowRecentSearches));
            }
        }

        public void ClearAllRecentSearches()
        {
            if (!MainThread.IsMainThread)
            {
                MainThread.BeginInvokeOnMainThread(ClearAllRecentSearches);
                return;
            }
            try
            {
                RecentSearches.Clear();
                _searchHistoryService.ClearAllRecentSearches();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] ClearAllRecentSearches error: {ex.Message}");
            }
            finally
            {
                OnPropertyChanged(nameof(HasRecentSearches));
                OnPropertyChanged(nameof(ShowRecentSearches));
            }
        }
    }
}
