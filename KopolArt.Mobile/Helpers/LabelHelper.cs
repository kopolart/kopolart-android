namespace KopolArt.Mobile.Helpers
{
    /// <summary>
    /// Attached properties for MAUI Label controls.
    /// Provides justification support on Android (API 26+) via TextView.JustificationMode.
    /// </summary>
    public static class LabelHelper
    {
        public static readonly BindableProperty JustifyTextProperty =
            BindableProperty.CreateAttached(
                "JustifyText",
                typeof(bool),
                typeof(LabelHelper),
                false,
                propertyChanged: OnJustifyTextChanged);

        public static bool GetJustifyText(BindableObject view) => (bool)view.GetValue(JustifyTextProperty);
        public static void SetJustifyText(BindableObject view, bool value) => view.SetValue(JustifyTextProperty, value);

        private static void OnJustifyTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is Label label)
            {
#if ANDROID
                ApplyJustification(label, (bool)newValue);

                label.HandlerChanged -= OnLabelHandlerChanged;
                if ((bool)newValue)
                {
                    label.HandlerChanged += OnLabelHandlerChanged;
                }
#endif
            }
        }

#if ANDROID
        private static void OnLabelHandlerChanged(object? sender, EventArgs e)
        {
            if (sender is Label label)
            {
                ApplyJustification(label, GetJustifyText(label));
            }
        }

        public static void ApplyJustification(Label label, bool justify)
        {
            if (label.Handler?.PlatformView is Android.Widget.TextView textView)
            {
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                {
                    textView.JustificationMode = justify
                        ? Android.Text.JustificationMode.InterWord
                        : Android.Text.JustificationMode.None;
                }
            }
        }
#endif
    }
}
