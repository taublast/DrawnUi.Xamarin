﻿using AppoMobi.Forms.Gestures;
using AppoMobi.Specials;
using DrawnUi.Maui.Draw;
using DrawnUi.Maui.Infrastructure.Extensions;
using ExCSS;
using SkiaSharp.Views.Forms;
using System.Windows.Input;
using Color = Xamarin.Forms.Color;


namespace DrawnUi.Maui.Draw
{
    public class SkiaHotspot : SkiaControl, ISkiaGestureListener
    {
        public SkiaHotspot()
        {
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Fill;
            //BackgroundColor = Color.Parse("#33ff0000");
        }


        protected override void Draw(SkiaDrawingContext context, SKRect destination, float scale)
        {
            var canvas = context.Canvas;

            if (IsRootView() && !IsOverlay)
                canvas.Clear(ClearColor.ToSKColor());

            Arrange(destination, SizeRequest.Width, SizeRequest.Height, scale);

            if (!CheckIsGhost())
            {
                DrawWithClipAndTransforms(context, DrawingRect, true, true, (ctx) =>
                {
                    PaintTintBackground(canvas, DrawingRect);
                    //Paint(ctx, DrawingRect, scale, CreatePaintArguments());
                });

                FinalizeDraw(context, scale);
            }
        }


        public override ISkiaGestureListener ProcessGestures(TouchActionType type, TouchActionEventArgs args,
            TouchActionResult touchAction, SKPoint childOffset, SKPoint childOffsetDirect, ISkiaGestureListener alreadyConsumed)
        {
            //Trace.WriteLine($"SkiaHotspot. {type} {touchAction} {args.Location.X} {args.Location.Y}");

            if (touchAction == TouchActionResult.Down)
            {
                TotalDown++;
                Down?.Invoke(this, args);
            }
            else
            if (touchAction == TouchActionResult.Up)
            {
                Up?.Invoke(this, args);
            }
            else
            if (touchAction == TouchActionResult.Tapped)
            {
                var consumed = false;

                if (Tapped != null)
                {
                    consumed = true;
                    Tapped.Invoke(this, args);
                }

                TotalTapped++;
                var delay = 10;

                var x = (args.Location.X) / RenderingScale;
                var y = (args.Location.Y) / RenderingScale;

                if (this.AnimationTapped != SkiaTouchAnimation.None)
                {

                    var control = this as SkiaControl;
                    if (this.TransformView is SkiaControl other)
                    {
                        control = other;
                    }

                    if (AnimationTapped == SkiaTouchAnimation.Ripple)
                    {
                        var ptsInsideControl = GetOffsetInsideControlInPoints(args.Location, childOffset);
                        control.PlayRippleAnimation(TouchEffectColor, ptsInsideControl.X, ptsInsideControl.Y);

                        delay = DelayCallbackMs;
                    }
                    else
                    if (AnimationTapped == SkiaTouchAnimation.Shimmer)
                    {
                        var color = ShimmerEffectColor;
                        control.PlayShimmerAnimation(color, ShimmerEffectWidth, ShimmerEffectAngle, ShimmerEffectSpeed);
                        delay = DelayCallbackMs;
                    }
                }

                if (CommandTapped != null)
                {
                    consumed = true;
                    Tasks.StartDelayedAsync(TimeSpan.FromMilliseconds(delay), async () =>
                    {
                        await Task.Run(() => { CommandTapped?.Execute(CommandTappedParameter); }).ConfigureAwait(false);
                    });
                }

                return consumed ? this : null;
            }
            //do not need to call base we have no children

            return null;
        }


        private long _TotalTapped;
        public long TotalTapped
        {
            get
            {
                return _TotalTapped;
            }
            set
            {
                if (_TotalTapped != value)
                {
                    _TotalTapped = value;
                    OnPropertyChanged();
                }
            }
        }

