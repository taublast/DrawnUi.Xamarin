using Android.App;
using Android.Glide;
using Android.Graphics;
using Android.Runtime;
using Bumptech.Glide;
using DrawnUi.Maui.Draw;
using SkiaSharp;
using SkiaSharp.Views.Android;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(AppoMobi.Xamarin.DrawnUi.Droid.DrawnUi))]

namespace AppoMobi.Xamarin.DrawnUi.Droid
{
    [Preserve(AllMembers = true)]
    public class DrawnUi : IDrawnUiPlatform
    {
        public DrawnUi()
        {

        }

        public static void Initialize(Activity activity)
        {
            Activity = activity;

            if (!DisableCache)
                Android.Glide.Forms.Init(activity);
        }

        public static Activity Activity { get; protected set; }

        public static bool DisableCache;

        async Task<SKBitmap> IDrawnUiPlatform.LoadSKBitmapAsync(ImageSource source, CancellationToken cancel)
        {
            if (source == null)
                return null;

            Bitmap androidBitmap = null;
            try
            {

                if (DisableCache)
                {

                    var handler = source.GetHandler();
                    androidBitmap = await handler.LoadImageAsync(source, Android.App.Application.Context, cancel);

                }
                else
                {
                    androidBitmap = await source.LoadOriginalViaGlide(Android.App.Application.Context, cancel);
                }

                if (androidBitmap != null)
                {
                    return androidBitmap.ToSKBitmap();
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
            if (DisableCache)
                return;

            var glide = Glide.Get(Activity);

            Task.Run(async () =>
            {
                glide.ClearDiskCache();
            }).ConfigureAwait(false);

            Device.BeginInvokeOnMainThread(() =>
            {
                glide.ClearMemory();
            });
        }



    }
}