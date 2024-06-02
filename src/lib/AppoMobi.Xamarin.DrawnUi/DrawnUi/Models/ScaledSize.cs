﻿using System.Runtime.CompilerServices;

namespace DrawnUi.Maui.Draw;

public struct RangeF
{
    public float Start { get; set; }
    public float End { get; set; }

    public RangeF(float start, float end)
    {
        Start = start;
        End = end;
    }

    public float Length => Math.Abs(End - Start);

    public float Delta => End - Start;
}



public class ScaledSize
{

    /*
     
     
    #region COMPARE

    public static bool operator ==(ScaledSize left, ScaledSize right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ScaledSize left, ScaledSize right)
    {
        return !left.Equals(right);
    }

    public override bool Equals(object obj)
    {
        if (obj is ScaledSize other)
        {
            return Scale == other.Scale && Pixels == other.Pixels;
            // Units == other.Units &&
            //Pixels == other.Pixels &&
            //WidthCut == other.WidthCut &&
            //HeightCut == other.HeightCut;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Scale, Units, Pixels, WidthCut, HeightCut);
    }

    #endregion

    */

    public ScaledSize()
    {
        Units = default;
        Pixels = default;
        WidthCut = false;
        HeightCut = false;
        Scale = 1;
    }

    public ScaledSize WithCut(bool widthCut, bool heightCut)
    {
        return new ScaledSize
        {
            Scale = this.Scale,
            Units = this.Units,
            Pixels = this.Pixels,
            WidthCut = widthCut,
            HeightCut = heightCut
        };
    }

    public static ScaledSize Default => new();

    public float _scale = 1;
    public float Scale
    {
        get
        {
            return _scale;
        }
        set
        {
            _scale = value;
        }
    }

    public SKSize Units { get; set; }
    public SKSize Pixels { get; set; }
    public bool WidthCut { get; set; }
    public bool HeightCut { get; set; }

    public ScaledSize Clone()
    {
        return new()
        {
            Scale = this.Scale,
            Units = this.Units,
            Pixels = this.Pixels,
            HeightCut = this.HeightCut,
            WidthCut = this.WidthCut
        };
    }

    public static ScaledSize CreateEmpty(float scale)
    {
        return new()
        {
            Scale = scale,
            Units = SKSize.Empty,
            Pixels = SKSize.Empty
        };
    }

    public bool IsEmpty
    {
        get
        {
            return Pixels.IsEmpty;
        }
    }

    public static ScaledSize FromUnits(float width, float height, float scale)
    {
        if (double.IsNaN(width))
            width = -1;
        if (double.IsNaN(height))
            height = -1;

        var nWidth = (float)Math.Round(width * scale);
        if (nWidth < 0)
            nWidth = -1;
        if (float.IsInfinity(width))
        {
            nWidth = float.PositiveInfinity;
        }

        var nHeight = (float)Math.Round(height * scale);
        if (nHeight < 0)
            nHeight = -1;

        if (float.IsInfinity(height))
        {
            nHeight = float.PositiveInfinity;
        }

        return new ScaledSize()
        {
            Scale = scale,
            Units = new SKSize(width, height),
            Pixels = new SKSize(nWidth, nHeight)
        };
    }

    public static ScaledSize FromPixels(SKSize size, float scale)
    {
        return FromPixels((float)Math.Round(size.Width), (float)Math.Round(size.Height), false, false, scale);
    }

    public static ScaledSize FromPixels(float width, float height, float scale)
    {
        return FromPixels(width, height, false, false, scale);
    }

    public static ScaledSize FromPixels(float width, float height, bool widthCut, bool heighCut, float scale)
    {
        if (double.IsNaN(width))
            width = -1;
        if (double.IsNaN(height))
            height = -1;

        var nWidth = width / scale;
        if (nWidth < 0)
            nWidth = -1;

        var nHeight = height / scale;
        if (nHeight < 0)
            nHeight = -1;

        return new ScaledSize()
        {
            Scale = scale,
            Pixels = new SKSize(width, height),
            Units = new SKSize(nWidth, nHeight),
            WidthCut = widthCut,
            HeightCut = heighCut
        };
    }
}