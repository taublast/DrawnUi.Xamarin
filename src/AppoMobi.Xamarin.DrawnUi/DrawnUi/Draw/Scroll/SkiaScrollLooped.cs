using System.Drawing;
using System.Numerics;
using Color = Xamarin.Forms.Color;

namespace DrawnUi.Maui.Draw;

 /// <summary>
/// Cycles content, so the scroll never ands but cycles from the start
/// </summary>
public class SkiaScrollLooped : SkiaScroll
{
    public SkiaScrollLooped()
    {
        Bounces = false;
    }

    public static bool Debug = false;

    public override bool IsClippedToBounds => true;

    public override bool ShouldInvalidateByChildren => false;


    protected override void InitializeScroller(float scale)
    {
        base.InitializeScroller(scale);


        _scrollMinX = float.NegativeInfinity;
        _scrollMaxX = float.PositiveInfinity;

        _scrollMinY = float.NegativeInfinity;
        _scrollMaxY = float.PositiveInfinity;

        //_velocityScrollerY.mMinValue = _scrollMinY;
        //_velocityScrollerY.mMaxValue = _scrollMaxY;

        //_velocityScrollerX.mMinValue = _scrollMinX;
        //_velocityScrollerX.mMaxValue = _scrollMaxX;

    }



    public ContainsPointResult GetItemIndex(SkiaLayout layout, float pixelsOffsetX, float pixelsOffsetY, RelativePositionType option)
    {
        if (layout.LatestStackStructure == null)
            return ContainsPointResult.NotFound();

        bool trace = false;

        if (this.Orientation == ScrollOrientation.Vertical)
        {
            var initialValue = pixelsOffsetY;

            // ----------- proper to infinite start 

            if (option == RelativePositionType.Center)
            {
                pixelsOffsetY -= Viewport.Pixels.Height / 2f;
            }
            else
            if (option == RelativePositionType.End)
            {
                pixelsOffsetY -= Viewport.Pixels.Height;
            }

            if (pixelsOffsetY > 0)
            {
                //inverted scroll
                pixelsOffsetY -= Content.MeasuredSize.Pixels.Height;

            }
            else
            {
                //normal scroll
                if (-pixelsOffsetY > Content.MeasuredSize.Pixels.Height)
                {
                    pixelsOffsetY += Content.MeasuredSize.Pixels.Height;
                }
            }

            // ----------- proper to infinite end

            var point = new SKPoint(
            (float)Math.Abs(pixelsOffsetX),
            (float)Math.Abs(pixelsOffsetY)
            );

            if (layout.Type == LayoutType.Column) //todo grid
            {
                var stackStructure = layout.LatestStackStructure;
                int index = -1;
                int row;
                int col;

                if (trace)
                    Trace.WriteLine($"offset: {point.Y}");

                for (row = 0; row < stackStructure.Count; row++)
                {
                    var rowContent = stackStructure[row];
                    for (col = 0; col < rowContent.Count; col++)
                    {
                        index++;
                        var childInfo = rowContent[col];

                        if (childInfo.Destination.ContainsInclusive(point))
                        {
                            return new ContainsPointResult()
                            {
                                Index = index,
                                Area = childInfo.Destination,
                                Point = point,
                                Unmodified = new SKPoint(0, initialValue)
                            };
                        }

                    }
                }
            }
        }
        else
        if (this.Orientation == ScrollOrientation.Horizontal)
        {
            var initialValue = pixelsOffsetX;

            // ----------- proper to infinite start 

            if (option == RelativePositionType.Center)
            {
                pixelsOffsetX -= Viewport.Pixels.Width / 2f;
            }
            else
            if (option == RelativePositionType.End)
            {
                pixelsOffsetX -= Viewport.Pixels.Width;
            }

            if (pixelsOffsetX > 0)
            {
                //inverted scroll
                //var bak = pixelsOffsetX;
                pixelsOffsetX -= Content.MeasuredSize.Pixels.Width;
                //Trace.WriteLine($"[INVERT ] {bak:0.0} --> {pixelsOffsetX:0.0}");
            }
            else
            {
                //normal scroll
                if (-pixelsOffsetX > Content.MeasuredSize.Pixels.Width)
                {
                    pixelsOffsetX += Content.MeasuredSize.Pixels.Width;
                }
            }

            //Trace.WriteLine($"[CALC] for {pixelsOffsetX:0.0}");
            // ----------- proper to infinite end


            var point = new SKPoint(
            (float)Math.Abs(pixelsOffsetX),
            (float)Math.Abs(pixelsOffsetY)
            );


            if (layout.Type == LayoutType.Row) //todo grid
            {
                var stackStructure = layout.StackStructure;
                int index = -1;
                int row;
                int col;

                if (trace)
                    Trace.WriteLine($"offset: {point.X}");

                for (row = 0; row < stackStructure.Count; row++)
                {
                    var rowContent = stackStructure[row];
                    for (col = 0; col < rowContent.Count; col++)
                    {
                        index++;
                        var childInfo = rowContent[col];

                        var childRect = childInfo.Destination.Clone();
                        //childRect.Offset(point.X, point.Y);

                        if (trace)
                            Trace.WriteLine($"child: {childRect.Left:0.0} - {childRect.Right:0.0}");

                        if (childRect.ContainsInclusive(point))
                        {
                            return new ContainsPointResult()
                            {
                                Index = index,
                                Area = childRect,
                                Point = point,
                                Unmodified = new SKPoint(initialValue, 0)
                            };
                        }

                    }
                }


            }
        }

        return ContainsPointResult.NotFound();
    }


