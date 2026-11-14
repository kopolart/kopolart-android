using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.ViewModels;

namespace KopolArt.Mobile.Views
{
    [QueryProperty(nameof(SelectedTab), "tab")]
    public partial class UsagePage : ContentPage
    {
        private readonly UsageViewModel _viewModel;
        private bool _shouldScrollToEndOnSizeChange;
        private string? _selectedTab;

        public string? SelectedTab
        {
            get => _selectedTab;
            set
            {
                _selectedTab = value;
                if (string.Equals(value, "analytics", StringComparison.OrdinalIgnoreCase))
                {
                    _viewModel.SelectAnalyticsTabCommand.Execute(null);
                }
            }
        }

        public UsagePage(UsageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            LandscapeSafeAreaHelper.Attach(this);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.RequestScrollToEnd += OnRequestScrollToEnd;
            if (ChartBarsLayout != null)
            {
                ChartBarsLayout.SizeChanged += OnChartBarsLayoutSizeChanged;
            }

            _ = _viewModel.LoadSessionsAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.RequestScrollToEnd -= OnRequestScrollToEnd;
            if (ChartBarsLayout != null)
            {
                ChartBarsLayout.SizeChanged -= OnChartBarsLayoutSizeChanged;
            }
        }

        private void OnRequestScrollToEnd()
        {
            _shouldScrollToEndOnSizeChange = true;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await ScrollChartToEndAsync();
            });
        }

        private void OnChartBarsLayoutSizeChanged(object? sender, EventArgs e)
        {
            if (_shouldScrollToEndOnSizeChange)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ScrollChartToEndAsync();
                });
            }
        }

        private async Task ScrollChartToEndAsync()
        {
            if (ChartScrollView == null || ChartBarsLayout == null) return;

            // Wait until layout pass completes and bars are measured
            for (int attempt = 0; attempt < 6; attempt++)
            {
                await Task.Delay(40);
                if (ChartScrollView == null || ChartBarsLayout == null) return;

                double targetX = ChartBarsLayout.Width - ChartScrollView.Width;
                if (targetX > 0)
                {
                    _shouldScrollToEndOnSizeChange = false;
                    await ChartScrollView.ScrollToAsync(targetX + 100, 0, false);
                    break;
                }
            }
        }
    }
}
