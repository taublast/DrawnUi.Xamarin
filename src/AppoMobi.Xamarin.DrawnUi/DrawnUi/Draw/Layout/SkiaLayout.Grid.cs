﻿//Adapted code from the Xamarin.Forms Grid implementation


using System.ComponentModel;

namespace DrawnUi.Maui.Draw;

/// <summary>
/// Provides the properties for a column in a GridLayout.
/// </summary>
public interface IGridColumnDefinition
{
    /// <summary>
    /// Gets the width of the column.
    /// </summary>
    GridLength Width { get; }
}

/// <summary>
/// Provides the properties for a row in a GridLayout.
/// </summary>
public interface IGridRowDefinition
{
    /// <summary>
    /// Gets the height of the row.
    /// </summary>
    GridLength Height { get; }
}

public partial class SkiaLayout
{
    #region GRID

    /// <summary>
    /// Returns number of drawn children
    /// </summary>
    /// <param name="context"></param>
    /// <param name="destination"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    protected virtual int DrawChildrenGrid(SkiaDrawingContext context, SKRect destination, float scale)
    {
        var drawn = 0;
        if (GridStructure != null)
        {
            //var visibleArea = RenderingViewport;

            using var cells = ChildrenFactory.GetViewsIterator();
            //todo optimize this so we do not call GetCellBoundsFor every redraw but use RenderTree like in column/row stack
            foreach (var child in cells)
            {
                if (!child.CanDraw)
                    continue;

                var cell = GridStructure.GetCellBoundsFor(child, destination.Left / scale,
                    destination.Top / scale);

                SKRect cellRect = new((float)Math.Round(cell.Left * scale), (float)Math.Round(cell.Top * scale),
                    (float)Math.Round(cell.Right * scale), (float)Math.Round(cell.Bottom * scale));

                DrawChild(context, cellRect, child, scale);
                drawn++;
            }
        }
        return drawn;
    }


    protected void BuildGridLayout(SKSize constarints)
    {
        GridStructureMeasured = new SkiaGridStructure(this, constarints.Width, constarints.Height);
    }

    public int GetColumn(BindableObject bindable)
    {
        return Grid.GetColumn(bindable);
    }

    public int GetColumnSpan(BindableObject bindable)
    {
        return Grid.GetColumnSpan(bindable);
    }

    public int GetRow(BindableObject bindable)
    {
        return Grid.GetRow(bindable);
    }

    public int GetRowSpan(BindableObject bindable)
    {
        return Grid.GetRowSpan(bindable);
    }

    public void SetColumn(BindableObject bindable, int value)
    {
        Grid.SetColumn(bindable, value);
    }

    public void SetColumnSpan(BindableObject bindable, int value)
    {
        Grid.SetColumnSpan(bindable, value);
    }

    public void SetRow(BindableObject bindable, int value)
    {
        Grid.SetRow(bindable, value);
    }

    public void SetRowSpan(BindableObject bindable, int value)
    {
        Grid.SetRowSpan(bindable, value);
    }


    public SkiaGridStructure GridStructure;

    public SkiaGridStructure GridStructureMeasured;

    #endregion

    #region PROPERTIES

    public static readonly BindableProperty DefaultColumnDefinitionProperty = BindableProperty.Create(
        nameof(DefaultColumnDefinition),
        typeof(GridLength),
        typeof(SkiaLayout),
        new GridLength(1, GridUnitType.Star), propertyChanged: Invalidate);

    /// <summary>
    /// Will use this to create a missing but required ColumnDefinition
    /// </summary>
    [System.ComponentModel.TypeConverter(typeof(ColumnDefinitionCollectionTypeConverter))]
    public ColumnDefinition DefaultColumnDefinition
    {
        get { return (ColumnDefinition)GetValue(DefaultColumnDefinitionProperty); }
        set { SetValue(DefaultColumnDefinitionProperty, value); }
    }

    public static readonly BindableProperty DefaultRowDefinitionProperty = BindableProperty.Create(
        nameof(DefaultRowDefinition),
        typeof(GridLength),
        typeof(SkiaLayout),
        new GridLength(1, GridUnitType.Star), propertyChanged: Invalidate);

    /// <summary>
    /// Will use this to create a missing but required RowDefinition
    /// </summary>
    [System.ComponentModel.TypeConverter(typeof(RowDefinitionCollectionTypeConverter))]
    public RowDefinition DefaultRowDefinition
    {
        get { return (RowDefinition)GetValue(DefaultRowDefinitionProperty); }
        set { SetValue(DefaultRowDefinitionProperty, value); }
    }

    private List<IGridColumnDefinition> _colDefs;
    private List<IGridRowDefinition> _rowDefs;

    //private ColumnDefinitionCollection _colDefs;
    //private RowDefinitionCollection _rowDefs;