    public override ContainsPointResult CalculateVisibleIndex(RelativePositionType option = RelativePositionType.Start)
    {
        if (Content is SkiaLayout layout)
        {

            var pixelsOffsetX = InternalViewportOffset.Pixels.X;// (float)(ViewportOffsetX * layout.RenderingScale);
            var pixelsOffsetY = InternalViewportOffset.Pixels.Y;// (float)(ViewportOffsetY * layout.RenderingScale);

            return GetItemIndex(layout, pixelsOffsetX, pixelsOffsetY, option);
        }

        return ContainsPointResult.NotFound();
    }


    protected override bool OffsetOk(Vector2 offset)
    {
        return true;
    }

    public override void Snap(float maxTimeSecs)
    {
        //Trace.WriteLine($"Snap entered");

        if (OrderedScrollTo.IsValid || _isSnapping)
        {
            return;
        }

        _isSnapping = true;

        if (Content is SkiaLayout layout)
        {
            var hit = CurrentIndexHit;
            if (hit?.Index > -1 && layout.ChildrenFactory.GetChildrenCount() > hit?.Index)
            {
                //if (hit.Unmodified == SKPoint.Empty)
                //{
                //	_isSnapping = false;
                //	return;
                //}

                var needMove = 0f;
                if (Orientation == ScrollOrientation.Vertical)
                {
                    //float needOffsetY = (float)Math.Truncate(ViewportOffsetY);
                    float needOffsetY = (float)Math.Truncate(InternalViewportOffset.Pixels.Y);
                    var initialOffset = needOffsetY;
                    if (SnapToChildren == SnapToChildrenType.Center)
                    {
                        var center = hit.Area.Height / 2f;
                        var pointY = hit.Area.Bottom - hit.Point.Y;
                        needMove = -(pointY - center);
                    }
                    else if (SnapToChildren == SnapToChildrenType.Side)
                    {

                        if (TrackIndexPosition == RelativePositionType.Start)
                        {
                            needMove = hit.Point.Y - hit.Area.Bottom;
                        }
                        else if (TrackIndexPosition == RelativePositionType.End)
                        {
                            needMove = -(hit.Area.Bottom - hit.Point.Y);
                        }
                    }

                    var threshold = RenderingScale * 2;

                    needOffsetY = hit.Unmodified.Y + needMove;
                    if (needMove != 0f && Math.Abs(initialOffset - needOffsetY) > threshold)
                    {
                        ScrollTo(InternalViewportOffset.Units.X, needOffsetY / layout.RenderingScale, maxTimeSecs);

                        //Trace.WriteLine($"Snapping to {needOffsetY / layout.RenderingScale}");
                        return;
                    }

                    //Trace.WriteLine($"Snap low threshold");
                }
                else if (Orientation == ScrollOrientation.Horizontal)
                {
                    float needOffsetX = (float)Math.Truncate(InternalViewportOffset.Units.X);
                    var initialOffset = needOffsetX;
                    if (SnapToChildren == SnapToChildrenType.Center)
                    {
                        var center = hit.Area.Width / 2f;
                        var pointX = hit.Area.Right - hit.Point.X;
                        needMove = -(pointX - center);
                    }
                    else if (SnapToChildren == SnapToChildrenType.Side)
                    {

                        if (TrackIndexPosition == RelativePositionType.Start)
                        {
                            needMove = hit.Point.X - hit.Area.Right;
                            //needOffsetX += needMove;
                        }
                        else if (TrackIndexPosition == RelativePositionType.End)
                        {
                            needMove = -(hit.Area.Right - hit.Point.X);
                            //needOffsetX += needMove;
                        }
                    }

                    needOffsetX = hit.Unmodified.X + needMove;
                    if (needMove != 0f && initialOffset != needOffsetX)
                    {

                        ScrollTo(needOffsetX / layout.RenderingScale, InternalViewportOffset.Units.Y, maxTimeSecs);

                        return;

                    }

                }
            }

        }

        _isSnapping = false;
    }