        private long _TotalDown;
        public long TotalDown
        {
            get
            {
                return _TotalDown;
            }
            set
            {
                if (_TotalDown != value)
                {
                    _TotalDown = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// You might want to pause to show effect before executing command. Default is 0.
        /// </summary>
        public static int DelayCallbackMs = 0;

        public event EventHandler<TouchActionEventArgs> Up;

        public event EventHandler<TouchActionEventArgs> Down;

        public event EventHandler<TouchActionEventArgs> Tapped;

        #region PROPERTIES


        public virtual void OnFocusChanged(bool focus)
        { }

        public static readonly BindableProperty ShimmerEffectColorProperty = BindableProperty.Create(nameof(ShimmerEffectColor),
            typeof(Color),
            typeof(SkiaHotspot),
            Color.White.WithAlpha(0.33f));
        public Color ShimmerEffectColor
        {
            get { return (Color)GetValue(ShimmerEffectColorProperty); }
            set { SetValue(ShimmerEffectColorProperty, value); }
        }

        public static readonly BindableProperty ShimmerEffectAngleProperty = BindableProperty.Create(nameof(ShimmerEffectAngle),
            typeof(float),
            typeof(SkiaHotspot),
            33.0f);
        public float ShimmerEffectAngle
        {
            get { return (float)GetValue(ShimmerEffectAngleProperty); }
            set { SetValue(ShimmerEffectAngleProperty, value); }
        }

        public static readonly BindableProperty ShimmerEffectWidthProperty = BindableProperty.Create(nameof(ShimmerEffectWidth),
            typeof(float),
            typeof(SkiaHotspot),
            150.0f);
        public float ShimmerEffectWidth
        {
            get { return (float)GetValue(ShimmerEffectWidthProperty); }
            set { SetValue(ShimmerEffectWidthProperty, value); }
        }

        public static readonly BindableProperty ShimmerEffectSpeedProperty = BindableProperty.Create(nameof(ShimmerEffectSpeed),
            typeof(int),
            typeof(SkiaHotspot),
            500);
        public int ShimmerEffectSpeed
        {
            get { return (int)GetValue(ShimmerEffectSpeedProperty); }
            set { SetValue(ShimmerEffectSpeedProperty, value); }
        }


        public static readonly BindableProperty TouchEffectColorProperty = BindableProperty.Create(nameof(TouchEffectColor), typeof(Color),
             typeof(SkiaHotspot),
            Color.White);
        public Color TouchEffectColor
        {
            get { return (Color)GetValue(TouchEffectColorProperty); }
            set { SetValue(TouchEffectColorProperty, value); }
        }

        public static readonly BindableProperty AnimationTappedProperty = BindableProperty.Create(nameof(AnimationTapped),
            typeof(SkiaTouchAnimation),
            typeof(SkiaHotspot), SkiaTouchAnimation.None);
        public SkiaTouchAnimation AnimationTapped
        {
            get { return (SkiaTouchAnimation)GetValue(AnimationTappedProperty); }
            set { SetValue(AnimationTappedProperty, value); }
        }

        public static readonly BindableProperty TransformViewProperty = BindableProperty.Create(nameof(TransformView), typeof(object),
            typeof(SkiaHotspot), null);
        public object TransformView
        {
            get { return (object)GetValue(TransformViewProperty); }
            set { SetValue(TransformViewProperty, value); }
        }

        public static readonly BindableProperty CommandTappedProperty = BindableProperty.Create(nameof(CommandTapped), typeof(ICommand),
            typeof(SkiaHotspot),
            null);
        public ICommand CommandTapped
        {
            get { return (ICommand)GetValue(CommandTappedProperty); }
            set { SetValue(CommandTappedProperty, value); }
        }

        public static readonly BindableProperty CommandTappedParameterProperty = BindableProperty.Create(nameof(CommandTappedParameter), typeof(object),
            typeof(SkiaHotspot),
            null);
        public object CommandTappedParameter
        {
            get { return GetValue(CommandTappedParameterProperty); }
            set { SetValue(CommandTappedParameterProperty, value); }
        }

        public static readonly BindableProperty CommandLongPressingProperty = BindableProperty.Create(nameof(CommandLongPressing), typeof(ICommand),
            typeof(SkiaHotspot),
            null);
        public ICommand CommandLongPressing
        {
            get { return (ICommand)GetValue(CommandLongPressingProperty); }
            set { SetValue(CommandLongPressingProperty, value); }
        }

        public static readonly BindableProperty CommandLongPressingParameterProperty = BindableProperty.Create(nameof(CommandLongPressingParameter), typeof(object),
            typeof(SkiaHotspot),
            null);
        public object CommandLongPressingParameter
        {
            get { return GetValue(CommandLongPressingParameterProperty); }
            set { SetValue(CommandLongPressingParameterProperty, value); }
        }


        #endregion

    }
}
