﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace DrawnUi.Maui.Draw;

public class SkiaGradient : BindableObject, ICloneable
{
    public ISkiaControl Parent { get; set; }
    public object Clone()
    {
        return new SkiaGradient
        {
            Type = Type,
            TileMode = TileMode,
            Light = Light,
            Colors = Colors,
            ColorPositions = ColorPositions,
            StartXRatio = StartXRatio,
            StartYRatio = StartYRatio,
            EndXRatio = EndXRatio,
            EndYRatio = EndYRatio,
            Opacity = Opacity
        };
    }

    private static void RedrawCanvas(BindableObject bindable, object oldvalue, object newvalue)
    {
        if (bindable is SkiaGradient shadow)
        {
            shadow.Parent?.Update();
        }
    }

    //-------------------------------------------------------------
    // Type
    //-------------------------------------------------------------
    private const string nameType = "Type";
    public static readonly BindableProperty TypeProperty = BindableProperty.Create(nameType, typeof(GradientType), typeof(SkiaGradient),
        GradientType.None,
        propertyChanged: RedrawCanvas);
    public GradientType Type
    {
        get { return (GradientType)GetValue(TypeProperty); }
        set { SetValue(TypeProperty, value); }
    }

    //-------------------------------------------------------------
    // BlendMode
    //-------------------------------------------------------------
    public static readonly BindableProperty BlendModeProperty = BindableProperty.Create(nameof(BlendMode),
        typeof(SKBlendMode), typeof(SkiaGradient),
        SKBlendMode.SrcOver,
        propertyChanged: RedrawCanvas);
    public SKBlendMode BlendMode
    {
        get { return (SKBlendMode)GetValue(BlendModeProperty); }
        set { SetValue(BlendModeProperty, value); }
    }

    //-------------------------------------------------------------
    // TileMode
    //-------------------------------------------------------------
    public static readonly BindableProperty TileModeProperty = BindableProperty.Create(nameof(TileMode), typeof(SKShaderTileMode), typeof(SkiaGradient),
        SKShaderTileMode.Clamp,
        propertyChanged: RedrawCanvas);
    public SKShaderTileMode TileMode
    {
        get { return (SKShaderTileMode)GetValue(TileModeProperty); }
        set { SetValue(TileModeProperty, value); }
    }

    //-------------------------------------------------------------
    // Light
    //-------------------------------------------------------------
    private const string nameLight = "Light";
    public static readonly BindableProperty LightProperty = BindableProperty.Create(nameLight, typeof(double), typeof(SkiaGradient), 1.0,
        propertyChanged: RedrawCanvas);
    public double Light
    {
        get { return (double)GetValue(LightProperty); }
        set { SetValue(LightProperty, value); }
    }

    //-------------------------------------------------------------
    // Opacity
    //-------------------------------------------------------------
    public static readonly BindableProperty OpacityProperty = BindableProperty.Create(nameof(Opacity),
        typeof(float), typeof(SkiaGradient),
        1.0f,
        propertyChanged: RedrawCanvas);
    public float Opacity
    {
        get { return (float)GetValue(OpacityProperty); }
        set { SetValue(OpacityProperty, value); }
    }


    //-------------------------------------------------------------
    // StartXRatio
    //-------------------------------------------------------------
    private const string nameStartXRatio = "StartXRatio";
    public static readonly BindableProperty StartXRatioProperty = BindableProperty.Create(nameStartXRatio, typeof(float), typeof(SkiaGradient), 0.1f,
        propertyChanged: RedrawCanvas);
    public float StartXRatio
    {
        get { return (float)GetValue(StartXRatioProperty); }
        set { SetValue(StartXRatioProperty, value); }
    }

    //-------------------------------------------------------------
    // StartYRatio
    //-------------------------------------------------------------
    private const string nameStartYRatio = "StartYRatio";
    public static readonly BindableProperty StartYRatioProperty = BindableProperty.Create(nameStartYRatio, typeof(float), typeof(SkiaGradient), 1.0f,
        propertyChanged: RedrawCanvas);
    public float StartYRatio
    {
        get { return (float)GetValue(StartYRatioProperty); }
        set { SetValue(StartYRatioProperty, value); }
    }

