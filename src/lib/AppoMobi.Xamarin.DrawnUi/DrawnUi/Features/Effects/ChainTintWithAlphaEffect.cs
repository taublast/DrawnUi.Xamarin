﻿using DrawnUi.Maui.Infrastructure.Extensions;
using Color = Xamarin.Forms.Color;

namespace DrawnUi.Maui.Draw;

public class ChainTintWithAlphaEffect : BaseChainedEffect
{

    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color),
        typeof(Color),
        typeof(SkiaImage),
        Color.Red,
        propertyChanged: NeedUpdate);

    public Color Color
    {
        get { return (Color)GetValue(ColorProperty); }
        set { SetValue(ColorProperty, value); }
    }

    public static readonly BindableProperty AlphaProperty = BindableProperty.Create(
        nameof(Alpha),
        typeof(double),
        typeof(SkiaImage),
        1.0,
        propertyChanged: NeedUpdate);

    public double Alpha
    {
        get { return (double)GetValue(AlphaProperty); }
        set { SetValue(AlphaProperty, value); }
    }

    public static readonly BindableProperty EffectBlendModeProperty = BindableProperty.Create(
        nameof(EffectBlendMode),
        typeof(SKBlendMode),
        typeof(SkiaImage),
        SKBlendMode.SrcATop,
        propertyChanged: NeedUpdate);

    public SKBlendMode EffectBlendMode
    {
        get { return (SKBlendMode)GetValue(EffectBlendModeProperty); }
        set { SetValue(EffectBlendModeProperty, value); }
    }

    public override ChainEffectResult Draw(SKRect destination, SkiaDrawingContext ctx, Action<SkiaDrawingContext> drawControl)
    {
        if (NeedApply)
        {
            if (Paint == null)
            {
                Paint = new()
                {
                    ColorFilter = SkiaImageEffects.Tint(Color.WithAlpha((float)Alpha), EffectBlendMode)
                };
            }

            var restore = ctx.Canvas.SaveLayer(Paint);

            drawControl(ctx);

            return ChainEffectResult.Create(true, restore);

        }
        return base.Draw(destination, ctx, drawControl);
    }

    public override bool NeedApply
    {
        get
        {
            return base.NeedApply && (this.Color != Color.Transparent);
        }
    }
}