    //Using this to draw a second fake copy of content to loop the cycle
    protected override void OnDrawn(SkiaDrawingContext context,
        SKRect destination,
        float zoomedScale,
        double scale = 1)
    {
        bool debug = false;

        if (!IsBanner)
        {
            if (Orientation == ScrollOrientation.Vertical)
            {
                var hiddenContentHeightPixels = Content.MeasuredSize.Pixels.Height - destination.Height;
                if (hiddenContentHeightPixels < 0)
                    hiddenContentHeightPixels = 0;

                if (hiddenContentHeightPixels > 0)
                {

                    var pixelsContentOffsetY = (float)(InternalViewportOffset.Pixels.Y);
                    float offsetY = 0;
                    if (pixelsContentOffsetY > 0)
                    {
                        offsetY = pixelsContentOffsetY - Content.MeasuredSize.Pixels.Height;
                    }
                    else
                    if (pixelsContentOffsetY < 0)
                    {
                        offsetY = Content.MeasuredSize.Pixels.Height + pixelsContentOffsetY;
                    }

                    var childRect = destination.Clone();
                    childRect.Offset(0, offsetY);

                    if (pixelsContentOffsetY > 0)
                        childRect.Bottom += hiddenContentHeightPixels;

                    DrawWithClipAndTransforms(context, DrawingRect, true,
                        true, (ctx) =>
                        {
                            if (Debug)
                            {
                                using var paint = new SKPaint()
                                {
                                    ColorFilter = SkiaImageEffects.Tint(Color.Red, SKBlendMode.SrcIn)
                                };
                                var count = context.Canvas.SaveLayer(paint);

                                DrawViews(context, childRect, zoomedScale, debug);

                                context.Canvas.RestoreToCount(count);
                            }
                            else
                            {
                                DrawViews(context, childRect, zoomedScale, debug);
                            }
                        });
                }
            }
            else
            if (Orientation == ScrollOrientation.Horizontal)
            {
                var hiddenContentWidthPixels = Content.MeasuredSize.Pixels.Width - destination.Width;
                if (hiddenContentWidthPixels < 0)
                    hiddenContentWidthPixels = 0;

                if (hiddenContentWidthPixels > 0)
                {

                    var pixelsContentOffsetX = InternalViewportOffset.Pixels.X;
                    float offsetX = 0;
                    if (pixelsContentOffsetX > 0)
                    {
                        offsetX = pixelsContentOffsetX - Content.MeasuredSize.Pixels.Width;
                    }
                    else
                    if (pixelsContentOffsetX < 0)
                    {
                        offsetX = Content.MeasuredSize.Pixels.Width + pixelsContentOffsetX;
                    }

                    var childRect = destination.Clone();
                    childRect.Offset(offsetX, 0);

                    DrawWithClipAndTransforms(context, DrawingRect, true,
                        true, (ctx) =>
                        {
                            DrawViews(context, childRect, zoomedScale, debug);
                        });

                }
            }
        }

        //ScrollOrientation.Both unsupported

        base.OnDrawn(context, destination, zoomedScale, scale);
    }