    //-------------------------------------------------------------
    // EndXRatio
    //-------------------------------------------------------------
    private const string nameEndXRatio = "EndXRatio";
    public static readonly BindableProperty EndXRatioProperty = BindableProperty.Create(nameEndXRatio, typeof(float), typeof(SkiaGradient), 0.5f,
        propertyChanged: RedrawCanvas);
    public float EndXRatio
    {
        get { return (float)GetValue(EndXRatioProperty); }
        set { SetValue(EndXRatioProperty, value); }
    }

    //-------------------------------------------------------------
    // EndYRatio
    //-------------------------------------------------------------
    private const string nameEndYRatio = "EndYRatio";
    public static readonly BindableProperty EndYRatioProperty = BindableProperty.Create(nameEndYRatio, typeof(float), typeof(SkiaGradient), 0.0f,
        propertyChanged: RedrawCanvas);
    public float EndYRatio
    {
        get { return (float)GetValue(EndYRatioProperty); }
        set { SetValue(EndYRatioProperty, value); }
    }

    #region COLORS


    public static readonly BindableProperty ColorsProperty = BindableProperty.Create(
        nameof(Colors),
        typeof(IList<Color>),
        typeof(SkiaGradient),
        defaultValueCreator: (instance) =>
        {
            var created = new ObservableCollection<Color>();
            ColorsPropertyChanged(instance, null, created);
            return created;
        },
        validateValue: (bo, v) => v is IList<Color>,
        propertyChanged: ColorsPropertyChanged,
        coerceValue: CoerceColors);


    public IList<Color> Colors
    {
        get => (IList<Color>)GetValue(ColorsProperty);
        set => SetValue(ColorsProperty, value);
    }

    private static object CoerceColors(BindableObject bindable, object value)
    {
        if (!(value is ReadOnlyCollection<Color> readonlyCollection))
        {
            return value;
        }

        return new ReadOnlyCollection<Color>(
            readonlyCollection.Select(s => s)
                .ToList());
    }

    private static void ColorsPropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
    {

        if (bindable is SkiaGradient gradient)
        {
            if (oldvalue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= gradient.OnSkiaPropertyColorCollectionChanged;
            }
            if (newvalue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += gradient.OnSkiaPropertyColorCollectionChanged;
            }

            gradient.Parent?.Update();
        }

    }

    private void OnSkiaPropertyColorCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        this.Parent?.Update();
    }

    #endregion


    #region COLOR POSITIONS


    public static readonly BindableProperty ColorPositionsProperty = BindableProperty.Create(
        nameof(ColorPositions),
        typeof(IList<double>),
        typeof(SkiaGradient),
        defaultValueCreator: (instance) =>
        {
            var created = new ObservableCollection<double>();
            ColorPositionsPropertyChanged(instance, null, created);
            return created;
        },
        validateValue: (bo, v) => v is IList<double>,
        propertyChanged: ColorPositionsPropertyChanged,
        coerceValue: CoerceColorPositions);


    public IList<double> ColorPositions
    {
        get => (IList<double>)GetValue(ColorPositionsProperty);
        set => SetValue(ColorPositionsProperty, value);
    }

    private static object CoerceColorPositions(BindableObject bindable, object value)
    {
        if (!(value is ReadOnlyCollection<double> readonlyCollection))
        {
            return value;
        }

        return new ReadOnlyCollection<double>(
            readonlyCollection.Select(s => s)
                .ToList());
    }

    private static void ColorPositionsPropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
    {

        if (bindable is SkiaGradient gradient)
        {
            if (oldvalue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= gradient.OnColorPositionsCollectionChanged;
            }
            if (newvalue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += gradient.OnColorPositionsCollectionChanged;
            }

            gradient.Parent?.Update();
        }

    }

    private void OnColorPositionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        this.Parent?.Update();
    }

    #endregion




}