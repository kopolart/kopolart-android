using Android.Webkit;
using Java.Interop;

namespace KopolArt.Mobile.Platforms.Android
{
    public class BlobDownloadBridge(CustomDownloadListener downloadListener) : Java.Lang.Object
    {
        [JavascriptInterface]
        [Export("processBlob")]
        public void ProcessBlob(string base64Data, string contentDisposition, string mimeType)
        {
            downloadListener.HandleBlobBase64(base64Data, contentDisposition, mimeType);
        }
    }
}