    public override Vector2 ClampOffset(float x, float y, bool strict = false)
    {
        //to avoid clamping infinity
        if (Orientation == ScrollOrientation.Vertical)
        {
            var clampedX = Math.Max(ContentOffsetBounds.Left, Math.Min(ContentOffsetBounds.Right, x));
            return new Vector2(clampedX, y);
        }
        else
       if (Orientation == ScrollOrientation.Horizontal)
        {
            var clampedY = Math.Max(ContentOffsetBounds.Top, Math.Min(ContentOffsetBounds.Bottom, y));
            return new Vector2(x, clampedY);
        }

        return new Vector2((float)Math.Round(x), (float)Math.Round(y));
    }

    protected override void PositionViewport(SKRect destination, SKPoint offsetPixels, float viewportScale, float scale)
    {
        if (!IsBanner)
        {
            var clampedOffsetPixels = ModifyViewportOffset(destination, offsetPixels, scale);
            base.PositionViewport(destination, clampedOffsetPixels, viewportScale, scale);
        }
        else
        {
            base.PositionViewport(destination, offsetPixels, viewportScale, scale);
        }
    }

    protected virtual SKPoint ModifyViewportOffset(SKRect destination, SKPoint offsetPixels, float scale)
    {
        float ClampOffsetForDuplicate(float offset, float limitPositive, float limitNegative)
        {
            int parts;
            if (offset > 0)
            {
                parts = (int)(offset / limitPositive);
                if (parts != 0)
                {
                    var cut = offset - limitPositive * parts;
                    offset = (float)cut * Math.Sign(offset);
                }
            }
            else
            if (offset < 0)
            {
                parts = (int)(Math.Abs(offset / limitNegative));
                if (parts != 0)
                {
                    var cut = Math.Abs(offset) - limitNegative * parts;
                    offset = (float)cut * Math.Sign(offset);
                }
            }
            return offset;
        }

        //banner-like
        if (IsBanner)
        {
            //infinite, but do not draw duplicate like for scrolling banner

            //offsetY = ClampOffset(offsetY, destination.Height / scale, Content.MeasuredSize.Units.Height);
            //offsetX = ClampOffset(offsetX, destination.Width / scale, Content.MeasuredSize.Units.Width);
        }
        else
        {
            //need draw duplicate
            var offsetY = ClampOffsetForDuplicate(
                offsetPixels.Y,
                Content.MeasuredSize.Pixels.Height, Content.MeasuredSize.Pixels.Height);

            var offsetX = ClampOffsetForDuplicate(
                offsetPixels.X,
                Content.MeasuredSize.Pixels.Width, Content.MeasuredSize.Pixels.Width);

            return new(offsetX, offsetY);
        }

        return offsetPixels;
    }

    public static readonly BindableProperty CycleSpaceProperty
    = BindableProperty.Create(nameof(CycleSpace),
    typeof(double), typeof(SkiaScrollLooped),
    0.0, propertyChanged: NeedDraw);
    /// <summary>
    /// Space between cycles, pixels
    /// </summary>
    public double CycleSpace
    {
        get
        {
            return (double)GetValue(CycleSpaceProperty);
        }
        set
        {
            SetValue(CycleSpaceProperty, value);
        }
    }

    public static readonly BindableProperty IsBannerProperty
    = BindableProperty.Create(nameof(IsBanner),
    typeof(bool), typeof(SkiaScrollLooped),
    false, propertyChanged: NeedDraw);
    /// <summary>
    /// Whether this should look like an infinite scrolling text banner
    /// </summary>
    public bool IsBanner
    {
        get
        {
            return (bool)GetValue(IsBannerProperty);
        }
        set
        {
            SetValue(IsBannerProperty, value);
        }
    }

}