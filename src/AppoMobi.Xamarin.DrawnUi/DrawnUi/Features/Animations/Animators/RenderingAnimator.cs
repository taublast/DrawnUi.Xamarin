﻿namespace DrawnUi.Maui.Draw;

/// <summary>
/// This animator renders on canvas instead of just updating a value
/// </summary>
public class RenderingAnimator : SkiaValueAnimator, IOverlayEffect
{

    public bool Render(IDrawnBase control, SkiaDrawingContext context, double scale)
    {
        return OnRendering(control, context, scale);
    }

    /// <summary>
    /// return true if has drawn something and rendering needs to be applied
    /// </summary>
    /// <param name="control"></param>
    /// <param name="context"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    protected virtual bool OnRendering(IDrawnBase control, SkiaDrawingContext context, double scale)
    {
        return false;
    }


    public RenderingAnimator(IDrawnBase parent) : base(parent)
    {

    }

    protected static SKPoint GetSelfDrawingLocation(IDrawnBase control)
    {
        SKPoint position;
        if (control is SkiaControl skia)
        {
            position = skia.GetSelfDrawingPosition();
        }
        else
        {
            //legacy case for drawnview
            position = new SKPoint(
                (float)(control.X * control.RenderingScale),
                (float)(control.Y * control.RenderingScale)
                );
        }
        return position;
    }

    protected static void DrawWithClipping(SkiaDrawingContext context, IDrawnBase control, SKPoint selfDrawingLocation, Action draw)
    {
        if (control.ClipEffects)
        {
            using (SKPath clipInsideParent = new SKPath())
            {
                ApplyControlClipping(control, clipInsideParent, selfDrawingLocation);

                context.Canvas.Save();
                context.Canvas.ClipPath(clipInsideParent, SKClipOperation.Intersect, true);

                draw();

                context.Canvas.Restore();
            }
        }
        else
        {
            draw();
        }
    }

    protected static void ApplyControlClipping(IDrawnBase control, SKPath clipInsideParent, SKPoint selfDrawingLocation)
    {
        SKPath clipContent;
        if (control is SkiaControl skia)
        {
            clipContent = skia.CreateClip(null, false);
            clipContent.Offset(selfDrawingLocation);
            clipInsideParent.AddPath(clipContent);
        }
        else
        {
            //legacy case for drawnview
            clipContent = control.CreateClip(null, true);
            clipContent.Offset((float)(control.TranslationX * control.RenderingScale), (float)(control.TranslationY * control.RenderingScale));
            clipInsideParent.AddPath(clipContent);
        }
    }
}