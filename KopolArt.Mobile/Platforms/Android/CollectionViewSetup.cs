#if ANDROID
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls.Handlers.Items;

namespace KopolArt.Mobile.Platforms.Android;

public static class CollectionViewSetup
{
    public static void ConfigureCollectionView()
    {
        CollectionViewHandler.Mapper.AppendToMapping("ShadowClippingFix", (handler, view) =>
        {
            if (handler.PlatformView is RecyclerView recyclerView)
            {
                recyclerView.SetClipChildren(false);
                recyclerView.SetClipToPadding(false);
                recyclerView.AddOnChildAttachStateChangeListener(new RecyclerViewChildAttachListener());
            }
        });

        CarouselViewHandler.Mapper.AppendToMapping("ShadowClippingFix", (handler, view) =>
        {
            if (handler.PlatformView is RecyclerView recyclerView)
            {
                recyclerView.SetClipChildren(false);
                recyclerView.SetClipToPadding(false);
                recyclerView.AddOnChildAttachStateChangeListener(new RecyclerViewChildAttachListener());
            }
        });

        Microsoft.Maui.Handlers.ScrollViewHandler.Mapper.AppendToMapping("ShadowClippingFix", (handler, view) =>
        {
            if (handler.PlatformView is global::Android.Views.ViewGroup vg)
            {
                vg.SetClipChildren(false);
                vg.SetClipToPadding(false);

                if (view.FlowDirection == FlowDirection.RightToLeft)
                {
                    vg.LayoutDirection = global::Android.Views.LayoutDirection.Rtl;
                }
            }
        });
    }

    private sealed class RecyclerViewChildAttachListener : Java.Lang.Object, RecyclerView.IOnChildAttachStateChangeListener
    {
        public void OnChildViewAttachedToWindow(global::Android.Views.View view)
        {
            DisableClipping(view);
        }

        public void OnChildViewDetachedFromWindow(global::Android.Views.View view)
        {
        }

        private static void DisableClipping(global::Android.Views.View view)
        {
            if (view is global::Android.Views.ViewGroup vg)
            {
                vg.SetClipChildren(false);
                vg.SetClipToPadding(false);
                for (int i = 0; i < vg.ChildCount; i++)
                {
                    var child = vg.GetChildAt(i);
                    if (child != null)
                    {
                        DisableClipping(child);
                    }
                }
            }
        }
    }
}
#endif
