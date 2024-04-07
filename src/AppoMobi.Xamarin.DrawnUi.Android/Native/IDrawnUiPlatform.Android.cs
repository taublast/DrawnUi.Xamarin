using Android.App;
using Android.Glide;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using AppoMobi.Specials;
using Bumptech.Glide;
using DrawnUi.Maui.Draw;
using SkiaSharp;
using SkiaSharp.Views.Android;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
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

            Tasks.StartDelayed(TimeSpan.FromMilliseconds(250), async () =>
            {
                _frameCallback = new FrameCallback((nanos) =>
                {
                    ChoreographerCallback?.Invoke(null, null);
                    Choreographer.Instance.PostFrameCallback(_frameCallback);
                });

                while (!_loopStarted)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if (_loopStarting)
                            return;
                        _loopStarting = true;

                        if (MainThread.IsMainThread) //Choreographer is available
                        {
                            if (!_loopStarted)
                            {
                                _loopStarted = true;
                                Choreographer.Instance.PostFrameCallback(_frameCallback);
                            }
                        }
                        _loopStarting = false;
                    });
                    await Task.Delay(100);
                }
            });

        }

        private static FrameCallback _frameCallback;
        static bool _loopStarting = false;
        static bool _loopStarted = false;
        public static event EventHandler ChoreographerCallback;

        public void RegisterLooperCallback(EventHandler callback)
        {
            ChoreographerCallback += callback;
        }

        public void UnregisterLooperCallback(EventHandler callback)
        {
            ChoreographerCallback -= callback;
        }

        public static Activity Activity { get; protected set; }

        public static bool DisableCache;

        async Task<SKBitmap> IDrawnUiPlatform.LoadImageOnPlatformAsync(ImageSource source, CancellationToken cancel)
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

        public class FrameCallback : Java.Lang.Object, Choreographer.IFrameCallback
        {
            public FrameCallback(Action<long> callback)
            {
                _callback = callback;
            }

            Action<long> _callback;

            public void DoFrame(long frameTimeNanos)
            {
                _callback?.Invoke(frameTimeNanos);
            }

        }
    }
}