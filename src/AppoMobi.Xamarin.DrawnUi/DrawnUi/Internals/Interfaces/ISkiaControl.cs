﻿using System.Collections.Generic;

namespace DrawnUi.Maui.Draw;

public interface ISkiaControl : IDrawnBase
{
    IDrawnBase Parent { get; set; }

    Thickness Margin { get; }
    Thickness Padding { get; }

    void SetParent(IDrawnBase parent);

    /// <summary>
    /// Takes place in layout, acts like is visible, but just not rendering
    /// </summary>
    bool IsGhost { get; }



    Action<SKPath, SKRect> Clipping { get; }

    int ZIndex { get; }

    void OnBeforeDraw();

    void OnBeforeMeasure();

    void Arrange(SKRect destination, float widthRequest, float heightRequest, float scale);

    void Render(SkiaDrawingContext context, SKRect destination, float scale);

    SKRect RenderedAtDestination { get; set; }

    void SetChildren(IEnumerable<ISkiaAttachable> views);



    /// <summary>
    /// Expecting PIXELS as input
    /// sets NeedMeasure to false
    /// </summary>
    /// <param name="widthConstraint"></param>
    /// <param name="heightConstraint"></param>
    /// <returns></returns>
    ScaledSize Measure(float widthConstraint, float heightConstraint, float scale);

    LayoutOptions HorizontalOptions { get; set; }

    LayoutOptions VerticalOptions { get; set; }

    bool CanDraw { get; }
}