﻿using DrawnUi.Maui.Infrastructure.Enums;
using Xamarin.Essentials;


namespace DrawnUi.Maui.Views;


public partial class DrawnView
{

    public bool IsDirty
    {
        get
        {
            return _isDirty;
        }

        set
        {
            if (_isDirty != value)
            {
                _isDirty = value;
                OnPropertyChanged();
            }
        }
    }
    bool _isDirty;

    public bool CheckCanDraw()
    {
        if (UpdateLocked && StopDrawingWhenUpdateIsLocked)
            return false;

        return CanvasView != null
               && !IsRendering
               && IsDirty
               && IsVisible;
    }


    public bool NeedRedraw { get; set; }

    public virtual void Update()
    {
        IsDirty = true;
        if (!OrderedDraw && CheckCanDraw())
        {
            OrderedDraw = true;
            InvalidateCanvas();
        }
    }

    protected async void InvalidateCanvas()
    {
        if (Device.RuntimePlatform == Device.Android)
        {
            InvalidateCanvasAndroid();
        }
        else
        {
            InvalidateCanvasPlatform();
        }
    }

    protected async void InvalidateCanvasAndroid()
    {
        if (CanvasView == null)
            return;

        //sanity check
        if (CanvasView.CanvasSize is { Width: > 0, Height: > 0 })
        {
            if (NeedCheckParentVisibility)
            {
                CheckElementVisibility(this);
            }

            if (UpdateMode == UpdateMode.Constant)
            {
                InvalidatedCanvas++;
                CanvasView?.InvalidateSurface();
                return;
            }

            if (CanDraw) //passed checks
            {
                _isWaiting = true;
                InvalidatedCanvas++;

                //cap fps around 120fps
                var nowNanos = Super.GetCurrentTimeNanos();
                var elapsedMicros = (nowNanos - CanvasView.FrameTime) / 1_000.0;

                var needWait =
                    Super.CapMicroSecs / 2f //do not ask why
                    - elapsedMicros;
                if (needWait >= 1)
                {
                    var ms = (int)(needWait / 1000);
                    if (ms < 1)
                        ms = 1;
                    await Task.Delay(ms);
                }
                else
                {
                    await Task.Delay(1);
                }
                _isWaiting = false;

                if (!Super.EnableRendering)
                {
                    OrderedDraw = false;
                    return;
                }

                CanvasView?.InvalidateSurface();

            }
            else
            {
                OrderedDraw = false;
            }

        }
        else
        {
            OrderedDraw = false;
            await Task.Delay(30);
            await Task.Run(() =>
            {
                Update();
            }).ConfigureAwait(false);
        }
    }

    protected async void InvalidateCanvasPlatform()
    {
        IsDirty = true;

        if (CanvasView == null)
        {
            OrderedDraw = false;
            return;
        }

        if (CanvasView.CanvasSize is { Width: > 0, Height: > 0 })
        {
            if (_isWaiting)  //busy
            {
                NeedRedraw = true;
                OrderedDraw = false;
            }
            else
            {
                NeedRedraw = false;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _isWaiting = true;
                    try
                    {
                        if (NeedCheckParentVisibility)
                            CheckElementVisibility(this);

                        if (UpdateMode == UpdateMode.Constant)
                        {
                            InvalidatedCanvas++;
                            CanvasView?.InvalidateSurface();
                            return;
                        }

                        if (!CanvasView.IsDrawing && CanDraw)  //passed checks
                        {
                            InvalidatedCanvas++;

                            //cap fps around 120fps
                            var nowNanos = Super.GetCurrentTimeNanos();
                            var elapsedMicros = (nowNanos - CanvasView.FrameTime) / 1_000.0;

                            var needWait =
                                Super.CapMicroSecs
                                - elapsedMicros;
                            if (needWait >= 1)
                            {
                                var ms = (int)(needWait / 1000);
                                if (ms < 1)
                                    ms = 1;
                                await Task.Delay(ms);
                            }
                            else
                            {
                                if (Device.RuntimePlatform != Device.Android)
                                    await Task.Delay(1); //unlock threads
                            }

                            if (!Super.EnableRendering)
                            {
                                OrderedDraw = false;
                                return;
                            }
                            CanvasView?.InvalidateSurface(); //very rarely could throw on windows here if maui destroys view when navigating, so we secured with try-catch

                            return;
                        }

                        OrderedDraw = false;

                    }
                    catch (Exception e)
                    {
                        Super.Log(e);
                    }
                    finally
                    {
                        _isWaiting = false;
                        if (NeedRedraw) //if we missed previous update
                        {
                            NeedRedraw = false;
                            Task.Run(() =>
                            {
                                Update();
                            }).ConfigureAwait(false);
                        }
                    }
                });
            }
        }
        else
        {
            OrderedDraw = false; //canvas not created yet
            await Task.Delay(30);
            await Task.Run(() =>
            {
                Update();
            }).ConfigureAwait(false);
        }
    }


}