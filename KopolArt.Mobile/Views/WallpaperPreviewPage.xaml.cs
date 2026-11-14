using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Presentation;
using KopolArt.Mobile.ViewModels;

namespace KopolArt.Mobile.Views
{
    public partial class WallpaperPreviewPage : ContentPage, IQueryAttributable
    {
        private const double LandscapeCardMaxWidth = 340;

        private readonly WallpaperPreviewViewModel _viewModel;
        private bool _isLandscapeLayout;

        public WallpaperPreviewPage(WallpaperPreviewViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            LandscapeSafeAreaHelper.Attach(this);
            SizeChanged += OnPageSizeChanged;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue(WallpaperPreviewViewModel.ItemQueryKey, out var value) && value is DownloadDisplayItem item)
            {
                _ = _viewModel.InitializeAsync(item);
            }
        }

        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            bool isLandscape = Width > Height;
            if (isLandscape == _isLandscapeLayout || Width <= 0 || Height <= 0)
                return;

            _isLandscapeLayout = isLandscape;
            ApplyLayout(isLandscape, Width);
        }

        private void ApplyLayout(bool isLandscape, double pageWidth)
        {
            RootGrid.RowDefinitions.Clear();
            RootGrid.ColumnDefinitions.Clear();

            Grid.SetRowSpan(TopBarGrid, 1);

            if (isLandscape)
            {
                double cardWidth = Math.Min(LandscapeCardMaxWidth, Math.Max(220, pageWidth * 0.38));
                RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
                RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                RootGrid.ColumnDefinitions.Add(new ColumnDefinition(cardWidth));

                Grid.SetRow(TopBarGrid, 0);
                Grid.SetColumn(TopBarGrid, 0);
                Grid.SetColumnSpan(TopBarGrid, 2);

                Grid.SetRow(PreviewHost, 1);
                Grid.SetColumn(PreviewHost, 0);
                Grid.SetRowSpan(PreviewHost, 1);

                Grid.SetRow(ActionCard, 0);
                Grid.SetColumn(ActionCard, 1);
                Grid.SetRowSpan(ActionCard, 2);
                ActionCard.VerticalOptions = LayoutOptions.Center;
            }
            else
            {
                RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
                RootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

                Grid.SetRow(TopBarGrid, 0);
                Grid.SetColumn(TopBarGrid, 0);
                Grid.SetColumnSpan(TopBarGrid, 1);

                Grid.SetRow(PreviewHost, 1);
                Grid.SetColumn(PreviewHost, 0);
                Grid.SetRowSpan(PreviewHost, 1);

                Grid.SetRow(ActionCard, 2);
                Grid.SetColumn(ActionCard, 0);
                Grid.SetRowSpan(ActionCard, 1);
                ActionCard.VerticalOptions = LayoutOptions.Fill;
            }
        }

        private void OnPreviewHostSizeChanged(object? sender, EventArgs e) =>
            _viewModel.SetViewportHost(PreviewHost.Width, PreviewHost.Height);

        private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
        {
            switch (e.Status)
            {
                case GestureStatus.Started:
                    _viewModel.BeginPinch();
                    break;

                case GestureStatus.Running:
                    _viewModel.UpdatePinch(e.Scale, e.ScaleOrigin);
                    break;
            }
        }

        private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _viewModel.BeginPan();
                    break;

                case GestureStatus.Running:
                    _viewModel.UpdatePan(e.TotalX, e.TotalY);
                    break;
            }
        }

        private void OnDoubleTapped(object? sender, TappedEventArgs e) => _viewModel.ResetView();
    }
}
