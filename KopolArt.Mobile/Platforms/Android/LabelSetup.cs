#if ANDROID
using Android.Text;
using Android.Widget;
using KopolArt.Mobile.Helpers;
using Microsoft.Maui.Handlers;

namespace KopolArt.Mobile.Platforms.Android;

public static class LabelSetup
{
    public static void ConfigureLabel()
    {
        TypefaceCache.Initialize();

        // 1. Label font preservation & justification
        LabelHandler.Mapper.AppendToMapping("FontPreserve", (handler, view) =>
        {
            if (view is Microsoft.Maui.Controls.Label label && handler.PlatformView is TextView textView)
            {
                TypefaceCache.ApplyTypeface(textView, label.FontFamily, label.FontAttributes);
                TypefaceCache.AttachFontPreserver(textView, () => label.FontFamily, () => label.FontAttributes);
            }
        });

        LabelHandler.Mapper.AppendToMapping("JustifyText", (handler, view) =>
        {
            if (view is Microsoft.Maui.Controls.Label label && handler.PlatformView is TextView textView)
            {
                if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                {
                    textView.JustificationMode = LabelHelper.GetJustifyText(label)
                        ? JustificationMode.InterWord
                        : JustificationMode.None;
                }
            }
        });

        // 2. Button font preservation
        ButtonHandler.Mapper.AppendToMapping("FontPreserve", (handler, view) =>
        {
            if (view is Microsoft.Maui.Controls.Button button && handler.PlatformView is TextView textView)
            {
                TypefaceCache.ApplyTypeface(textView, button.FontFamily, button.FontAttributes);
                TypefaceCache.AttachFontPreserver(textView, () => button.FontFamily, () => button.FontAttributes);
            }
        });

        // 3. Entry font preservation
        EntryHandler.Mapper.AppendToMapping("FontPreserve", (handler, view) =>
        {
            if (view is Microsoft.Maui.Controls.Entry entry && handler.PlatformView is TextView textView)
            {
                TypefaceCache.ApplyTypeface(textView, entry.FontFamily, entry.FontAttributes);
                TypefaceCache.AttachFontPreserver(textView, () => entry.FontFamily, () => entry.FontAttributes);
            }
        });

        // 4. Editor font preservation
        EditorHandler.Mapper.AppendToMapping("FontPreserve", (handler, view) =>
        {
            if (view is Microsoft.Maui.Controls.Editor editor && handler.PlatformView is TextView textView)
            {
                TypefaceCache.ApplyTypeface(textView, editor.FontFamily, editor.FontAttributes);
                TypefaceCache.AttachFontPreserver(textView, () => editor.FontFamily, () => editor.FontAttributes);
            }
        });
    }
}
#endif
