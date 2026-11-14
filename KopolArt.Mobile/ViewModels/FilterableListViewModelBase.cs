using System.Collections.ObjectModel;
using System.Windows.Input;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.ViewModels
{
    public abstract class FilterableListViewModelBase : BaseViewModel
    {
        private string _searchText = string.Empty;
        private string _selectedCategory = CategoryBadgeHelper.CategoryAll;
        private bool _isEmpty;
        private bool _isLoading;

        public ObservableCollection<CategoryFilterOption> CategoryFilters { get; } = [];

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = OnFilterOrSearchChangedAsync();
                }
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            set
            {
                if (SetProperty(ref _isEmpty, value))
                {
                    OnPropertyChanged(nameof(IsNotEmpty));
                }
            }
        }

        public bool IsNotEmpty => !IsEmpty;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand BackCommand { get; }
        public ICommand SelectCategoryCommand { get; }
        public ICommand ExploreWebsiteCommand { get; }

        protected FilterableListViewModelBase()
        {
            BackCommand = new Command(async () => await GoBackSafelyAsync());
            SelectCategoryCommand = new Command<string>(async category => await OnSelectCategoryAsync(category));
            ExploreWebsiteCommand = new Command(async () => await NavigateToWebsiteAsync());
            InitCategoryFilters();
        }

        public void InitCategoryFilters(IEnumerable<string>? customCategories = null)
        {
            CategoryFilters.Clear();
            var categories = customCategories ?? CategoryBadgeHelper.AllFilterCategories;
            foreach (var cat in categories)
            {
                CategoryFilters.Add(new CategoryFilterOption
                {
                    Name = cat,
                    IsSelected = cat == SelectedCategory
                });
            }
        }

        protected virtual async Task OnSelectCategoryAsync(string? category)
        {
            if (string.IsNullOrWhiteSpace(category) || category == SelectedCategory)
                return;

            SelectedCategory = category;
            foreach (var filter in CategoryFilters)
            {
                filter.IsSelected = filter.Name == SelectedCategory;
            }

            await OnFilterOrSearchChangedAsync();
        }

        protected abstract Task OnFilterOrSearchChangedAsync();

        protected bool IsCategoryFilterActive =>
            !string.IsNullOrWhiteSpace(SelectedCategory) &&
            SelectedCategory != CategoryBadgeHelper.CategoryAll;

        protected static void AddDateGroups<TItem, TGroup>(
            ObservableCollection<TGroup> target,
            IEnumerable<TItem> items,
            Func<TItem, DateTime> dateSelector,
            bool ascending,
            Func<string, string, IEnumerable<TItem>, TGroup> groupFactory)
            where TGroup : IDateGroup<TItem>
        {
            var grouped = items
                .GroupBy(item => PersianFormatHelper.GetDateGroupPeriod(dateSelector(item)))
                .OrderBy(group => ascending ? (3 - (int)group.Key) : (int)group.Key);

            foreach (var group in grouped)
            {
                var (title, icon) = PersianFormatHelper.GetDateGroupInfo(group.Key);
                target.Add(groupFactory(title, icon, group));
            }
        }

        protected static void RemoveFromDateGroups<TGroup, TItem>(
            ObservableCollection<TGroup> groups,
            TItem item)
            where TGroup : class, IDateGroup<TItem>
        {
            var parentGroup = groups.FirstOrDefault(group => group.Contains(item));
            if (parentGroup == null)
                return;

            parentGroup.Remove(item);

            if (parentGroup.Count == 0)
            {
                groups.Remove(parentGroup);
            }
        }

        protected async Task GoBackSafelyAsync()
        {
            await NavigationHelper.GoBackSafelyAsync();
        }

        protected async Task NavigateToWebsiteAsync()
        {
            try
            {
                await NavigationHelper.NavigateToBrowserAsync(Constants.Urls.Home);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FilterableListViewModelBase] Explore error: {ex.Message}");
            }
        }
    }
}
