using DrawnUi.Maui.Draw;
using SkiaSharp;
using SkiaSharp.Views.iOS;
using System;
using System.Threading;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.iOS;

[assembly: Xamarin.Forms.Dependency(typeof(AppoMobi.Xamarin.DrawnUi.Apple.DrawnUi))]
namespace AppoMobi.Xamarin.DrawnUi.Apple
{
    [Preserve(AllMembers = true)]
    public class DrawnUi : IDrawnUiPlatform
    {
        public DrawnUi()
        {

        }

        public static void Initialize()
        {

        }

        public static bool DisableCache;

        async Task<SKBitmap> IDrawnUiPlatform.LoadSKBitmapAsync(ImageSource source, CancellationToken cancel)
        {
            if (source == null)
                return null;

            UIImage iosImage = null;
            try
            {
                if (true) //DisableCache
                {
                    var handler = GetHandler(source);
                    iosImage = await handler.LoadImageAsync(source, cancel, 1.0f);
                }
                else
                {
                    //iosImage = await NukeHelper.LoadViaNuke(source, cancel, 1.0f);
                }

                if (iosImage != null)
                {
                    return iosImage.ToSKBitmap();
                }
            }
            catch (Exception e)
            {
                Super.Log($"[LoadSKBitmapAsync] {e}");
            }

            return null;
        }

        void IDrawnUiPlatform.ClearImagesCache()
        {
            //NukeHelper.ClearCache();
        }

        public static IImageSourceHandler? GetHandler(ImageSource source)
        {
            //Image source handler to return
            IImageSourceHandler? returnValue = null;
            //check the specific source type and return the correct image source handler
            if (source is UriImageSource)
            {
                returnValue = new ImageLoaderSourceHandler();
            }
            else if (source is FileImageSource)
            {
                returnValue = new FileImageSourceHandler();
            }
            else if (source is StreamImageSource)
            {
                returnValue = new StreamImagesourceHandler();
            }
            else if (source is FontImageSource)
            {
                returnValue = new FontImageSourceHandler();
            }

            return returnValue;
        }

    }
}