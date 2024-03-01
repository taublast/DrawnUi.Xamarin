﻿using DrawnUi.Maui.Views;
using Xamarin.Essentials;

namespace DrawnUi.Maui.Draw;

public class SkiaDrawingContext
{
    public SKCanvas Canvas { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public long FrameTimeNanos { get; set; }
    public DrawnView Superview { get; set; }
    public string Tag { get; set; }

    public SkiaDrawingContext Clone()
    {
        return new SkiaDrawingContext()
        {
            Superview = Superview,
            Width = Width,
            Height = Height,
            Canvas = this.Canvas,
            FrameTimeNanos = this.FrameTimeNanos,
        };
    }

    public static float DeviceDensity
    {
        get
        {
            return (float)DeviceDisplay.MainDisplayInfo.Density;
        }
    }


}