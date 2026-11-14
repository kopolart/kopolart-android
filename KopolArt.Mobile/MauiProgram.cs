using KopolArt.Mobile.Extensions;
using KopolArt.Mobile.Platforms.Android.Extensions;

namespace KopolArt.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont(Constants.Fonts.UiFontMediumFile, Constants.Fonts.UiFontMediumAlias);
                    fonts.AddFont(Constants.Fonts.UiFontMediumFile, Constants.Fonts.UiFontRegularAlias);
                    fonts.AddFont(Constants.Fonts.UiFontBoldFile, Constants.Fonts.UiFontBoldAlias);
                    fonts.AddFont(Constants.Fonts.UiFontBlackFile, Constants.Fonts.UiFontBlackAlias);
                    fonts.AddFont(Constants.Fonts.KopolArtFaFont, Constants.Fonts.KopolArtFaAlias);
                });

            Platforms.Android.WebViewSetup.ConfigureCustomWebView();
            Platforms.Android.CollectionViewSetup.ConfigureCollectionView();
            Platforms.Android.LabelSetup.ConfigureLabel();

            // Dependency Injection
            builder.Services
                .AddPlatformServices()
                .AddCoreServices()
                .AddViewModels()
                .AddViews();

            ConfigureGlobalExceptionHandlers();

            return builder.Build();
        }

        private static void ConfigureGlobalExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                System.Diagnostics.Debug.WriteLine($"[FATAL] Unhandled exception: {ex}");
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"[WARN] Unobserved task exception: {args.Exception}");
                args.SetObserved();
            };

            Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"[FATAL] Android unhandled exception: {args.Exception}");
                args.Handled = false;
            };
        }
    }
}
