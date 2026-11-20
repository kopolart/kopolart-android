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

        // Mapper keys must be real property names: the chained action runs after that
        // property's own mapping (and once at handler creation), never for invented keys.
        static void PreserveLabelFont(Microsoft.Maui.Handlers.ILabelHandler handler, Microsoft.Maui.ILabel view)
        {
            if (view is Microsoft.Maui.Controls.Label label && handler.PlatformView is TextView textView)
            {
                TypefaceCache.ApplyTypeface(textView, label.FontFamily, label.FontAttributes);
                TypefaceCache.AttachFontPreserver(textView, () => label.FontFamily, () => label.FontAttributes);
            }
        }

        LabelHandler.Mapper.AppendToMapping(nameof(Label.FontFamily), PreserveLabelFont);
        LabelHandler.Mapper.AppendToMapping(nameof(Label.FontAttributes), PreserveLabelFont);

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

        static void PreserveButtonFont(Microsoft.Maui.Handlers.IButtonHandler handler, Microsoft.Maui.IButton view)
        {
            if (view is Microsoft.Maui.Controls.Button button && handler.PlatformView is TextView textView)
            {
                TypefaceCache.ApplyTypeface(textView, button.FontFamily, button.FontAttributes);
                TypefaceCache.AttachFontPreserver(textView, () => button.FontFamily, () => button.FontAttributes);
            }
        }

        ButtonHandler.Mapper.AppendToMapping(nameof(Microsoft.Maui.Controls.Button.FontFamily), PreserveButtonFont);
        ButtonHandler.Mapper.AppendToMapping(nameof(Microsoft.Maui.Controls.Button.FontAttributes), PreserveButtonFont);

        static void PreserveEntryFont(Microsoft.Maui.Handlers.IEntryHandler handler, Microsoft.Maui.IEntry view)
        {
            if (view is Microsoft.Maui.Controls.Entry entry && handler.PlatformView is TextView textView)
            {
                TypefaceCache.ApplyTypeface(textView, entry.FontFamily, entry.FontAttributes);
                TypefaceCache.AttachFontPreserver(textView, () => entry.FontFamily, () => entry.FontAttributes);
            }
        }

        EntryHandler.Mapper.AppendToMapping(nameof(Entry.FontFamily), PreserveEntryFont);
        EntryHandler.Mapper.AppendToMapping(nameof(Entry.FontAttributes), PreserveEntryFont);

        static void PreserveEditorFont(Microsoft.Maui.Handlers.IEditorHandler handler, Microsoft.Maui.IEditor view)
        {
            if (view is Microsoft.Maui.Controls.Editor editor && handler.PlatformView is TextView textView)
            {
                TypefaceCache.ApplyTypeface(textView, editor.FontFamily, editor.FontAttributes);
                TypefaceCache.AttachFontPreserver(textView, () => editor.FontFamily, () => editor.FontAttributes);
            }
        }

        EditorHandler.Mapper.AppendToMapping(nameof(Editor.FontFamily), PreserveEditorFont);
        EditorHandler.Mapper.AppendToMapping(nameof(Editor.FontAttributes), PreserveEditorFont);
    }
}
#endif