    IReadOnlyList<IGridRowDefinition> ISkiaGridLayout.RowDefinitions => _rowDefs ??= new();//RowDefinitions
    IReadOnlyList<IGridColumnDefinition> ISkiaGridLayout.ColumnDefinitions => _colDefs ??= new();//ColumnDefinitions

    public static readonly BindableProperty RowSpacingProperty = BindableProperty.Create(nameof(RowSpacing), typeof(double), typeof(SkiaLayout),
        1.0,
        propertyChanged: Invalidate);
    public double RowSpacing
    {
        get { return (double)GetValue(RowSpacingProperty); }
        set { SetValue(RowSpacingProperty, value); }
    }

    public static readonly BindableProperty ColumnSpacingProperty = BindableProperty.Create(nameof(ColumnSpacing), typeof(double), typeof(SkiaLayout),
        1.0,
        propertyChanged: Invalidate);
    public double ColumnSpacing
    {
        get { return (double)GetValue(ColumnSpacingProperty); }
        set { SetValue(ColumnSpacingProperty, value); }
    }

    public static readonly BindableProperty ColumnDefinitionsProperty = BindableProperty.Create(nameof(ColumnDefinitions),
        typeof(ColumnDefinitionCollection), typeof(SkiaLayout),
        null,
        validateValue: (bindable, value) => value != null,
        propertyChanged: UpdateSizeChangedHandlers, defaultValueCreator: bindable =>
        {
            var colDef = new ColumnDefinitionCollection()
            {
                //new ColumnDefinition(new GridLength(1,GridUnitType.Auto))
            };
            colDef.ItemSizeChanged += ((SkiaLayout)bindable).DefinitionsChanged;
            return colDef;
        });
    [System.ComponentModel.TypeConverter(typeof(ColumnDefinitionCollectionTypeConverter))]

    public ColumnDefinitionCollection ColumnDefinitions
    {
        get { return (ColumnDefinitionCollection)GetValue(ColumnDefinitionsProperty); }
        set { SetValue(ColumnDefinitionsProperty, value); }
    }

    public static readonly BindableProperty RowDefinitionsProperty = BindableProperty.Create(nameof(RowDefinitions),
        typeof(RowDefinitionCollection), typeof(SkiaLayout),
        null,
        validateValue: (bindable, value) => value != null,
        propertyChanged: UpdateSizeChangedHandlers, defaultValueCreator: bindable =>
        {
            var colDef = new RowDefinitionCollection()
            {
                //new RowDefinition(new GridLength(1,GridUnitType.Auto))
            };
            colDef.ItemSizeChanged += ((SkiaLayout)bindable).DefinitionsChanged;
            return colDef;
        });
    [System.ComponentModel.TypeConverter(typeof(RowDefinitionCollectionTypeConverter))]

    public RowDefinitionCollection RowDefinitions
    {
        get { return (RowDefinitionCollection)GetValue(RowDefinitionsProperty); }
        set { SetValue(RowDefinitionsProperty, value); }
    }

    protected static void UpdateSizeChangedHandlers(BindableObject bindable, object oldValue, object newValue)
    {
        var gridLayout = (SkiaLayout)bindable;

        if (oldValue is ColumnDefinitionCollection oldColDefs)
        {
            oldColDefs.ItemSizeChanged -= gridLayout.DefinitionsChanged;
        }
        else if (oldValue is RowDefinitionCollection oldRowDefs)
        {
            oldRowDefs.ItemSizeChanged -= gridLayout.DefinitionsChanged;
        }

        if (newValue is ColumnDefinitionCollection newColDefs)
        {
            newColDefs.ItemSizeChanged += gridLayout.DefinitionsChanged;
        }
        else if (newValue is RowDefinitionCollection newRowDefs)
        {
            newRowDefs.ItemSizeChanged += gridLayout.DefinitionsChanged;
        }

        gridLayout.DefinitionsChanged(bindable, EventArgs.Empty);
    }

    protected static void Invalidate(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SkiaLayout grid)
        {
            grid.Invalidate();
        }
    }

    void DefinitionsChanged(object sender, EventArgs args)
    {
        // Clear out the IGridLayout row/col defs; they'll be set up again next time they're accessed
        _rowDefs = null;
        _colDefs = null;

        UpdateRowColumnBindingContexts();

        Invalidate();
    }

    void UpdateRowColumnBindingContexts()
    {
        var bindingContext = BindingContext;

        RowDefinitionCollection rowDefs = RowDefinitions;
        for (var i = 0; i < rowDefs.Count; i++)
        {
            SetInheritedBindingContext(rowDefs[i], bindingContext);
        }

        ColumnDefinitionCollection colDefs = ColumnDefinitions;
        for (var i = 0; i < colDefs.Count; i++)
        {
            SetInheritedBindingContext(colDefs[i], bindingContext);
        }
    }



    #endregion

}