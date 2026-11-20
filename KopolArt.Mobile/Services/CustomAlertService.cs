using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;
using Microsoft.Maui.Controls.Shapes;
namespace KopolArt.Mobile.Services
{
    public class CustomAlertService : ICustomAlertService
    {
        public Task ShowAlertAsync(string title, string message, string buttonText = "تأیید", AlertType type = AlertType.Info)
        {
            return ShowDialogInternalAsync(title, message, buttonText, null, type, false);
        }
        public async Task<bool> ShowConfirmAsync(string title, string message, string acceptText = "تأیید", string cancelText = "انصراف", AlertType type = AlertType.Question, bool isDestructive = false)
        {
            var result = await ShowDialogInternalAsync(title, message, acceptText, cancelText, type, isDestructive);
            return result;
        }
        public async Task<string?> ShowChoiceAsync(
            string title,
            string message,
            string primaryActionText,
            string secondaryActionText,
            string cancelText = "انصراف",
            AlertType type = AlertType.Question)
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var (currentPage, rootGrid) = AlertCardBuilder.GetCurrentPageRoot();
                if (currentPage == null || rootGrid == null)
                {
                    return null;
                }
                var tcs = new TaskCompletionSource<string?>();
                var context = AlertCardBuilder.CreateAlertContext(currentPage, rootGrid, title, message, type, isDestructive: false, spacing: 12, messageFontSize: 12);
                var buttonsStack = new VerticalStackLayout
                {
                    Spacing = 8,
                    Margin = new Thickness(0, 6, 0, 0)
                };
                var primaryBtn = new Button
                {
                    Text = primaryActionText,
                    FontFamily = Constants.Fonts.UiFontBoldAlias,
                    FontSize = 13,
                    HeightRequest = 42,
                    CornerRadius = 14,
                    TextColor = Colors.White,
                    BackgroundColor = AppColors.BrandPrimary
                };
                primaryBtn.Clicked += async (s, e) => await context.DismissAsync(() => tcs.TrySetResult("primary"));
                buttonsStack.Children.Add(primaryBtn);
                var secondaryBtn = new Button
                {
                    Text = secondaryActionText,
                    FontFamily = Constants.Fonts.UiFontBoldAlias,
                    FontSize = 13,
                    HeightRequest = 42,
                    CornerRadius = 14,
                    BackgroundColor = Colors.Transparent,
                    BorderWidth = 1.5
                };
                secondaryBtn.SetAppThemeColor(Button.TextColorProperty, AppColors.BrandPrimary, AppColors.BrandPrimaryDark);
                secondaryBtn.SetAppThemeColor(Button.BorderColorProperty, AppColors.BrandPrimary, AppColors.BrandPrimaryDark);
                secondaryBtn.Clicked += async (s, e) => await context.DismissAsync(() => tcs.TrySetResult("secondary"));
                buttonsStack.Children.Add(secondaryBtn);
                var cancelBtn = new Button
                {
                    Text = cancelText,
                    FontFamily = Constants.Fonts.UiFontMediumAlias,
                    FontSize = 12,
                    HeightRequest = 38,
                    CornerRadius = 12,
                    BackgroundColor = Colors.Transparent
                };
                cancelBtn.SetAppThemeColor(Button.TextColorProperty, AppColors.TextMutedLight, AppColors.TextMutedDark);
                cancelBtn.Clicked += async (s, e) => await context.DismissAsync(() => tcs.TrySetResult(null));
                buttonsStack.Children.Add(cancelBtn);
                context.CardStack.Children.Add(buttonsStack);
                var backdropTapGesture = new TapGestureRecognizer();
                backdropTapGesture.Tapped += async (s, e) => await context.DismissAsync(() => tcs.TrySetResult(null));
                context.Backdrop.GestureRecognizers.Add(backdropTapGesture);
                await context.AnimateInAsync();
                return await tcs.Task;
            });
        }
        public async Task<string?> ShowOptionsAsync(
            string title,
            string? message,
            IEnumerable<string> options,
            string cancelText = "انصراف",
            AlertType type = AlertType.Info)
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var (currentPage, rootGrid) = AlertCardBuilder.GetCurrentPageRoot();
                if (currentPage == null || rootGrid == null)
                {
                    return null;
                }
                var tcs = new TaskCompletionSource<string?>();
                var context = AlertCardBuilder.CreateAlertContext(
                    currentPage,
                    rootGrid,
                    title,
                    message ?? string.Empty,
                    type,
                    isDestructive: false,
                    spacing: 12,
                    messageFontSize: 12);
                var optionsStack = new VerticalStackLayout
                {
                    Spacing = 8,
                    Margin = new Thickness(0, 4, 0, 0),
                    HorizontalOptions = LayoutOptions.Fill
                };
                foreach (var option in options)
                {
                    var optionBtn = new Button
                    {
                        Text = option,
                        FontFamily = Constants.Fonts.UiFontBoldAlias,
                        FontSize = 13,
                        HeightRequest = 42,
                        CornerRadius = 14,
                        BorderWidth = 1,
                        HorizontalOptions = LayoutOptions.Fill
                    };
                    optionBtn.SetAppThemeColor(Button.BackgroundColorProperty, AppColors.OptionBackgroundLight, AppColors.OptionBackgroundDark);
                    optionBtn.SetAppThemeColor(Button.BorderColorProperty, AppColors.CardBorderLight, AppColors.CardBorderDark);
                    optionBtn.SetAppThemeColor(Button.TextColorProperty, AppColors.TextPrimaryLight, AppColors.TextPrimaryDark);
                    var captured = option;
                    optionBtn.Clicked += async (s, e) => await context.DismissAsync(() => tcs.TrySetResult(captured));
                    optionsStack.Children.Add(optionBtn);
                }
                var cancelBtn = new Button
                {
                    Text = cancelText,
                    FontFamily = Constants.Fonts.UiFontMediumAlias,
                    FontSize = 12,
                    HeightRequest = 38,
                    CornerRadius = 12,
                    BackgroundColor = Colors.Transparent,
                    HorizontalOptions = LayoutOptions.Fill
                };
                cancelBtn.SetAppThemeColor(Button.TextColorProperty, AppColors.TextSubtleLight, AppColors.TextSubtleDark);
                cancelBtn.Clicked += async (s, e) => await context.DismissAsync(() => tcs.TrySetResult(null));
                optionsStack.Children.Add(cancelBtn);
                context.CardStack.Children.Add(optionsStack);
                var backdropTapGesture = new TapGestureRecognizer();
                backdropTapGesture.Tapped += async (s, e) => await context.DismissAsync(() => tcs.TrySetResult(null));
                context.Backdrop.GestureRecognizers.Add(backdropTapGesture);
                await context.AnimateInAsync();
                return await tcs.Task;
            });
        }

        public async Task<string?> ShowGridOptionsAsync(
            string title,
            string? message,
            IEnumerable<AlertOptionItem> options,
            string cancelText = "انصراف",
            AlertType type = AlertType.Info,
            int columns = 2)
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var (currentPage, rootGrid) = AlertCardBuilder.GetCurrentPageRoot();
                if (currentPage == null || rootGrid == null)
                {
                    return null;
                }

                var optionsList = options.ToList();
                if (optionsList.Count == 0) return null;

                var tcs = new TaskCompletionSource<string?>();
                var context = AlertCardBuilder.CreateAlertContext(
                    currentPage,
                    rootGrid,
                    title,
                    message ?? string.Empty,
                    type,
                    isDestructive: false,
                    spacing: 12,
                    messageFontSize: 12);

                var grid = new Grid
                {
                    ColumnSpacing = 8,
                    RowSpacing = 8,
                    HorizontalOptions = LayoutOptions.Fill,
                    FlowDirection = FlowDirection.RightToLeft
                };

                for (int i = 0; i < columns; i++)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                }

                int totalRows = (int)Math.Ceiling((double)optionsList.Count / columns);
                for (int r = 0; r < totalRows; r++)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                for (int index = 0; index < optionsList.Count; index++)
                {
                    var opt = optionsList[index];
                    int row = index / columns;
                    int col = index % columns;

                    var tile = new Border
                    {
                        Padding = new Thickness(6, 10),
                        HeightRequest = 62,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill,
                        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
                        StrokeThickness = 1
                    };
                    tile.SetAppThemeColor(Border.BackgroundColorProperty, AppColors.OptionBackgroundLight, AppColors.OptionBackgroundDark);
                    tile.SetAppThemeColor(Border.StrokeProperty, AppColors.CardBorderLight, AppColors.CardBorderDark);

                    var tileStack = new VerticalStackLayout
                    {
                        Spacing = 4,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    };

                    if (!string.IsNullOrWhiteSpace(opt.IconGlyph))
                    {
                        var iconLabel = new Label
                        {
                            Text = opt.IconGlyph,
                            FontFamily = opt.IconFontFamily ?? Constants.Fonts.KopolArtFaAlias,
                            FontSize = 18,
                            HorizontalOptions = LayoutOptions.Center,
                            HorizontalTextAlignment = TextAlignment.Center
                        };
                        iconLabel.SetAppThemeColor(Label.TextColorProperty, AppColors.BrandPrimaryText, AppColors.BrandAccent);
                        tileStack.Children.Add(iconLabel);
                    }

                    var textLabel = new Label
                    {
                        Text = opt.Text,
                        FontFamily = Constants.Fonts.UiFontBoldAlias,
                        FontSize = 11.5,
                        HorizontalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center,
                        LineBreakMode = LineBreakMode.TailTruncation
                    };
                    textLabel.SetAppThemeColor(Label.TextColorProperty, AppColors.TextPrimaryLight, AppColors.TextPrimaryDark);
                    tileStack.Children.Add(textLabel);

                    tile.Content = tileStack;

                    if (columns == 2 && index == optionsList.Count - 1 && index % 2 == 0)
                    {
                        Grid.SetRow(tile, row);
                        Grid.SetColumn(tile, 0);
                        Grid.SetColumnSpan(tile, 2);
                    }
                    else
                    {
                        Grid.SetRow(tile, row);
                        Grid.SetColumn(tile, col);
                    }

                    var capturedReturn = opt.ReturnValue ?? opt.Text;
                    var tap = new TapGestureRecognizer();
                    tap.Tapped += async (s, e) =>
                    {
                        try
                        {
                            await tile.FadeToAsync(0.5, 40);
                            await tile.FadeToAsync(1.0, 40);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CustomAlertService] Tile highlight animation failed: {ex.Message}");
                        }
                        await context.DismissAsync(() => tcs.TrySetResult(capturedReturn));
                    };
                    tile.GestureRecognizers.Add(tap);

                    grid.Children.Add(tile);
                }

                var containerStack = new VerticalStackLayout
                {
                    Spacing = 8,
                    Margin = new Thickness(0, 4, 0, 0),
                    HorizontalOptions = LayoutOptions.Fill
                };

                containerStack.Children.Add(grid);

                var cancelBtn = new Button
                {
                    Text = cancelText,
                    FontFamily = Constants.Fonts.UiFontMediumAlias,
                    FontSize = 12,
                    HeightRequest = 38,
                    CornerRadius = 12,
                    BackgroundColor = Colors.Transparent,
                    HorizontalOptions = LayoutOptions.Fill
                };
                cancelBtn.SetAppThemeColor(Button.TextColorProperty, AppColors.TextMutedLight, AppColors.TextMutedDark);
                cancelBtn.Clicked += async (s, e) => await context.DismissAsync(() => tcs.TrySetResult(null));
                containerStack.Children.Add(cancelBtn);
                context.CardStack.Children.Add(containerStack);

                var backdropTapGesture = new TapGestureRecognizer();
                backdropTapGesture.Tapped += async (s, e) => await context.DismissAsync(() => tcs.TrySetResult(null));
                context.Backdrop.GestureRecognizers.Add(backdropTapGesture);

                await context.AnimateInAsync();
                return await tcs.Task;
            });
        }

        private async Task<bool> ShowDialogInternalAsync(
            string title,
            string message,
            string acceptText,
            string? cancelText,
            AlertType type,
            bool isDestructive)
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var (currentPage, rootGrid) = AlertCardBuilder.GetCurrentPageRoot();
                if (currentPage == null || rootGrid == null)
                {
                    return false;
                }
                var tcs = new TaskCompletionSource<bool>();
                bool isConfirm = !string.IsNullOrEmpty(cancelText);
                var context = AlertCardBuilder.CreateAlertContext(currentPage, rootGrid, title, message, type, isDestructive, spacing: 14, messageFontSize: 13);
                var buttonsGrid = new Grid
                {
                    Margin = new Thickness(0, 8, 0, 0),
                    ColumnSpacing = 10
                };
                var acceptBtn = new Button
                {
                    Text = acceptText,
                    FontFamily = Constants.Fonts.UiFontBoldAlias,
                    FontSize = 13,
                    HeightRequest = 44,
                    CornerRadius = 14,
                    TextColor = Colors.White,
                    BackgroundColor = isDestructive ? AppColors.StatusRed : AppColors.BrandPrimary
                };
                acceptBtn.Clicked += async (s, e) => await context.DismissAsync(() => tcs.TrySetResult(true));
                if (isConfirm)
                {
                    buttonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                    buttonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                    var cancelBtn = new Button
                    {
                        Text = cancelText,
                        FontFamily = Constants.Fonts.UiFontBoldAlias,
                        FontSize = 13,
                        HeightRequest = 44,
                        CornerRadius = 14,
                        BackgroundColor = Colors.Transparent,
                        BorderWidth = 1
                    };
                    cancelBtn.SetAppThemeColor(Button.TextColorProperty, AppColors.TextSecondaryLight, AppColors.TextSecondaryDark);
                    cancelBtn.SetAppThemeColor(Button.BorderColorProperty, AppColors.CardBorderLight, AppColors.CardBorderDark);
                    cancelBtn.Clicked += async (s, e) => await context.DismissAsync(() => tcs.TrySetResult(false));
                    Grid.SetColumn(acceptBtn, 0);
                    Grid.SetColumn(cancelBtn, 1);
                    buttonsGrid.Children.Add(acceptBtn);
                    buttonsGrid.Children.Add(cancelBtn);
                }
                else
                {
                    buttonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                    Grid.SetColumn(acceptBtn, 0);
                    buttonsGrid.Children.Add(acceptBtn);
                }
                context.CardStack.Children.Add(buttonsGrid);
                var backdropTapGesture = new TapGestureRecognizer();
                backdropTapGesture.Tapped += async (s, e) => await context.DismissAsync(() => tcs.TrySetResult(!isConfirm));
                context.Backdrop.GestureRecognizers.Add(backdropTapGesture);
                await context.AnimateInAsync();
                return await tcs.Task;
            });
        }

        public async Task<DownloadPromptResult?> ShowDownloadLocationPromptAsync(string currentFolderDisplay)
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var (currentPage, rootGrid) = AlertCardBuilder.GetCurrentPageRoot();
                if (currentPage == null || rootGrid == null)
                {
                    return null;
                }

                var tcs = new TaskCompletionSource<DownloadPromptResult?>();
                var context = AlertCardBuilder.CreateAlertContext(
                    currentPage,
                    rootGrid,
                    "محل ذخیره فایل دانلودی",
                    "فایل در پوشه زیر ذخیره خواهد شد. مایلید در همین مسیر ذخیره شود یا پوشه دیگری را انتخاب می‌کنید؟",
                    AlertType.Question,
                    isDestructive: false,
                    spacing: 12,
                    messageFontSize: 13);

                // Path display card
                var pathBorder = new Border
                {
                    StrokeThickness = 1,
                    Padding = new Thickness(12, 10),
                    Margin = new Thickness(0, 4, 0, 0),
                    StrokeShape = new RoundRectangle { CornerRadius = 12 }
                };
                pathBorder.SetAppThemeColor(Border.BackgroundColorProperty, AppColors.OptionBackgroundLight, AppColors.OptionInsetDark);
                pathBorder.SetAppThemeColor(Border.StrokeProperty, AppColors.CardBorderLight, AppColors.CardBorderDark);

                var pathGrid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Auto),
                        new ColumnDefinition(GridLength.Star)
                    },
                    ColumnSpacing = 8,
                    VerticalOptions = LayoutOptions.Center
                };

                var folderIcon = new Label
                {
                    Text = KopolArtFaIcons.FolderOpen,
                    FontFamily = Constants.Fonts.KopolArtFaAlias,
                    FontSize = 16,
                    VerticalOptions = LayoutOptions.Center
                };
                folderIcon.SetAppThemeColor(Label.TextColorProperty, AppColors.BrandPrimary, AppColors.BrandPrimaryDark);

                var pathLabel = new Label
                {
                    Text = currentFolderDisplay,
                    FontFamily = Constants.Fonts.UiFontBoldAlias,
                    FontSize = 12,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    MaxLines = 1,
                    VerticalOptions = LayoutOptions.Center
                };
                pathLabel.SetAppThemeColor(Label.TextColorProperty, AppColors.TextPrimaryLight, AppColors.TextPrimaryDark);

                pathGrid.Add(folderIcon, 0, 0);
                pathGrid.Add(pathLabel, 1, 0);
                pathBorder.Content = pathGrid;
                context.CardStack.Children.Add(pathBorder);

                // "Don't show again" option row
                var dontShowRow = new HorizontalStackLayout
                {
                    Spacing = 8,
                    Margin = new Thickness(2, 6, 2, 2),
                    HorizontalOptions = LayoutOptions.Start,
                    FlowDirection = FlowDirection.RightToLeft,
                    VerticalOptions = LayoutOptions.Center
                };

                var dontShowCheckBox = new CheckBox
                {
                    IsChecked = false,
                    Color = AppColors.BrandPrimary,
                    VerticalOptions = LayoutOptions.Center
                };

                var dontShowLabel = new Label
                {
                    Text = "این پیام دیگر نمایش داده نشود (ذخیره انتخاب)",
                    FontFamily = Constants.Fonts.UiFontMediumAlias,
                    FontSize = 11.5,
                    VerticalOptions = LayoutOptions.Center
                };
                dontShowLabel.SetAppThemeColor(Label.TextColorProperty, AppColors.TextSecondaryLight, AppColors.TextSecondaryDark);

                var toggleGesture = new TapGestureRecognizer();
                toggleGesture.Tapped += (s, e) => dontShowCheckBox.IsChecked = !dontShowCheckBox.IsChecked;
                dontShowLabel.GestureRecognizers.Add(toggleGesture);

                dontShowRow.Children.Add(dontShowCheckBox);
                dontShowRow.Children.Add(dontShowLabel);
                context.CardStack.Children.Add(dontShowRow);

                // Action buttons
                var buttonsStack = new VerticalStackLayout
                {
                    Spacing = 8,
                    Margin = new Thickness(0, 4, 0, 0)
                };

                // Primary: Continue download
                var continueBtn = new Button
                {
                    Text = "دانلود در همین پوشه",
                    FontFamily = Constants.Fonts.UiFontBoldAlias,
                    FontSize = 13,
                    HeightRequest = 42,
                    CornerRadius = 14,
                    TextColor = Colors.White,
                    BackgroundColor = AppColors.BrandPrimary
                };
                continueBtn.Clicked += async (s, e) => await context.DismissAsync(() =>
                    tcs.TrySetResult(new DownloadPromptResult
                    {
                        Action = DownloadPromptAction.Continue,
                        DontShowAgain = dontShowCheckBox.IsChecked
                    }));
                buttonsStack.Children.Add(continueBtn);

                // Secondary: Change folder
                var changeFolderBtn = new Button
                {
                    Text = "انتخاب پوشه دیگر...",
                    FontFamily = Constants.Fonts.UiFontBoldAlias,
                    FontSize = 13,
                    HeightRequest = 42,
                    CornerRadius = 14,
                    BackgroundColor = Colors.Transparent,
                    BorderWidth = 1.5
                };
                changeFolderBtn.SetAppThemeColor(Button.TextColorProperty, AppColors.BrandPrimary, AppColors.BrandPrimaryDark);
                changeFolderBtn.SetAppThemeColor(Button.BorderColorProperty, AppColors.BrandPrimary, AppColors.BrandPrimaryDark);
                changeFolderBtn.Clicked += async (s, e) => await context.DismissAsync(() =>
                    tcs.TrySetResult(new DownloadPromptResult
                    {
                        Action = DownloadPromptAction.ChangeFolder,
                        DontShowAgain = dontShowCheckBox.IsChecked
                    }));
                buttonsStack.Children.Add(changeFolderBtn);

                // Cancel button
                var cancelBtn = new Button
                {
                    Text = "انصراف",
                    FontFamily = Constants.Fonts.UiFontMediumAlias,
                    FontSize = 12,
                    HeightRequest = 36,
                    CornerRadius = 12,
                    BackgroundColor = Colors.Transparent
                };
                cancelBtn.SetAppThemeColor(Button.TextColorProperty, AppColors.TextMutedLight, AppColors.TextMutedDark);
                cancelBtn.Clicked += async (s, e) => await context.DismissAsync(() =>
                    tcs.TrySetResult(new DownloadPromptResult
                    {
                        Action = DownloadPromptAction.Cancel,
                        DontShowAgain = false
                    }));
                buttonsStack.Children.Add(cancelBtn);

                context.CardStack.Children.Add(buttonsStack);

                var backdropTapGesture = new TapGestureRecognizer();
                backdropTapGesture.Tapped += async (s, e) => await context.DismissAsync(() =>
                    tcs.TrySetResult(new DownloadPromptResult
                    {
                        Action = DownloadPromptAction.Cancel,
                        DontShowAgain = false
                    }));
                context.Backdrop.GestureRecognizers.Add(backdropTapGesture);

                await context.AnimateInAsync();
                return await tcs.Task;
            });
        }
    }
}
