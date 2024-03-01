﻿ 
namespace DrawnUi.Maui.Draw;

public class BlinkAnimator : ProgressAnimator
{
    public BlinkAnimator(IDrawnBase parent) : base(parent)
    {
        Repeat = -1; //forever
        Speed = 1000; //ms
        Color1 = Color.Red;
        Color2 = Color.Gray;
        ColorsRatio = 0.5;
    }

    protected override void OnProgressChanged(double progress)
    {
        var border = ColorsRatio;

        if (progress < border)
        {
            CurrentColor = Color1;
        }
        else
        {
            CurrentColor = Color2;
        }

    }

    public Color CurrentColor { get; protected set; }

    #region PARAMETERS
    public Color Color1 { get; set; }
    public Color Color2 { get; set; }


    public double ColorsRatio { get; set; }

    #endregion

}

public class ToggleAnimator : ProgressAnimator
{
    public ToggleAnimator(IDrawnBase parent) : base(parent)
    {
        Repeat = -1; //forever
        Speed = 1000; //ms
        State = false;
        Ratio = 0.5;
    }

    protected override void OnProgressChanged(double progress)
    {
        var border = Ratio;

        if (progress < border)
        {
            State = false;
        }
        else
        {
            State = true;
        }

    }

    #region PARAMETERS
    public bool State { get; set; }


    public double Ratio { get; set; }

    #endregion

}