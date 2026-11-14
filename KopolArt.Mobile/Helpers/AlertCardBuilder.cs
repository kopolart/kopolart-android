using Microsoft.Maui.Controls.Shapes;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Helpers
{
    public static class AlertCardBuilder
    {
        public static (ContentPage? Page, Grid? RootGrid) GetCurrentPageRoot()
        {
            var currentPage = Shell.Current?.CurrentPage as ContentPage;
            if (currentPage == null && Application.Current?.Windows.Count > 0)
            {
                var window = Application.Current.Windows[0];
                currentPage = (window.Page as Shell)?.CurrentPage as ContentPage
                              ?? window.Page as ContentPage;
            }

            if (currentPage == null) return (null, null);

            if (currentPage.Content is Grid rootGrid)
            {
                return (currentPage, rootGrid);
            }

            if (currentPage.Content != null)
            {
                var existingContent = currentPage.Content;
                var wrapperGrid = new Grid();
                currentPage.Content = null;
                wrapperGrid.Children.Add(existingContent);
                currentPage.Content = wrapperGrid;
                return (currentPage, wrapperGrid);
            }
            else
            {
                var emptyGrid = new Grid();
                currentPage.Content = emptyGrid;
                return (currentPage, emptyGrid);
            }
        }

        public static AlertCardContext CreateAlertContext(
            ContentPage page,
            Grid rootGrid,
            string title,
            string message,
            AlertType type,
            bool isDestructive = false,
            int spacing = 14,
            int messageFontSize = 13)
        {
            var pagePadding = page.Padding;
            var totalPaddingLeft = rootGrid.Padding.Left + pagePadding.Left;
            var totalPaddingTop = rootGrid.Padding.Top + pagePadding.Top;
            var totalPaddingRight = rootGrid.Padding.Right + pagePadding.Right;
            var totalPaddingBottom = rootGrid.Padding.Bottom + pagePadding.Bottom;

            var overlayGrid = new Grid
            {
                ZIndex = 99999,
                InputTransparent = false,
                FlowDirection = FlowDirection.RightToLeft,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Margin = new Thickness(-totalPaddingLeft, -totalPaddingTop, -totalPaddingRight, -totalPaddingBottom)
            };

            Grid.SetRow(overlayGrid, 0);
            Grid.SetRowSpan(overlayGrid, Math.Max(1, rootGrid.RowDefinitions.Count));
            Grid.SetColumn(overlayGrid, 0);
            Grid.SetColumnSpan(overlayGrid, Math.Max(1, rootGrid.ColumnDefinitions.Count));

            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
            var backdropColor = isDark ? Color.FromRgba(0, 0, 0, 170) : Color.FromRgba(0, 0, 0, 130);
            var backdrop = new Border
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                InputTransparent = false,
                StrokeThickness = 0,
                Stroke = Colors.Transparent,
                BackgroundColor = backdropColor,
                Background = new SolidColorBrush(backdropColor),
                Opacity = 0
            };
            overlayGrid.Children.Add(backdrop);

            var card = new Border
            {
                MaximumWidthRequest = 330,
                Margin = new Thickness(20, 16),
                Padding = new Thickness(20, 18),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                StrokeThickness = 1,
                Scale = 0.85,
                Opacity = 0
            };
            card.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(24) };
            card.SetAppThemeColor(Border.BackgroundColorProperty, AppColors.CardBackgroundLight, AppColors.CardBackgroundDark);
            card.SetAppThemeColor(Border.StrokeProperty, AppColors.CardBorderLight, AppColors.CardBorderDark);

            var cardTapGesture = new TapGestureRecognizer();
            cardTapGesture.Tapped += (s, e) => { /* Consume tap inside card */ };
            card.GestureRecognizers.Add(cardTapGesture);

            var cardStack = new VerticalStackLayout
            {
                Spacing = spacing,
                HorizontalOptions = LayoutOptions.Fill
            };

            // Icon Badge Circle
            var iconCircle = new Border
            {
                WidthRequest = 52,
                HeightRequest = 52,
                HorizontalOptions = LayoutOptions.Center,
                StrokeThickness = 0
            };
            iconCircle.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(26) };

            var (iconGlyph, iconBgColor, iconColor) = GetIconStyle(type, isDestructive, isDark);
            iconCircle.BackgroundColor = iconBgColor;

            var iconLabel = new Label
            {
                Text = iconGlyph,
                FontFamily = Constants.Fonts.KopolArtFaAlias,
                FontSize = 26,
                TextColor = iconColor,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            iconCircle.Content = iconLabel;
            cardStack.Children.Add(iconCircle);

            // Title Label
            var titleLabel = new Label
            {
                Text = title,
                FontFamily = Constants.Fonts.UiFontBlackAlias,
                FontSize = 17,
                HorizontalTextAlignment = TextAlignment.Center
            };
            titleLabel.SetAppThemeColor(Label.TextColorProperty, AppColors.TextPrimaryLight, AppColors.TextPrimaryDark);
            cardStack.Children.Add(titleLabel);

            // Message Label
            if (!string.IsNullOrWhiteSpace(message))
            {
                var messageLabel = new Label
                {
                    Text = message,
                    FontFamily = Constants.Fonts.UiFontMediumAlias,
                    FontSize = messageFontSize,
                    LineBreakMode = LineBreakMode.WordWrap,
                    HorizontalTextAlignment = TextAlignment.Center
                };
                messageLabel.SetAppThemeColor(Label.TextColorProperty, AppColors.TextSecondaryLight, AppColors.TextSecondaryDark);
                cardStack.Children.Add(messageLabel);
            }

            var cardScrollView = new ScrollView
            {
                Content = cardStack,
                VerticalScrollBarVisibility = ScrollBarVisibility.Never,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Center
            };

            card.Content = cardScrollView;
            overlayGrid.Children.Add(card);

            void UpdateCardConstraints()
            {
                if (page.Height > 0)
                {
                    bool isLandscape = page.Width > page.Height;
                    double verticalMargin = isLandscape ? 32 : 48;
                    card.MaximumHeightRequest = Math.Max(200, page.Height - verticalMargin);
                }
            }

            UpdateCardConstraints();
            EventHandler sizeChangedHandler = (s, e) => UpdateCardConstraints();
            page.SizeChanged += sizeChangedHandler;

            return new AlertCardContext
            {
                RootGrid = rootGrid,
                OverlayGrid = overlayGrid,
                Backdrop = backdrop,
                Card = card,
                CardStack = cardStack,
                IsDark = isDark,
                Page = page,
                SizeChangedHandler = sizeChangedHandler
            };
        }

        public static (string Glyph, Color BgColor, Color FgColor) GetIconStyle(AlertType type, bool isDestructive, bool isDark = false)
        {
            if (isDestructive)
            {
                return (KopolArtFaIcons.AlertDanger, AppColors.StatusRedTint, AppColors.StatusRed);
            }

            return type switch
            {
                AlertType.Success => (KopolArtFaIcons.AlertSuccess, AppColors.StatusGreenTint, AppColors.StatusGreen),
                AlertType.Warning => (KopolArtFaIcons.AlertDanger, AppColors.StatusAmberTint, AppColors.StatusAmber),
                AlertType.Question => (KopolArtFaIcons.AlertQuestion, isDark ? AppColors.SlateTintDark : AppColors.SlateTintLight, isDark ? AppColors.SlateDark : AppColors.SlateLight),
                AlertType.Error => (KopolArtFaIcons.Cancel, AppColors.StatusRedTint, AppColors.StatusRed),
                _ => (KopolArtFaIcons.AlertInfo, isDark ? AppColors.StatusBlueTintDark : AppColors.StatusBlueTintLight, isDark ? AppColors.StatusBlueDark : AppColors.StatusBlue)
            };
        }
    }
}
