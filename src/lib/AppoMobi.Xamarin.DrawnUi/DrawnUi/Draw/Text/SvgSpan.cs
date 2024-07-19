﻿namespace DrawnUi.Maui.Draw;

public class SvgSpan : TextSpan, IDrawnTextSpan
{
	public SvgSpan()
	{
		Text = "?";
		TextFiltered = "?";
	}

	private DrawImageAlignment _VerticalAlignement = DrawImageAlignment.End;
	public DrawImageAlignment VerticalAlignement
	{
		get
		{
			return _VerticalAlignement;
		}
		set
		{
			if (_VerticalAlignement != value)
			{
				_VerticalAlignement = value;
				OnPropertyChanged();
			}
		}
	}

	private double _Height;
	public double Height
	{
		get
		{
			return _Height;
		}
		set
		{
			if (_Height != value)
			{
				_Height = value;
				OnPropertyChanged();
			}
		}
	}

	private double _Width;
	public double Width
	{
		get
		{
			return _Width;
		}
		set
		{
			if (_Width != value)
			{
				_Width = value;
				OnPropertyChanged();
			}
		}
	}

	private Color _TintColor = Color.Transparent;
	public Color TintColor
	{
		get
		{
			return _TintColor;
		}
		set
		{
			if (_TintColor != value)
			{
				_TintColor = value;
				OnPropertyChanged();
			}
		}
	}

	private string _Source;
	public string Source
	{
		get
		{
			return _Source;
		}
		set
		{
			if (_Source != value)
			{
				_Source = value;
				OnPropertyChanged();
			}
		}
	}

	protected SkiaSvg Control = new();

	void IDrawnTextSpan.Render(SkiaDrawingContext ctx, SKRect destination, float scale)
	{
		Control.Render(ctx, destination, scale);
	}

	public override void Dispose()
	{
		Control.Dispose();

		base.Dispose();
	}

	ScaledSize IDrawnTextSpan.Measure(float maxWidth, float maxHeight, float scale)
	{
		if (Control.Parent == null)
		{
			Control.Parent = this.Parent as SkiaControl; // to Update the parent when svg loads source etc
			Control.TintColor = this.TintColor;
			Control.BackgroundColor = this.BackgroundColor;
			Control.Source = this.Source;
			Control.WidthRequest = this.Width;
			Control.HeightRequest = this.Height;
		}

		return Control.Measure((float)this.Width * scale, (float)this.Height * scale, scale);
	}
}