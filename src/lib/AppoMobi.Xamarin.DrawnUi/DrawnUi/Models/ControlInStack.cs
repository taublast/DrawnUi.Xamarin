﻿using System.Numerics;

namespace DrawnUi.Maui.Draw;

public class ControlInStack
{
    public ControlInStack()
    {
        Drawn = new();
        Destination = new();
        Area = new();
    }

    /// <summary>
    /// Index inside enumerator that was passed for measurement OR index inside ItemsSource
    /// </summary>
    public int ControlIndex { get; set; }

    /// <summary>
    /// Measure result
    /// </summary>
    public ScaledSize Measured { get; set; }

    /// <summary>
    /// Available area for Arrange
    /// </summary>
    public SKRect Area { get; set; }

    /// <summary>
    /// PIXELS
    /// </summary>
    public SKRect Destination { get; set; }

    /// <summary>
    /// This will be null for recycled views
    /// </summary>
    public SkiaControl View { get; set; }

    /// <summary>
    /// Was used for actual drawing
    /// </summary>
    public DrawingRect Drawn { get; set; }

    /// <summary>
    /// For internal use by your custom controls
    /// </summary>
    public Vector2 Offset { get; set; }

    public bool IsVisible { get; set; }

    public int ZIndex { get; set; }
}