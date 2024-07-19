﻿//Adapted code from the Xamarin.Forms Grid implementation

using DrawnUi.Maui.Infrastructure.Xaml;
using System.ComponentModel;
using System.Linq;

namespace DrawnUi.Maui.Draw;

public partial class SkiaLayout
{
	#region GRID


	public virtual ScaledSize MeasureGrid(SKRect rectForChildrenPixels, float scale)
	{

		var constraints = GetSizeInPoints(rectForChildrenPixels.Size, scale);

		BuildGridLayout(constraints);

		GridStructureMeasured.DecompressStars(constraints);

		var maxHeight = 0.0f;
		var maxWidth = 0.0f;
		var spacing = 0.0;

		if (GridStructureMeasured.Columns.Length > 1)
		{
			spacing = (GridStructureMeasured.Columns.Length - 1) * ColumnSpacing;
		}

		var contentWidth = (float)Math.Round((GridStructureMeasured.Columns.Sum(x => x.Size) + spacing + Padding.Left + Padding.Right) * scale);

		spacing = 0.0;
		if (GridStructureMeasured.Rows.Length > 1)
		{
			spacing = (GridStructureMeasured.Rows.Length - 1) * RowSpacing;
		}
		var contentHeight = (float)(Math.Round(GridStructureMeasured.Rows.Sum(x => x.Size) + spacing + Padding.Top + Padding.Bottom) * scale);

		if (contentWidth > maxWidth)
			maxWidth = contentWidth;
		if (contentHeight > maxHeight)
			maxHeight = contentHeight;


		return ScaledSize.FromPixels(maxWidth, maxHeight, scale);
	}


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

			List<SkiaControlWithRect> tree = new();

			foreach (var child in cells)
			{
				if (!child.CanDraw)
					continue;

				//GetCellBoundsFor works with points
				var cell = GridStructure.GetCellBoundsFor(child, (destination.Left / scale),
					(destination.Top / scale));

				//GetCellBoundsFor is in pixels
				SKRect cellRect = new((float)Math.Round(cell.Left * scale), (float)Math.Round(cell.Top * scale),
					(float)Math.Round(cell.Right * scale), (float)Math.Round(cell.Bottom * scale));


				if (IsRenderingWithComposition)
				{
					if (DirtyChildrenInternal.Contains(child))
					{
						DrawChild(context, cellRect, child, scale);
					}
					else
					{
						child.Arrange(cellRect, child.SizeRequest.Width, child.SizeRequest.Height, scale);
					}
				}
				else
				{
					DrawChild(context, cellRect, child, scale);
				}

				tree.Add(new SkiaControlWithRect(child, cellRect, child.LastDrawnAt, drawn));

				drawn++;
			}

			RenderTree = tree;
			_builtRenderTreeStamp = _measuredStamp;

		}
		return drawn;
	}


	protected void BuildGridLayout(SKSize constraints)
	{
		GridStructureMeasured = new SkiaGridStructure(this, constraints.Width, constraints.Height);
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
	[System.ComponentModel.TypeConverter(typeof(GridLengthTypeConverter))]
	public GridLength DefaultColumnDefinition
	{
		get { return (GridLength)GetValue(DefaultColumnDefinitionProperty); }
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
	[System.ComponentModel.TypeConverter(typeof(GridLengthTypeConverter))]
	public GridLength DefaultRowDefinition
	{
		get { return (GridLength)GetValue(DefaultRowDefinitionProperty); }
		set { SetValue(DefaultRowDefinitionProperty, value); }
	}

	private List<ColumnDefinition> _colDefs;
	private List<RowDefinition> _rowDefs;
	IReadOnlyList<RowDefinition> ISkiaGridLayout.RowDefinitions => _rowDefs ??= new(RowDefinitions);
	IReadOnlyList<ColumnDefinition> ISkiaGridLayout.ColumnDefinitions => _colDefs ??= new(ColumnDefinitions);
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
			if (bindable is SkiaLayout control)
			{
				control.InvalidateMeasure();
			}
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
			if (bindable is SkiaLayout control)
			{
				control.InvalidateMeasure();
			}
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

	protected void UpdateRowColumnBindingContexts()
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