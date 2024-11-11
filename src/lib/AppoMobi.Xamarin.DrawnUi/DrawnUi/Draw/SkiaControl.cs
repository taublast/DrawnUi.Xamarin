﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Color = Xamarin.Forms.Color;
using Size = Xamarin.Forms.Size;


namespace DrawnUi.Maui.Draw
{
    [DebuggerDisplay("{DebugString}")]
    [ContentProperty("Children")]
    public partial class SkiaControl : VisualElement,
        IHasAfterEffects, ISkiaControl
    //IVisualTreeElement, IReloadHandler, IHotReloadableView
    {
        public SkiaControl()
        {
            Init();
        }

        protected virtual void OnChildAdded(SkiaControl child)
        {
            Invalidate();
        }

        public virtual void OptionalOnBeforeDrawing()
        {
            Superview?.UpdateRenderingChains(this);
        }

        protected override void OnChildRemoved(Element child, int oldLogicalIndex)
        {
            //base.OnChildRemoved(child, oldLogicalIndex);
        }

        protected override void OnChildAdded(Element child)
        {
            //base.OnChildAdded(child);
        }

        protected virtual void OnChildRemoved(SkiaControl child)
        {
            Invalidate();
        }

        public virtual bool IsVisibleInViewTree()
        {
            var isVisible = IsVisible && !IsDisposed;

            var parent = this.Parent as SkiaControl;

            if (parent == null)
                return isVisible;

            if (isVisible)
                return parent.IsVisibleInViewTree();

            return false;
        }

        /// <summary>
        /// Absolute position in points
        /// </summary>
        /// <returns></returns>
        public virtual SKPoint GetPositionOnCanvasInPoints(bool useTranslation = true)
        {
            var position = GetPositionOnCanvas(useTranslation);

            return new(position.X / RenderingScale, position.Y / RenderingScale);
        }

        /// <summary>
        /// Absolute position in pixels.
        /// </summary>
        /// <returns></returns>
        public virtual SKPoint GetPositionOnCanvas(bool useTranslation = true)
        {
            //Debug.WriteLine($"GetPositionOnCanvas ------------------------START at {LastDrawnAt}");

            //ignore cache for this specific control only
            var position = BuildDrawingOffsetRecursive(LastDrawnAt, this, true, useTranslation);

            //Debug.WriteLine("GetPositionOnCanvas ------------------------END");
            return new(position.X, position.Y);
        }

        /// <summary>
        /// Find drawing position for control accounting for all caches up the rendering tree.
        /// </summary>
        /// <returns></returns>
        public virtual SKPoint GetSelfDrawingPosition()
        {
            var position = BuildSelfDrawingPosition(LastDrawnAt, this, true);

            return new(position.X, position.Y);
        }

        public SKPoint BuildSelfDrawingPosition(SKPoint offset, SkiaControl control, bool isChild)
        {
            if (control == null)
            {
                return offset;
            }

            var drawingOffset = SKPoint.Empty;
            if (!isChild)
            {
                drawingOffset = control.GetPositionOffsetInPixels(true);
                drawingOffset.Offset(offset);
            }
            else
            {
                drawingOffset = offset;
            }

            var parent = control.Parent as SkiaControl;
            if (parent == null)
            {
                return drawingOffset;
            }

            return BuildSelfDrawingPosition(drawingOffset, parent, false);
        }


        public SKPoint BuildDrawingOffsetRecursive(SKPoint offset, SkiaControl control, bool ignoreCache, bool useTranslation = true)
        {
            if (control == null)
            {
                //Debug.WriteLine($"[BuildDrawingOffsetRecursive] {this} returned {offset} for null control");
                return offset;
            }

            var drawingOffset = control.GetPositionOffsetInPixels(false, ignoreCache);
            drawingOffset.Offset(offset);

            var parent = control.Parent as SkiaControl;

            if (parent == null)
            {
                drawingOffset.Offset(control.LastDrawnAt);
                //Debug.WriteLine($"[BuildDrawingOffsetRecursive] {this} returned {drawingOffset} for null parent");
                return drawingOffset;
            }

            return BuildDrawingOffsetRecursive(drawingOffset, parent, false, useTranslation);
        }


        public virtual string DebugString
        {
            get
            {
                return $"{GetType().Name} Tag {Tag}, IsVisible {IsVisible}, Children {Views.Count}, {Width:0.0}x{Height:0.0}dp, DrawingRect: {DrawingRect}";
            }
        }



        /*

        #region HOTRELOAD

        /// <summary>
        /// HOTRELOAD IReloadHandler
        /// </summary>
        public virtual void Reload()
        {
            InvalidateMeasure();
        }

        public virtual void ReportHotreloadChildAdded(SkiaControl child)
        {
            if (child == null)
                return;

            //this.OnChildAdded(child);

            var children = GetVisualChildren();
            var index = children.FindIndex(child);

            if (index >= 0)
                VisualDiagnostics.OnChildAdded(this, child, index);
        }

        public virtual void ReportHotreloadChildRemoved(SkiaControl control)
        {
            if (control == null)
                return;


            var children = GetVisualChildren();
            var index = children.FindIndex(control);

            if (index >= 0)
                VisualDiagnostics.OnChildRemoved(this, control, index);
            //            this.OnChildRemoved(control, index);
        }

        #region HotReload

        IView IReplaceableView.ReplacedView =>
            MauiHotReloadHelper.GetReplacedView(this) ?? this;

        IReloadHandler IHotReloadableView.ReloadHandler { get; set; }

        void IHotReloadableView.TransferState(IView newView)
        {
            //reload the the ViewModel
            if (newView is SkiaControl v)
                v.BindingContext = BindingContext;
        }

        void IHotReloadableView.Reload()
        {
            InvalidateMeasure();
        }

        #endregion

        #region IVisualTreeElement

        public virtual IReadOnlyList<IVisualTreeElement> GetVisualChildren() //working fine
        {
            return Views.Cast<IVisualTreeElement>().ToList().AsReadOnly();

            //return Views.Select(x => x as IVisualTreeElement).ToList().AsReadOnly();;
        }

        public virtual IVisualTreeElement GetVisualParent()  //working fine
        {
            return Parent as IVisualTreeElement;
        }

        #endregion

        #endregion

        */

        public virtual bool CanDraw
        {
            get
            {
                if (!IsVisible || IsDisposed || SkipRendering)
                    return false;

                if (Superview != null && !Superview.CanDraw)
                {
                    return false;
                }

                return true;
            }
        }


        /// <summary>
        /// Can be set but custom controls while optimizing rendering etc. Will affect CanDraw.
        /// </summary>
        public bool SkipRendering { get; set; }

        protected bool DefaultChildrenCreated { get; set; }

        protected virtual void CreateDefaultContent()
        {

        }
        /// <summary>
        /// To create custom content in code-behind. Will be called from OnLayoutChanged if Views.Count == 0.
        /// </summary>
        public Func<List<SkiaControl>> CreateChildren
        {
            get => _createChildren;
            set
            {
                _createChildren = value;
                if (value != null)
                {
                    DefaultChildrenCreated = false;
                    CreateChildrenFromCode();
                }
            }
        }

        /// <summary>
        /// Executed when CreateChildren is set
        /// </summary>
        /// <returns></returns>
        protected virtual void CreateChildrenFromCode()
        {
            if (this.Views.Count == 0 && !DefaultChildrenCreated)
            {
                DefaultChildrenCreated = true;
                if (CreateChildren != null)
                {
                    try
                    {
                        var children = CreateChildren();
                        foreach (var skiaControl in children)
                        {
                            AddSubView(skiaControl);
                        }
                    }
                    catch (Exception e)
                    {
                        Super.Log(e);
                    }
                }

            }
        }

        public virtual void AddSubView(SkiaControl control)
        {
            if (control == null)
                return;
            control.SetParent(this);
            OnDrawnChildAdded(control);

            //if (Debugger.IsAttached)
            //    Superview?.PostponeExecutionAfterDraw(() =>
            //    {
            //        ReportHotreloadChildAdded(control);
            //    });
        }

        public virtual void RemoveSubView(SkiaControl control)
        {
            if (control == null)
                return;

            //if (Debugger.IsAttached)
            //    Superview?.PostponeExecutionAfterDraw(() =>
            //    {
            //        ReportHotreloadChildRemoved(control);
            //    });

            control.SetParent(null);
            OnDrawnChildRemoved(control);
        }


        /// <summary>
        /// This actually used by SkiaMauiElement but could be used by other controls. Also might be useful for debugging purposes.
        /// </summary>
        /// <returns></returns>
        public VisualTreeChain GenerateParentChain()
        {
            var currentParent = this.Parent as SkiaControl;

            var chain = new VisualTreeChain(this);

            var parents = new List<SkiaControl>();

            // Traverse up the parent hierarchy
            while (currentParent != null)
            {
                // Add the current parent to the chain
                parents.Add(currentParent);
                // Move to the next parent
                currentParent = currentParent.Parent as SkiaControl;
            }

            parents.Reverse();

            foreach (var parent in parents)
            {
                chain.AddNode(parent);
            }

            return chain;
        }

        /// <summary>
        /// //todo base. this is actually used by SkiaMauiElement only
        /// </summary>
        /// <param name="transform"></param>
        public virtual void SetVisualTransform(VisualTransform transform)
        {

        }

        public virtual void SuperViewChanged()
        {

        }

        /// <summary>
        /// Returns rendering scale adapted for another output size, useful for offline rendering
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public float GetRenderingScaleFor(float width, float height)
        {
            var aspectX = width / DrawingRect.Width;
            var aspectY = height / DrawingRect.Height;
            var scale = Math.Min(aspectX, aspectY) * RenderingScale;
            return scale;
        }

        public float GetRenderingScaleFor(float measure)
        {
            var aspectX = measure / DrawingRect.Width;
            var scale = aspectX * RenderingScale;
            return scale;
        }

        /// <summary>
        /// Creates a new animator, animates from 0 to 1 over a given time, and calls your callback with the current eased value
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="length"></param>
        /// <param name="easing"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public Task AnimateAsync(Action<double> callback,
            Action callbaclOnCancel = null,
            float ms = 250, Easing easing = null, CancellationTokenSource cancel = default)
        {
            if (easing == null)
            {
                easing = Easing.Linear;
            }

            var animator = new SkiaValueAnimator(this);

            if (cancel == default)
                cancel = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<bool>(cancel.Token);

            // Update animator parameters
            animator.mMinValue = 0;
            animator.mMaxValue = 1;
            animator.Speed = ms;
            animator.Easing = easing;

            animator.OnStop = () =>
            {
                if (!tcs.Task.IsCompleted)
                    tcs.SetResult(true);
                DisposeObject(animator);
            };
            animator.OnUpdated = (value) =>
            {
                if (!cancel.IsCancellationRequested)
                {
                    callback?.Invoke(value);
                }
                else
                {
                    callback?.Invoke(value);
                    animator.Stop();
                    DisposeObject(animator);
                }
            };

            animator.Start();

            return tcs.Task;
        }


        CancellationTokenSource _fadeCancelTokenSource;
        /// <summary>
        /// Fades the view from the current Opacity to end, animator is reused if already running
        /// </summary>
        /// <param name="end"></param>
        /// <param name="length"></param>
        /// <param name="easing"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public Task FadeToAsync(double end, float ms = 250, Easing easing = null, CancellationTokenSource cancel = default)
        {
            // Cancel previous animation if it exists and is still running.
            _fadeCancelTokenSource?.Cancel();
            _fadeCancelTokenSource = new CancellationTokenSource();

            var startOpacity = this.Opacity;
            return AnimateAsync(
                (value) =>
                {
                    this.Opacity = startOpacity + (end - startOpacity) * value;
                    //Debug.WriteLine($"[ANIM] Opacity: {this.Opacity}");
                },
                () =>
                {
                    this.Opacity = end;
                },
                ms,
                easing,
                _fadeCancelTokenSource);
        }

        CancellationTokenSource _scaleCancelTokenSource;
        /// <summary>
        /// Scales the view from the current Scale to x,y, animator is reused if already running
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="length"></param>
        /// <param name="easing"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public Task ScaleToAsync(double x, double y, float length = 250, Easing easing = null, CancellationTokenSource cancel = default)
        {
            // Cancel previous animation if it exists and is still running.
            _scaleCancelTokenSource?.Cancel();
            _scaleCancelTokenSource = new CancellationTokenSource();

            var startScaleX = this.ScaleX;
            var startScaleY = this.ScaleY;

            return AnimateAsync(value =>
            {
                this.ScaleX = startScaleX + (x - startScaleX) * value;
                this.ScaleY = startScaleY + (y - startScaleY) * value;
            },
            () =>
            {
                this.ScaleX = x;
                this.ScaleY = y;
            }, length, easing, _scaleCancelTokenSource);
        }


        CancellationTokenSource _translateCancelTokenSource;
        /// <summary>
        /// Translates the view from the current position to x,y, animator is reused if already running
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="length"></param>
        /// <param name="easing"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public Task TranslateToAsync(double x, double y, uint length = 250, Easing easing = null, CancellationTokenSource cancel = default)
        {
            // Cancel previous animation if it exists and is still running.
            _translateCancelTokenSource?.Cancel();
            _translateCancelTokenSource = new CancellationTokenSource();

            var startTranslationX = this.TranslationX;
            var startTranslationY = this.TranslationY;

            return AnimateAsync(value =>
            {
                this.TranslationX = (float)(startTranslationX + (x - startTranslationX) * value);
                this.TranslationY = (float)(startTranslationY + (y - startTranslationY) * value);
            },
            () =>
            {
                this.TranslationX = x;
                this.TranslationY = y;
            },
            length, easing, _translateCancelTokenSource);
        }

        CancellationTokenSource _rotateCancelTokenSource;
        /// <summary>
        /// Rotates the view from the current rotation to end, animator is reused if already running
        /// </summary>
        /// <param name="end"></param>
        /// <param name="length"></param>
        /// <param name="easing"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public Task RotateToAsync(double end, uint length = 250, Easing easing = null, CancellationTokenSource cancel = default)
        {
            // Cancel previous animation if it exists and is still running.
            _rotateCancelTokenSource?.Cancel();
            _rotateCancelTokenSource = new CancellationTokenSource();

            var startRotation = this.Rotation;

            return AnimateAsync(value =>
            {
                this.Rotation = (float)(startRotation + (end - startRotation) * value);
            },
            () =>
            {
                this.Rotation = end;
            },
            length, easing, _rotateCancelTokenSource);
        }


        public virtual void OnPrintDebug()
        {

        }

        public void PrintDebug(string indent = "     ")
        {
            Super.Log($"{indent}└─ {GetType().Name} {Width:0.0}x{Height:0.0} pts ({MeasuredSize.Pixels.Width:0.0}x{MeasuredSize.Pixels.Height:0.0} px)");
            OnPrintDebug();
            foreach (var view in Views)
            {
                Trace.Write($"{indent}    ├─ ");
                view.PrintDebug(indent + "    │  ");
            }
        }

        public static readonly BindableProperty DebugRenderingProperty = BindableProperty.Create(nameof(DebugRendering),
        typeof(bool),
        typeof(SkiaControl),
        false, propertyChanged: NeedDraw);
        public bool DebugRendering
        {
            get { return (bool)GetValue(DebugRenderingProperty); }
            set { SetValue(DebugRenderingProperty, value); }
        }

        float _debugRenderingCurrentOpacity;

        /// <summary>
        /// When the animator is cancelled if applyEndValueOnStop is true then the end value will be sent to your callback
        /// <param name="callback"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="length"></param>
        /// <param name="easing"></param>
        /// <param name="cancel"></param>
        /// <param name="onStopped"></param>
        /// <returns></returns>
        public Task AnimateRangeAsync(Action<double> callback, double start, double end, double length = 250, Easing easing = null, CancellationToken cancel = default, bool applyEndValueOnStop = false)
        {
            RangeAnimator animator = null;

            var tcs = new TaskCompletionSource<bool>(cancel);

            tcs.Task.ContinueWith(task =>
            {
                DisposeObject(animator);
            });

            animator = new RangeAnimator(this)
            {
                OnStop = () =>
                {
                    //if (animator.WasStarted && !cancel.IsCancellationRequested)
                    {
                        if (applyEndValueOnStop)
                            callback?.Invoke(end);
                        tcs.SetResult(true);
                    }
                }
            };
            animator.Start(
                (value) =>
                {
                    if (!cancel.IsCancellationRequested)
                    {
                        callback?.Invoke(value);
                    }
                    else
                    {
                        animator.Stop();
                    }
                },
                start, end, (uint)length, easing);

            return tcs.Task;
        }



        public SKPoint NotValidPoint()
        {
            return new SKPoint(float.NaN, float.NaN);
        }

        public bool PointIsValid(SKPoint point)
        {
            return !float.IsNaN(point.X) && !float.IsNaN(point.Y);
        }

        /// <summary>
        /// Is set by InvalidateMeasure();
        /// </summary>
        public SKSize SizeRequest { get; protected set; }

        /// <summary>
        /// Apply margins to SizeRequest
        /// </summary>
        /// <param name="widthConstraint"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public float AdaptWidthConstraintToRequest(float widthConstraint, Thickness constraints, double scale)
        {
            var widthPixels = (float)Math.Round(SizeRequest.Width * scale + constraints.HorizontalThickness);

            if (SizeRequest.Width >= 0)
                widthConstraint = widthPixels;

            return widthConstraint;
        }

        /// <summary>
        /// Apply margins to SizeRequest
        /// </summary>
        /// <param name="heightConstraint"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public float AdaptHeightContraintToRequest(float heightConstraint, Thickness constraints, double scale)
        {
            var thickness = constraints.VerticalThickness;

            var widthPixels = (float)Math.Round(SizeRequest.Height * scale + thickness);

            if (SizeRequest.Height >= 0)
                heightConstraint = widthPixels;

            return heightConstraint;
        }

        public virtual MeasuringConstraints GetMeasuringConstraints(MeasureRequest request)
        {
            var withLock = GetSizeRequest(request.WidthRequest, request.HeightRequest, true);
            var margins = GetMarginsInPixels(request.Scale);

            var adaptedWidthConstraint = AdaptWidthConstraintToRequest(withLock.Width, margins, request.Scale);
            var adaptedHeightConstraint = AdaptHeightContraintToRequest(withLock.Height, margins, request.Scale);

            var rectForChildrenPixels = GetMeasuringRectForChildren(adaptedWidthConstraint, adaptedHeightConstraint, request.Scale);

            return new MeasuringConstraints
            {
                Margins = margins,
                TotalMargins = GetAllMarginsInPixels(request.Scale),
                Request = new(adaptedWidthConstraint, adaptedHeightConstraint),
                Content = rectForChildrenPixels
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AdaptConstraintToContentRequest(
            float constraintPixels,
            double measuredDimension,
            double sideConstraintsPixels,
            bool autoSize,
            double minRequest, double maxRequest, float scale, bool canExpand)
        {

            var contentDimension = sideConstraintsPixels + measuredDimension;

            if (autoSize && measuredDimension >= 0 && (canExpand || measuredDimension < constraintPixels)
                || float.IsInfinity(constraintPixels))
            {
                constraintPixels = (float)contentDimension;
            }

            if (minRequest >= 0)
            {
                var min = double.MinValue;
                if (!double.IsInfinity(minRequest))
                {
                    min = Math.Round(minRequest * scale);
                }
                constraintPixels = (float)Math.Max(constraintPixels, min);
            }

            if (maxRequest >= 0)
            {
                var max = double.MaxValue;
                if (!double.IsInfinity(maxRequest))
                {
                    max = Math.Round(maxRequest * scale);
                }
                constraintPixels = (float)Math.Min(constraintPixels, max);
            }

            return (float)Math.Round(constraintPixels);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float AdaptWidthConstraintToContentRequest(MeasuringConstraints constraints, float contentWidthPixels, bool canExpand)
        {
            var sideConstraintsPixels = NeedAutoWidth
                ? constraints.TotalMargins.HorizontalThickness
                : constraints.Margins.HorizontalThickness;

            return AdaptConstraintToContentRequest(
                constraints.Request.Width,
                contentWidthPixels,
                sideConstraintsPixels,
                NeedAutoWidth,
                MinimumWidthRequest,
                MaximumWidthRequest,
                RenderingScale, canExpand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float AdaptHeightConstraintToContentRequest(MeasuringConstraints constraints,
            float contentHeightPixels, bool canExpand)
        {
            var sideConstraintsPixels = NeedAutoHeight
                ? constraints.TotalMargins.VerticalThickness
                : constraints.Margins.VerticalThickness;

            return AdaptConstraintToContentRequest(
                constraints.Request.Height,
                contentHeightPixels,
                sideConstraintsPixels,
                NeedAutoHeight,
                MinimumHeightRequest,
                MaximumHeightRequest,
                RenderingScale, canExpand);
        }


        public float AdaptWidthConstraintToContentRequest(float widthConstraintPixels,
            ScaledSize measuredContent, double sideConstraintsPixels)
        {
            return AdaptConstraintToContentRequest(
                widthConstraintPixels,
                measuredContent.Pixels.Width,
                sideConstraintsPixels,
                NeedAutoWidth,
                MinimumWidthRequest,
                MaximumWidthRequest,
                RenderingScale, this.HorizontalOptions.Expands);
        }


        public float AdaptHeightConstraintToContentRequest(float heightConstraintPixels,
            ScaledSize measuredContent,
            double sideConstraintsPixels)
        {

            return AdaptConstraintToContentRequest(
                heightConstraintPixels,
                measuredContent.Pixels.Height,
                sideConstraintsPixels,
                NeedAutoHeight,
                MinimumHeightRequest,
                MaximumHeightRequest,
                RenderingScale, this.VerticalOptions.Expands);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SKRect AdaptToContraints(SKRect measuredPixels,
            double constraintLeft,
            double constraintRight,
            double constraintTop,
            double constraintBottom)
        {
            double widthConstraintsPixels = constraintLeft + constraintRight;
            double heightConstraintsPixels = constraintTop + constraintBottom;
            var outPixels = measuredPixels.Clone();

            if (NeedAutoWidth)
            {
                if (outPixels.Width > 0
                    && outPixels.Width < widthConstraintsPixels)
                {
                    outPixels.Left -= (float)constraintLeft;
                    outPixels.Right += (float)constraintRight;
                }
            }

            if (NeedAutoHeight)
            {
                if (outPixels.Height > 0
                    && outPixels.Height < heightConstraintsPixels)
                {
                    outPixels.Top -= (float)constraintTop;
                    outPixels.Bottom += (float)constraintBottom;
                }
            }

            return outPixels;
        }



        /// <summary>
        /// In UNITS
        /// </summary>
        /// <param name="widthRequestPts"></param>
        /// <param name="heightRequestPts"></param>
        /// <returns></returns>
        protected Size AdaptSizeRequestToContent(double widthRequestPts, double heightRequestPts)
        {
            if (NeedAutoWidth && ContentSize.Units.Width > 0)
            {
                widthRequestPts = ContentSize.Units.Width + Padding.Left + Padding.Right;
            }
            if (NeedAutoHeight && ContentSize.Units.Height > 0)
            {
                heightRequestPts = ContentSize.Units.Height + Padding.Top + Padding.Bottom;
            }

            return new Size(widthRequestPts, heightRequestPts); ;
        }

        /// <summary>
        /// Execute post drawing operations, like post-animators etc
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scale"></param>
        protected void FinalizeDrawingWithRenderObject(SkiaDrawingContext context, double scale)
        {


        }

        /// <summary>
        /// Use Superview from public area
        /// </summary>
        /// <returns></returns>
        protected DrawnView GetTopParentView()
        {
            return GetParentElement(this) as DrawnView;
        }
        public static IDrawnBase GetParentElement(IDrawnBase control)
        {
            if (control is DrawnView)
            {
                return control;
            }

            if (control is SkiaControl skia)
            {
                if (skia.Parent != null)
                    return GetParentElement(skia.Parent);
            }

            return null;
        }

        /// <summary>
        /// To detect if current location is inside Destination
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool GestureIsInside(TouchActionEventArgs args, float offsetX = 0, float offsetY = 0)
        {

            return IsPixelInside((float)args.Location.X + offsetX, (float)args.Location.Y + offsetY);

            //            return IsPointInside((float)args.Location.X + offsetX, (float)args.Location.Y + offsetY, (float)RenderingScale);
        }


        /// <summary>
        /// To detect if a gesture Start point was inside Destination
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool GestureStartedInside(TouchActionEventArgs args, float offsetX = 0, float offsetY = 0)
        {
            return IsPixelInside((float)args.StartingLocation.X + offsetX, (float)args.StartingLocation.Y + offsetY);

            //            return IsPointInside((float)args.Distance.Start.X + offsetX, (float)args.Distance.Start.Y + offsetY, (float)RenderingScale);
        }

        /// <summary>
        /// Whether the point is inside Destination
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public bool IsPointInside(float x, float y, float scale)
        {
            return IsPointInside(DrawingRect, x, y, scale);
        }

        public bool IsPointInside(SKRect rect, float x, float y, float scale)
        {
            var xx = x * scale;
            var yy = y * scale;
            bool isInside = rect.ContainsInclusive(xx, yy);

            return isInside;
        }

        public bool IsPixelInside(SKRect rect, float x, float y)
        {
            bool isInside = rect.ContainsInclusive(x, y);
            return isInside;
        }



        /// <summary>
        /// Whether the pixel is inside Destination
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsPixelInside(float x, float y)
        {
            bool isInside = DrawingRect.Contains(x, y);
            return isInside;
        }

        protected virtual bool CheckGestureIsInsideChild(SkiaControl child, TouchActionEventArgs args, float offsetX = 0, float offsetY = 0)
        {
            return child.CanDraw && child.GestureIsInside(args, offsetX, offsetY);
        }

        protected virtual bool CheckGestureIsForChild(SkiaControl child, TouchActionEventArgs args, float offsetX = 0, float offsetY = 0)
        {
            return child.CanDraw && child.GestureStartedInside(args, offsetX, offsetY);
        }

        protected object LockIterateListeners = new();

        public static readonly BindableProperty LockChildrenGesturesProperty = BindableProperty.Create(nameof(LockChildrenGestures),
            typeof(LockTouch),
            typeof(SkiaControl),
            LockTouch.Disabled);

        /// <summary>
        /// What gestures are allowed to be passed to children below.
        /// If set to Enabled wit, otherwise can be more specific.
        /// </summary>
        public LockTouch LockChildrenGestures
        {
            get { return (LockTouch)GetValue(LockChildrenGesturesProperty); }
            set { SetValue(LockChildrenGesturesProperty, value); }
        }



        protected bool CheckChildrenGesturesLocked(TouchActionResult action)
        {
            switch (LockChildrenGestures)
            {
                case LockTouch.Enabled:
                return true;

                case LockTouch.Disabled:
                break;

                case LockTouch.PassTap:
                if (action != TouchActionResult.Tapped)
                    return true;
                break;

                case LockTouch.PassTapAndLongPress:
                if (action != TouchActionResult.Tapped && action != TouchActionResult.LongPressing)
                    return true;
                break;
            }

            return false;
        }

        public Dictionary<Guid, ISkiaGestureListener> HadInput { get; } = new(128);

        protected TouchActionResult _lastIncomingTouchAction;

        public virtual ISkiaGestureListener OnSkiaGestureEvent(TouchActionType type, TouchActionEventArgs args,
            TouchActionResult touchAction,
            SKPoint childOffset, SKPoint childOffsetDirect, ISkiaGestureListener wasConsumed)
        {
            if (touchAction == _lastIncomingTouchAction && touchAction == TouchActionResult.Up)
            {
                //a case when we were called for same event by parent and by some top level
                //because we previously has input and we got into HadInput to be notified of releases
                //so we have set up a filter here
                return null;
            }

            _lastIncomingTouchAction = touchAction;

            return ProcessGestures(type, args, touchAction, childOffset, childOffsetDirect, wasConsumed);
        }

        public virtual ISkiaGestureListener ProcessGestures(
            TouchActionType type,
            TouchActionEventArgs args,
            TouchActionResult touchAction,
            SKPoint childOffset, SKPoint childOffsetDirect, ISkiaGestureListener alreadyConsumed)
        {
            //Debug.WriteLine($"[IN] {type} {touchAction}");

            if (Superview == null)
            {
                //shit happened. we are capturing input but we are not supposed to be on the screen!
                Super.Log($"[OnGestureEvent] base captured by unassigned view {this.GetType()} {this.Tag}");
                return null;
            }

            if (TouchEffect.LogEnabled)
            {
                Super.Log($"[BASE] {this.Tag} Got {touchAction}.. {Uid}");
            }

            lock (LockIterateListeners)
            {
                ISkiaGestureListener consumed = null;
                ISkiaGestureListener tmpConsumed = alreadyConsumed;

                try
                {
                    if (CheckChildrenGesturesLocked(touchAction))
                        return null;

                    bool manageChildFocus = false;

                    var point = TranslateInputOffsetToPixels(args.Location, childOffset);

                    if (HadInput.Count > 0)
                    {
                        if (
                            (touchAction == TouchActionResult.Panned ||
                             touchAction == TouchActionResult.Panning ||
                             touchAction == TouchActionResult.Pinched ||
                             touchAction == TouchActionResult.Up))
                        {
                            foreach (var hadInput in HadInput.Values)
                            {
                                if (!hadInput.CanDraw || hadInput.InputTransparent)
                                {
                                    continue;
                                }
                                consumed = hadInput.OnSkiaGestureEvent(type, args, touchAction, TranslateInputCoords(childOffset, true),
                                    TranslateInputCoords(childOffsetDirect, false), tmpConsumed);
                                if (consumed != null)
                                {
                                    if (tmpConsumed == null)
                                        tmpConsumed = consumed;

                                    if (touchAction != TouchActionResult.Up)
                                        break;
                                }
                            }
                        }
                        else
                        {
                            HadInput.Clear();
                        }
                    }

                    var hadInputConsumed = consumed;

                    if (consumed == null || touchAction == TouchActionResult.Up)// !GestureListeners.Contains(consumed))
                        foreach (var listener in GestureListeners)
                        {
                            if (!listener.CanDraw || listener.InputTransparent)
                                continue;

                            if (HadInput.Values.Contains(listener) &&
                                (touchAction == TouchActionResult.Panned ||
                                touchAction == TouchActionResult.Panning ||
                                touchAction == TouchActionResult.Pinched ||
                                touchAction == TouchActionResult.Up))
                            {
                                continue;
                            }

                            //Debug.WriteLine($"Checking {listener} gestures in {this.Tag} {this}");

                            if (listener == Superview.FocusedChild)
                                manageChildFocus = true;

                            var forChild = IsGestureForChild(listener, point);

                            if (TouchEffect.LogEnabled)
                            {
                                if (listener is SkiaControl c)
                                {
                                    Debug.WriteLine($"[BASE] for child {forChild} {c.Tag} at {point.X:0},{point.Y:0} -> {c.HitBoxAuto} ");
                                }
                            }

                            if (forChild)
                            {
                                if (manageChildFocus && listener == Superview.FocusedChild)
                                {
                                    manageChildFocus = false;
                                }

                                //Log($"[OnGestureEvent] sent {touchAction} to {listener.Tag}");
                                consumed = listener.OnSkiaGestureEvent(type, args, touchAction,
                                    TranslateInputCoords(childOffset, true),
                                    TranslateInputCoords(childOffsetDirect, false),
                                    tmpConsumed);

                                if (consumed != null)
                                {
                                    if (tmpConsumed == null)
                                        tmpConsumed = consumed;

                                    if (touchAction != TouchActionResult.Up)
                                    {
                                        HadInput.TryAdd(listener.Uid, consumed);
                                    }
                                    break;
                                }
                            }
                        }

                    if (HadInput.Count > 0 && touchAction == TouchActionResult.Up)
                    {
                        HadInput.Clear();
                    }

                    if (manageChildFocus)
                    {
                        Superview.FocusedChild = null;
                    }

                    if (hadInputConsumed != null)
                        consumed = hadInputConsumed;

                }
                catch (Exception e)
                {
                    Super.Log(e);
                }

                return consumed;
            }
        }

        public bool UpdateLocked { get; set; }

        public void LockUpdate(bool value)
        {
            UpdateLocked = value;
        }

        private void Init()
        {
            NeedMeasure = true;
            IsLayoutDirty = true;

            SizeChanged += ViewSizeChanged;

            CalculateMargins();
            CalculateSizeRequest();
        }

        public static readonly BindableProperty ParentProperty = BindableProperty.Create(nameof(Parent),
        typeof(IDrawnBase),
        typeof(SkiaControl),
        null, propertyChanged: OnControlParentChanged);
        /// <summary>
        /// Do not set this directly if you don't know what you are doing, use SetParent()
        /// </summary>
        public new IDrawnBase Parent
        {
            get { return (IDrawnBase)GetValue(ParentProperty); }
            set { SetValue(ParentProperty, value); }
        }


        #region View


        public static readonly BindableProperty VerticalOptionsProperty = BindableProperty.Create(nameof(VerticalOptions),
        typeof(LayoutOptions),
        typeof(SkiaControl),
        LayoutOptions.Start,
        propertyChanged: NeedInvalidateMeasure);

        public LayoutOptions VerticalOptions
        {
            get { return (LayoutOptions)GetValue(VerticalOptionsProperty); }
            set { SetValue(VerticalOptionsProperty, value); }
        }

        public static readonly BindableProperty HorizontalOptionsProperty = BindableProperty.Create(nameof(HorizontalOptions),
            typeof(LayoutOptions),
            typeof(SkiaControl),
            LayoutOptions.Start,
            propertyChanged: NeedInvalidateMeasure);

        public LayoutOptions HorizontalOptions
        {
            get { return (LayoutOptions)GetValue(HorizontalOptionsProperty); }
            set { SetValue(HorizontalOptionsProperty, value); }
        }

        /// <summary>
        /// todo override for templated skialayout to use ViewsProvider
        /// </summary>
        /// <param name="newvalue"></param>
        protected virtual void OnParentVisibilityChanged(bool newvalue)
        {
            if (!newvalue)
            {
                //DestroyRenderingObject();
            }

            Superview?.SetViewTreeVisibilityByParent(this, newvalue);

            try
            {
                foreach (var child in Views)
                {
                    child.OnParentVisibilityChanged(newvalue);
                }
            }
            catch (Exception e)
            {
                Super.Log(e);
            }
        }

        /// <summary>
        /// todo override for templated skialayout to use ViewsProvider
        /// </summary>
        /// <param name="newvalue"></param>
        public virtual void OnVisibilityChanged(bool newvalue)
        {
            if (!newvalue)
            {
                //DestroyRenderingObject();
            }
            // need to this to:
            // disable child gesture listeners
            // pause hidden animations
            try
            {
                var pass = IsVisible && newvalue;
                foreach (var child in Views)
                {
                    child.OnParentVisibilityChanged(pass);
                }
            }
            catch (Exception e)
            {
                Super.Log(e);
            }
        }

        /// <summary>
        /// Base performs some cleanup actions with Superview
        /// </summary>
        public virtual void OnDisposing()
        {
            WhenDisposing?.Invoke(this);
            Superview?.UnregisterGestureListener(this as ISkiaGestureListener);
            Superview?.UnregisterAllAnimatorsByParent(this);
        }

        protected object lockPausingAnimators = new();

        public virtual void PauseAllAnimators()
        {
            var paused = Superview?.SetPauseStateOfAllAnimatorsByParent(this, true);
        }

        public virtual void ResumePausedAnimators()
        {
            var resumed = Superview?.SetPauseStateOfAllAnimatorsByParent(this, false);
        }

        public static readonly BindableProperty IsGhostProperty = BindableProperty.Create(nameof(IsGhost),
            typeof(bool),
            typeof(SkiaControl),
            false,
            propertyChanged: NeedDraw);
        public bool IsGhost
        {
            get { return (bool)GetValue(IsGhostProperty); }
            set { SetValue(IsGhostProperty, value); }
        }

        public static readonly BindableProperty IgnoreChildrenInvalidationsProperty
        = BindableProperty.Create(nameof(IgnoreChildrenInvalidations),
        typeof(bool), typeof(SkiaControl),
        false);

        public bool IgnoreChildrenInvalidations
        {
            get
            {
                return (bool)GetValue(IgnoreChildrenInvalidationsProperty);
            }
            set
            {
                SetValue(IgnoreChildrenInvalidationsProperty, value);
            }
        }

        public static readonly BindableProperty ClearColorProperty = BindableProperty.Create(nameof(ClearColor), typeof(Color), typeof(SkiaControl),
            Color.Transparent,
            propertyChanged: NeedDraw);
        public Color ClearColor
        {
            get { return (Color)GetValue(ClearColorProperty); }
            set { SetValue(ClearColorProperty, value); }
        }



        #region FillGradient

        public static readonly BindableProperty FillGradientProperty = BindableProperty.Create(nameof(FillGradient),
            typeof(SkiaGradient), typeof(SkiaControl),
            null,
            propertyChanged: NeedDraw);
        public SkiaGradient FillGradient
        {
            get { return (SkiaGradient)GetValue(FillGradientProperty); }
            set { SetValue(FillGradientProperty, value); }
        }

        public bool HasFillGradient
        {
            get
            {
                return this.FillGradient != null && this.FillGradient.Type != GradientType.None;
            }
        }


        #endregion

        public SKSize GetSizeRequest(float widthConstraint, float heightConstraint, bool insideLayout)
        {
            //if (insideLayout)
            //{

            //}

            widthConstraint *= (float)this.WidthRequestRatio;
            heightConstraint *= (float)this.HeightRequestRatio;

            if (LockRatio > 0)
            {
                var lockValue = (float)Math.Max(widthConstraint, heightConstraint);
                return new SKSize(lockValue, lockValue);
            }

            if (LockRatio < 0)
            {
                var lockValue = (float)Math.Min(widthConstraint, heightConstraint);
                return new SKSize(lockValue, lockValue);
            }

            return new SKSize(widthConstraint, heightConstraint);
        }

        public static readonly BindableProperty ViewportHeightLimitProperty = BindableProperty.Create(
            nameof(ViewportHeightLimit),
            typeof(double),
            typeof(SkiaControl),
            -1.0, propertyChanged: NeedInvalidateViewport);

        /// <summary>
        /// Will be used inside GetDrawingRectWithMargins to limit the height of the DrawingRect
        /// </summary>
        public double ViewportHeightLimit
        {
            get { return (double)GetValue(ViewportHeightLimitProperty); }
            set { SetValue(ViewportHeightLimitProperty, value); }
        }

        public static readonly BindableProperty ViewportWidthLimitProperty = BindableProperty.Create(
            nameof(ViewportWidthLimit),
            typeof(double),
            typeof(SkiaControl),
            -1.0, propertyChanged: NeedInvalidateViewport);

        /// <summary>
        /// Will be used inside GetDrawingRectWithMargins to limit the width of the DrawingRect
        /// </summary>
        public double ViewportWidthLimit
        {
            get { return (double)GetValue(ViewportWidthLimitProperty); }
            set { SetValue(ViewportWidthLimitProperty, value); }
        }


        private double _height = -1;
        public new double Height
        {
            get
            {
                return _height;
            }
            set
            {
                if (_height != value)
                {
                    _height = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _width = -1;
        public new double Width
        {
            get
            {
                return _width;
            }
            set
            {
                if (_width != value)
                {
                    _width = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Please use ScaleX, ScaleY instead of this maui property
        /// </summary>
        public new double Scale
        {
            get
            {
                return Math.Min(ScaleX, ScaleY);
            }
            set
            {
                ScaleX = value;
                ScaleY = value;
            }
        }

        public static readonly BindableProperty LockRatioProperty = BindableProperty.Create(nameof(LockRatio),
            typeof(double), typeof(SkiaControl),
            0.0,
            propertyChanged: NeedInvalidateMeasure);

        /// <summary>
        /// Locks the final size to the min (-1.0 <-> 0.0) or max (0.0 <-> 1.0) of the provided size.
        /// </summary>
        public double LockRatio
        {
            get { return (double)GetValue(LockRatioProperty); }
            set { SetValue(LockRatioProperty, value); }
        }

        public static readonly BindableProperty HeightRequestRatioProperty = BindableProperty.Create(
            nameof(HeightRequestRatio),
            typeof(double),
            typeof(SkiaControl),
            1.0,
            propertyChanged: NeedInvalidateMeasure);

        /// <summary>
        /// HeightRequest Multiplier, default is 1.0
        /// </summary>
        public double HeightRequestRatio
        {
            get { return (double)GetValue(HeightRequestRatioProperty); }
            set { SetValue(HeightRequestRatioProperty, value); }
        }

        public static readonly BindableProperty WidthRequestRatioProperty = BindableProperty.Create(
            nameof(WidthRequestRatio),
            typeof(double),
            typeof(SkiaControl),
            1.0,
            propertyChanged: NeedInvalidateMeasure);

        /// <summary>
        /// WidthRequest Multiplier, default is 1.0
        /// </summary>
        public double WidthRequestRatio
        {
            get { return (double)GetValue(WidthRequestRatioProperty); }
            set { SetValue(WidthRequestRatioProperty, value); }
        }

        public static readonly BindableProperty HorizontalFillRatioProperty = BindableProperty.Create(
            nameof(HorizontalFillRatio),
            typeof(double),
            typeof(SkiaControl),
            1.0,
            propertyChanged: NeedInvalidateMeasure);

        public double HorizontalFillRatio
        {
            get { return (double)GetValue(HorizontalFillRatioProperty); }
            set { SetValue(HorizontalFillRatioProperty, value); }
        }

        public static readonly BindableProperty VerticalFillRatioProperty = BindableProperty.Create(
            nameof(VerticalFillRatio),
            typeof(double),
            typeof(SkiaControl),
            1.0,
            propertyChanged: NeedInvalidateMeasure);

        public double VerticalFillRatio
        {
            get { return (double)GetValue(VerticalFillRatioProperty); }
            set { SetValue(VerticalFillRatioProperty, value); }
        }

        public static readonly BindableProperty HorizontalPositionOffsetRatioProperty = BindableProperty.Create(
            nameof(HorizontalPositionOffsetRatio),
            typeof(double),
            typeof(SkiaControl),
            0.0,
            propertyChanged: NeedDraw);

        public double HorizontalPositionOffsetRatio
        {
            get { return (double)GetValue(HorizontalPositionOffsetRatioProperty); }
            set { SetValue(HorizontalPositionOffsetRatioProperty, value); }
        }

        public static readonly BindableProperty VerticalPositionOffsetRatioProperty = BindableProperty.Create(
            nameof(VerticalPositionOffsetRatio),
            typeof(double),
            typeof(SkiaControl),
            0.0,
            propertyChanged: NeedDraw);

        public double VerticalPositionOffsetRatio
        {
            get { return (double)GetValue(VerticalPositionOffsetRatioProperty); }
            set { SetValue(VerticalPositionOffsetRatioProperty, value); }
        }

        public static readonly BindableProperty FillBlendModeProperty = BindableProperty.Create(nameof(FillBlendMode),
            typeof(SKBlendMode), typeof(SkiaControl),
            SKBlendMode.SrcOver,
            propertyChanged: NeedDraw);
        public SKBlendMode FillBlendMode
        {
            get { return (SKBlendMode)GetValue(FillBlendModeProperty); }
            set { SetValue(FillBlendModeProperty, value); }
        }


        public static readonly BindableProperty MaximumWidthRequestProperty = BindableProperty.Create(
            nameof(MaximumWidthRequest),
            typeof(double),
            typeof(SkiaControl),
            -1.0);

        public double MaximumWidthRequest
        {
            get { return (double)GetValue(MaximumWidthRequestProperty); }
            set { SetValue(MaximumWidthRequestProperty, value); }
        }

        public static readonly BindableProperty MaximumHeightRequestProperty = BindableProperty.Create(
            nameof(MaximumHeightRequest),
            typeof(double),
            typeof(SkiaControl),
            -1.0);

        public double MaximumHeightRequest
        {
            get { return (double)GetValue(MaximumHeightRequestProperty); }
            set { SetValue(MaximumHeightRequestProperty, value); }
        }


        /*

        disabled this for fps... we can use drawingrect.x and y /renderingscale but OnPropertyChanged calls might slow us down?..

        private double _X;
        public double X
        {
            get
            {
                return _X;
            }
            set
            {
                if (_X != value)
                {
                    _X = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _Y;
        public double Y
        {
            get
            {
                return _Y;
            }
            set
            {
                if (_Y != value)
                {
                    _Y = value;
                    OnPropertyChanged();
                }
            }
        }

        */

        //public static readonly BindableProperty HeightProperty = BindableProperty.Create(nameof(Height),
        //	typeof(double), typeof(SkiaControl),
        //	-1.0);
        //public double Height
        //{
        //	get { return (double)GetValue(HeightProperty); }
        //	set { SetValue(HeightProperty, value); }
        //}


        //public static readonly BindableProperty WidthProperty = BindableProperty.Create(nameof(Width),
        //	typeof(double), typeof(SkiaControl),
        //	-1.0);
        //public double Width
        //{
        //	get { return (double)GetValue(WidthProperty); }
        //	set { SetValue(WidthProperty, value); }
        //}


        /*

        public static readonly BindableProperty OpacityProperty = BindableProperty.Create(nameof(Opacity),
            typeof(double), typeof(SkiaControl),
            0.0,
            propertyChanged: RedrawCanvas);
        public double Opacity
        {
            get { return (double)GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }
        */


        public static readonly BindableProperty SkewXProperty
        = BindableProperty.Create(nameof(SkewX),
        typeof(float), typeof(SkiaControl),
        0.0f, propertyChanged: NeedRepaint);

        public float SkewX
        {
            get
            {
                return (float)GetValue(SkewXProperty);
            }
            set
            {
                SetValue(SkewXProperty, value);
            }
        }

        public static readonly BindableProperty SkewYProperty
        = BindableProperty.Create(nameof(SkewY),
        typeof(float), typeof(SkiaControl),
        0.0f, propertyChanged: NeedRepaint);

        public float SkewY
        {
            get
            {
                return (float)GetValue(SkewYProperty);
            }
            set
            {
                SetValue(SkewYProperty, value);
            }
        }

        public static readonly BindableProperty CameraAngleXProperty
            = BindableProperty.Create(nameof(CameraAngleX),
                typeof(float), typeof(SkiaControl),
                0.0f, propertyChanged: NeedRepaint);

        public float CameraAngleX
        {
            get
            {
                return (float)GetValue(CameraAngleXProperty);
            }
            set
            {
                SetValue(CameraAngleXProperty, value);
            }
        }

        public static readonly BindableProperty CameraAngleYProperty
            = BindableProperty.Create(nameof(CameraAngleY),
                typeof(float), typeof(SkiaControl),
                0.0f, propertyChanged: NeedRepaint);

        public float CameraAngleY
        {
            get
            {
                return (float)GetValue(CameraAngleYProperty);
            }
            set
            {
                SetValue(CameraAngleYProperty, value);
            }
        }

        public static readonly BindableProperty CameraAngleZProperty
            = BindableProperty.Create(nameof(CameraAngleZ),
                typeof(float), typeof(SkiaControl),
                0.0f, propertyChanged: NeedRepaint);

        public float CameraAngleZ
        {
            get
            {
                return (float)GetValue(CameraAngleZProperty);
            }
            set
            {
                SetValue(CameraAngleZProperty, value);
            }
        }

        public static readonly BindableProperty CameraTranslationZProperty
            = BindableProperty.Create(nameof(CameraTranslationZ),
                typeof(float), typeof(SkiaControl),
                0.0f, propertyChanged: NeedRepaint);

        public float CameraTranslationZ
        {
            get
            {
                return (float)GetValue(CameraTranslationZProperty);
            }
            set
            {
                SetValue(CameraTranslationZProperty, value);
            }
        }



        //public new static readonly BindableProperty VerticalOptionsProperty = BindableProperty.Create(nameof(VerticalOptions),
        //    typeof(LayoutOptions),
        //    typeof(SkiaControl),
        //    LayoutOptions.Start,
        //    propertyChanged: NeedInvalidateMeasure);

        //public new LayoutOptions VerticalOptions
        //{
        //    get { return (LayoutOptions)GetValue(VerticalOptionsProperty); }
        //    set { SetValue(VerticalOptionsProperty, value); }
        //}

        //public new  static readonly BindableProperty HorizontalOptionsProperty = BindableProperty.Create(nameof(HorizontalOptions),
        //    typeof(LayoutOptions),
        //    typeof(SkiaControl),
        //    LayoutOptions.Start,
        //    propertyChanged: NeedInvalidateMeasure);

        //public new LayoutOptions HorizontalOptions
        //{
        //    get { return (LayoutOptions)GetValue(HorizontalOptionsProperty); }
        //    set { SetValue(HorizontalOptionsProperty, value); }
        //}


        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            //if (!isApplyingStyle && !string.IsNullOrEmpty(propertyName))
            //{
            //    ExplicitPropertiesSet[propertyName] = true;
            //}

            #region intercept properties coming from VisualElement..

            //some VisualElement props will not call this method so we would override them as new

            if (propertyName.IsEither(nameof(ZIndex)))
            {
                Parent?.InvalidateViewsList();
                Repaint();
            }
            else
            if (propertyName.IsEither(
                    nameof(Opacity),
                    nameof(TranslationX), nameof(TranslationY),
                    nameof(Rotation),
                    nameof(ScaleX), nameof(ScaleY)
                    ))
            {
                Repaint();
            }
            else
            if (propertyName.IsEither(nameof(BackgroundColor),
                    nameof(HorizontalOptions), nameof(VerticalOptions),
                    nameof(IsClippedToBounds),
                    nameof(Padding)))
            {
                Update();
            }
            else
            if (propertyName.IsEither(
                    nameof(HeightRequest), nameof(WidthRequest),
                    nameof(MaximumWidthRequest), nameof(MinimumWidthRequest),
                    nameof(MaximumHeightRequest), nameof(MinimumHeightRequest)
                ))
            {
                InvalidateMeasure();
            }
            else
            if (propertyName.IsEither(nameof(IsVisible)))
            {
                OnVisibilityChanged(IsVisible);

                InvalidateMeasure();
            }
            else
            if (propertyName.IsEither(
                    nameof(AnchorX), nameof(AnchorY),
                    nameof(RotationX), nameof(RotationY)))
            {
                //todo add option not to throw?..
                throw new NotImplementedException("DrawnUi is not using this Maui VisualElement property.");
            }

            #endregion
        }

        public static readonly BindableProperty Perspective1Property
        = BindableProperty.Create(nameof(Perspective1),
        typeof(float), typeof(SkiaControl),
        0.0f, propertyChanged: NeedRepaint);

        public float Perspective1
        {
            get
            {
                return (float)GetValue(Perspective1Property);
            }
            set
            {
                SetValue(Perspective1Property, value);
            }
        }

        public static readonly BindableProperty Perspective2Property
        = BindableProperty.Create(nameof(Perspective2),
        typeof(float), typeof(SkiaControl),
          0.0f, propertyChanged: NeedRepaint);

        public float Perspective2
        {
            get
            {
                return (float)GetValue(Perspective2Property);
            }
            set
            {
                SetValue(Perspective2Property, value);
            }
        }

        public static readonly BindableProperty TransformPivotPointXProperty
        = BindableProperty.Create(nameof(TransformPivotPointX),
        typeof(double), typeof(SkiaControl),
        0.5, propertyChanged: NeedRepaint);
        public double TransformPivotPointX
        {
            get
            {
                return (double)GetValue(TransformPivotPointXProperty);
            }
            set
            {
                SetValue(TransformPivotPointXProperty, value);
            }
        }

        public static readonly BindableProperty TransformPivotPointYProperty
        = BindableProperty.Create(nameof(TransformPivotPointY),
        typeof(double), typeof(SkiaControl),
        0.5, propertyChanged: NeedRepaint);

        public double TransformPivotPointY
        {
            get
            {
                return (double)GetValue(TransformPivotPointYProperty);
            }
            set
            {
                SetValue(TransformPivotPointYProperty, value);
            }
        }

        public static readonly BindableProperty ZIndexProperty
            = BindableProperty.Create(nameof(ZIndex),
                typeof(int), typeof(SkiaControl),
                0);

        public int ZIndex
        {
            get
            {
                return (int)GetValue(ZIndexProperty);
            }
            set
            {
                SetValue(ZIndexProperty, value);
            }
        }

        private static void OnControlParentChanged(BindableObject bindable, object oldvalue, object newvalue)
        {
            if (bindable is SkiaControl control)
            {
                control.OnParentChanged(newvalue as IDrawnBase, oldvalue as IDrawnBase);
            }
        }

        public new event EventHandler<IDrawnBase> ParentChanged;

        public static readonly BindableProperty AdjustClippingProperty = BindableProperty.Create(
            nameof(AdjustClipping),
            typeof(Thickness),
            typeof(SkiaControl),
            default(Thickness),
            propertyChanged: NeedInvalidateMeasure);

        public Thickness AdjustClipping
        {
            get { return (Thickness)GetValue(AdjustClippingProperty); }
            set { SetValue(AdjustClippingProperty, value); }
        }


        public static readonly BindableProperty PaddingProperty = BindableProperty.Create(nameof(Padding), typeof(Thickness),
            typeof(SkiaControl), default(Thickness),
            propertyChanged: NeedInvalidateMeasure);
        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        public static readonly BindableProperty MarginProperty = BindableProperty.Create(nameof(Margin), typeof(Thickness),
            typeof(SkiaControl), default(Thickness),
            propertyChanged: NeedInvalidateMeasure);
        public Thickness Margin
        {
            get { return (Thickness)GetValue(MarginProperty); }
            set { SetValue(MarginProperty, value); }
        }

        public static readonly BindableProperty MarginTopProperty = BindableProperty.Create(
            nameof(MarginTop),
            typeof(double),
            typeof(SkiaControl),
            -1.0, propertyChanged: NeedInvalidateMeasure);

        public double MarginTop
        {
            get { return (double)GetValue(MarginTopProperty); }
            set { SetValue(MarginTopProperty, value); }
        }

        public static readonly BindableProperty MarginBottomProperty = BindableProperty.Create(
            nameof(MarginBottom),
            typeof(double),
            typeof(SkiaControl),
            -1.0, propertyChanged: NeedInvalidateMeasure);

        public double MarginBottom
        {
            get { return (double)GetValue(MarginBottomProperty); }
            set { SetValue(MarginBottomProperty, value); }
        }

        public static readonly BindableProperty MarginLeftProperty = BindableProperty.Create(
            nameof(MarginLeft),
            typeof(double),
            typeof(SkiaControl),
            -1.0, propertyChanged: NeedInvalidateMeasure);

        public double MarginLeft
        {
            get { return (double)GetValue(MarginLeftProperty); }
            set { SetValue(MarginLeftProperty, value); }
        }

        public static readonly BindableProperty MarginRightProperty = BindableProperty.Create(
            nameof(MarginRight),
            typeof(double),
            typeof(SkiaControl),
            -1.0, propertyChanged: NeedInvalidateMeasure);

        public double MarginRight
        {
            get { return (double)GetValue(MarginRightProperty); }
            set { SetValue(MarginRightProperty, value); }
        }

        public static readonly BindableProperty AddMarginTopProperty = BindableProperty.Create(
            nameof(AddMarginTop),
            typeof(double),
            typeof(SkiaControl),
            0.0, propertyChanged: NeedInvalidateMeasure);

        public double AddMarginTop
        {
            get { return (double)GetValue(AddMarginTopProperty); }
            set { SetValue(AddMarginTopProperty, value); }
        }

        public static readonly BindableProperty AddMarginBottomProperty = BindableProperty.Create(
            nameof(AddMarginBottom),
            typeof(double),
            typeof(SkiaControl),
            0.0, propertyChanged: NeedInvalidateMeasure);

        public double AddMarginBottom
        {
            get { return (double)GetValue(AddMarginBottomProperty); }
            set { SetValue(AddMarginBottomProperty, value); }
        }

        public static readonly BindableProperty AddMarginLeftProperty = BindableProperty.Create(
            nameof(AddMarginLeft),
            typeof(double),
            typeof(SkiaControl),
            0.0, propertyChanged: NeedInvalidateMeasure);

        public double AddMarginLeft
        {
            get { return (double)GetValue(AddMarginLeftProperty); }
            set { SetValue(AddMarginLeftProperty, value); }
        }

        public static readonly BindableProperty AddMarginRightProperty = BindableProperty.Create(
            nameof(AddMarginRight),
            typeof(double),
            typeof(SkiaControl),
            0.0, propertyChanged: NeedInvalidateMeasure);

        public double AddMarginRight
        {
            get { return (double)GetValue(AddMarginRightProperty); }
            set { SetValue(AddMarginRightProperty, value); }
        }

        /// <summary>
        /// Total calculated margins in points
        /// </summary>
        public Thickness Margins
        {
            get => _margins;
            protected set
            {
                if (value.Equals(_margins)) return;
                _margins = value;
                OnPropertyChanged();
            }
        }

        public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(double),
            typeof(SkiaControl),
            8.0,
            propertyChanged: NeedInvalidateMeasure);
        public double Spacing
        {
            get { return (double)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }

        public static readonly BindableProperty AddTranslationYProperty = BindableProperty.Create(
            nameof(AddTranslationY),
            typeof(double),
            typeof(SkiaControl),
            0.0, propertyChanged: NeedRepaint);

        public double AddTranslationY
        {
            get { return (double)GetValue(AddTranslationYProperty); }
            set { SetValue(AddTranslationYProperty, value); }
        }

        public static readonly BindableProperty AddTranslationXProperty = BindableProperty.Create(
            nameof(AddTranslationX),
            typeof(double),
            typeof(SkiaControl),
            0.0, propertyChanged: NeedRepaint);

        public double AddTranslationX
        {
            get { return (double)GetValue(AddTranslationXProperty); }
            set { SetValue(AddTranslationXProperty, value); }
        }

        public static readonly BindableProperty ExpandCacheRecordingAreaProperty
            = BindableProperty.Create(nameof(ExpandCacheRecordingArea),
                typeof(double), typeof(SkiaControl),
                0.0, propertyChanged: NeedDraw);
        /// <summary>
        /// Normally cache is recorded inside DrawingRect, but you might want to exapnd this to include shadows around, for example.
        /// Specify number of points by which you want to expand the recording area.
        /// Also you might maybe want to include a bigger area if your control is not inside the DrawingRect due to transforms/translations.
        /// Override GetCacheRecordingArea method for a similar action.
        /// </summary>
        public double ExpandCacheRecordingArea
        {
            get
            {
                return (double)GetValue(ExpandCacheRecordingAreaProperty);
            }
            set
            {
                SetValue(ExpandCacheRecordingAreaProperty, value);
            }
        }

        /*
        public static readonly BindableProperty AlignContentVerticalProperty = BindableProperty.Create(nameof(AlignContentVertical),
            typeof(LayoutOptions),
            typeof(SkiaControl),
            LayoutOptions.Start,
            propertyChanged: NeedInvalidateMeasure);
        public LayoutOptions AlignContentVertical
        {
            get { return (LayoutOptions)GetValue(AlignContentVerticalProperty); }
            set { SetValue(AlignContentVerticalProperty, value); }
        }

        public static readonly BindableProperty AlignContentHorizontalProperty = BindableProperty.Create(nameof(AlignContentHorizontal),
            typeof(LayoutOptions),
            typeof(SkiaControl),
            LayoutOptions.Start,
            propertyChanged: NeedInvalidateMeasure);
        public LayoutOptions AlignContentHorizontal
        {
            get { return (LayoutOptions)GetValue(AlignContentHorizontalProperty); }
            set { SetValue(AlignContentHorizontalProperty, value); }
        }
        */


        public static readonly BindableProperty IsClippedToBoundsProperty = BindableProperty.Create(nameof(IsClippedToBounds),
            typeof(bool), typeof(SkiaControl), false,
            propertyChanged: NeedInvalidateMeasure);
        /// <summary>
        /// This cuts shadows etc. You might want to enable it for some cases as it speeds up the rendering, it is False by default
        /// </summary>
        public bool IsClippedToBounds
        {
            get { return (bool)GetValue(IsClippedToBoundsProperty); }
            set { SetValue(IsClippedToBoundsProperty, value); }
        }

        public static readonly BindableProperty ClipEffectsProperty = BindableProperty.Create(nameof(ClipEffects),
            typeof(bool), typeof(SkiaControl), true,
            propertyChanged: NeedInvalidateMeasure);
        /// <summary>
        /// This cuts shadows etc
        /// </summary>
        public virtual bool ClipEffects
        {
            get { return (bool)GetValue(ClipEffectsProperty); }
            set { SetValue(ClipEffectsProperty, value); }
        }

        public static readonly BindableProperty BindableTriggerProperty = BindableProperty.Create(nameof(BindableTrigger),
            typeof(object), typeof(SkiaControl),
            null,
            propertyChanged: TriggerPropertyChanged);

        private static void TriggerPropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
        {
            if (bindable is SkiaControl control)
            {
                control.OnTriggerChanged();
            }
        }

        public virtual void OnTriggerChanged()
        {

        }

        public object BindableTrigger
        {
            get { return (object)GetValue(BindableTriggerProperty); }
            set { SetValue(BindableTriggerProperty, value); }
        }

        public static readonly BindableProperty Value1Property = BindableProperty.Create(nameof(Value1),
            typeof(double), typeof(SkiaControl),
            0.0,
            propertyChanged: NeedDraw);
        public double Value1
        {
            get { return (double)GetValue(Value1Property); }
            set { SetValue(Value1Property, value); }
        }

        public static readonly BindableProperty Value2Property = BindableProperty.Create(nameof(Value2),
            typeof(double), typeof(SkiaControl),
            0.0,
            propertyChanged: NeedDraw);
        public double Value2
        {
            get { return (double)GetValue(Value2Property); }
            set { SetValue(Value2Property, value); }
        }

        public static readonly BindableProperty Value3Property = BindableProperty.Create(nameof(Value3),
            typeof(double), typeof(SkiaControl),
            0.0,
            propertyChanged: NeedDraw);
        public double Value3
        {
            get { return (double)GetValue(Value3Property); }
            set { SetValue(Value3Property, value); }
        }

        //-------------------------------------------------------------
        // Value4
        //-------------------------------------------------------------
        public static readonly BindableProperty Value4Property = BindableProperty.Create(nameof(Value4),
            typeof(double), typeof(SkiaControl),
            0.0,
            propertyChanged: NeedDraw);
        public double Value4
        {
            get { return (double)GetValue(Value4Property); }
            set { SetValue(Value4Property, value); }
        }


        public static readonly BindableProperty RenderingScaleProperty = BindableProperty.Create(nameof(RenderingScale),
            typeof(float), typeof(SkiaControl),
            -1.0f, propertyChanged: NeedUpdateScale);

        private static void NeedUpdateScale(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SkiaControl control)
            {
                control.OnScaleChanged();
            }
        }

        public float RenderingScale
        {
            get
            {
                var value = -1f;
                try
                {
                    value = (float)GetValue(RenderingScaleProperty);
                }
                catch (Exception e)
                {
                    Super.Log(e); //catching Nullable object must have a value, is this because of NET8?
                }
                if (value <= 0)
                {
                    return GetDensity();
                }
                return value;
            }
            set
            {
                SetValue(RenderingScaleProperty, value);
            }
        }

        public static float GetDensity()
        {
            return (float)Screen.DisplayInfo.Density;
        }

        //public double RenderingScaleSafe
        //{
        //    get
        //    {
        //        var value = Density;
        //        if (value == 0)
        //            value = 1;

        //        if (RenderingScale < 0 || RenderingScale == 0)
        //        {
        //            return value;
        //        }

        //        return RenderingScale;
        //    }
        //}



        #endregion


        public SKRect RenderedAtDestination { get; set; }

        public virtual void OnScaleChanged()
        {
            InvalidateMeasure();
        }


        private Style _currentStyle;


        private void UnsubscribeFromOldStyle()
        {
            if (_currentStyle != null)
            {

            }
        }

        public virtual void SetPropertyValue(BindableProperty property, object value)
        {
            this.SetValue(property, value);
        }


        public Guid Uid { get; set; } = Guid.NewGuid();

        //todo check adapt for MAUI

        public static double DegreesToRadians(double value)
        {
            return ((value * Math.PI) / 180);
        }
        public static double RadiansToDegrees(double value)
        {
            return value * 180 / Math.PI;
        }

        public static (double X1, double Y1, double X2, double Y2) LinearGradientAngleToPoints(double direction)
        {
            //adapt to css style
            direction -= 90;

            //allow negative angles
            if (direction < 0)
                direction = 360 + direction;

            if (direction > 360)
                direction = 360;

            (double x, double y) PointOfAngle(double a)
            {
                return (x: Math.Cos(a), y: Math.Sin(a));
            };



            var eps = Math.Pow(2, -52);
            var angle = (direction % 360);
            var startPoint = PointOfAngle(DegreesToRadians(180 - angle));
            var endPoint = PointOfAngle(DegreesToRadians(360 - angle));

            if (startPoint.x <= 0 || Math.Abs(startPoint.x) <= eps)
                startPoint.x = 0;

            if (startPoint.y <= 0 || Math.Abs(startPoint.y) <= eps)
                startPoint.y = 0;

            if (endPoint.x <= 0 || Math.Abs(endPoint.x) <= eps)
                endPoint.x = 0;

            if (endPoint.y <= 0 || Math.Abs(endPoint.y) <= eps)
                endPoint.y = 0;

            return (startPoint.x, startPoint.y, endPoint.x, endPoint.y);
        }


        /// <summary>
        /// Dispose with needed delay. 
        /// </summary>
        /// <param name="disposable"></param>
        public virtual void DisposeObject(IDisposable disposable)
        {
            if (disposable != null)
            {
                if (Superview is DrawnView view)
                {
                    view.ToBeDisposed.Enqueue(disposable);
                }
                else
                {
                    Tasks.StartDelayed(TimeSpan.FromSeconds(5), () =>
                    {
                        disposable?.Dispose();
                    });
                }
            }
        }

        private void ViewSizeChanged(object sender, EventArgs e)
        {
            OnSizeChanged();
        }

        protected virtual void OnSizeChanged()
        {
            Update();
        }


        /// <summary>
        /// Optional control identifier
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Optional scene hero control identifier
        /// </summary>
        public string Hero { get; set; }

        public int ContextIndex { get; set; } = -1;

        public bool IsRootView()
        {
            return this.Parent == null;
        }

        /// <summary>
        ///  destination in PIXELS, requests in UNITS
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="widthRequest"></param>
        /// <param name="heightRequest"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public ScaledSize DefineAvailableSize(SKRect destination,
            float widthRequest, float heightRequest, float scale)
        {
            var rectWidth = destination.Width;
            var wants = widthRequest * scale;
            if (wants > 0 && wants < rectWidth)
                rectWidth = (int)wants;

            var rectHeight = destination.Height;
            wants = heightRequest * scale;
            if (wants > 0 && wants < rectHeight)
                rectHeight = (int)wants;

            return ScaledSize.FromPixels(rectWidth, rectHeight, scale);
        }

        /// <summary>
        /// Set this by parent if needed, normally child can detect this itsself. If true will call Arrange when drawing.
        /// </summary>
        public bool IsLayoutDirty
        {
            get => _isLayoutDirty;
            set
            {
                if (value == _isLayoutDirty) return;
                _isLayoutDirty = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///  destination in PIXELS, requests in UNITS. resulting Destination prop will be filed in PIXELS.
        /// Not using Margins nor Padding
        /// Children are responsible to apply Padding to their content and to apply Margin to destination when measuring and drawing
        /// </summary>
        /// <param name="destination">PIXELS</param>
        /// <param name="widthRequest">UNITS</param>
        /// <param name="heightRequest">UNITS</param>
        /// <param name="scale"></param>
        public SKRect CalculateLayout(SKRect destination, float widthRequest, float heightRequest, float scale)
        {
            if (widthRequest == 0 || heightRequest == 0)
            {
                return new SKRect(0, 0, 0, 0);
            }

            var rectAvailable = DefineAvailableSize(destination, widthRequest, heightRequest, scale);

            var useMaxWidth = rectAvailable.Pixels.Width;
            var useMaxHeight = rectAvailable.Pixels.Height;
            var availableWidth = destination.Width;
            var availableHeight = destination.Height;

            var layoutHorizontal = new LayoutOptions(HorizontalOptions.Alignment, HorizontalOptions.Expands);
            var layoutVertical = new LayoutOptions(VerticalOptions.Alignment, HorizontalOptions.Expands);

            // initial fill
            var left = destination.Left;
            var top = destination.Top;
            var right = 0f;
            var bottom = 0f;

            // layoutHorizontal
            switch (layoutHorizontal.Alignment)
            {

                case LayoutAlignment.Center when DrawnExtensions.IsFinite(availableWidth):
                    {
                        left += (float)Math.Round(availableWidth / 2.0f - useMaxWidth / 2.0f);
                        right = left + useMaxWidth;

                        if (left < destination.Left)
                        {
                            left = destination.Left;
                            right = left + useMaxWidth;
                        }

                        if (right > destination.Right)
                        {
                            right = destination.Right;
                        }

                        break;
                    }
                case LayoutAlignment.End when DrawnExtensions.IsFinite(destination.Right):
                    {
                        right = destination.Right;
                        left = right - useMaxWidth;
                        if (left < destination.Left)
                        {
                            left = destination.Left;
                        }

                        break;
                    }
                case LayoutAlignment.Fill:
                case LayoutAlignment.Start:
                default:
                    {
                        right = left + useMaxWidth;
                        if (right > destination.Right)
                        {
                            right = destination.Right;
                        }

                        break;
                    }
            }

            // VerticalOptions
            switch (layoutVertical.Alignment)
            {

                case LayoutAlignment.Center when DrawnExtensions.IsFinite(availableHeight):
                    {
                        top += (float)Math.Round(availableHeight / 2.0f - useMaxHeight / 2.0f);
                        bottom = top + useMaxHeight;

                        if (top < destination.Top)
                        {
                            top = destination.Top;
                            bottom = top + useMaxHeight;
                        }

                        else if (bottom > destination.Bottom)
                        {
                            bottom = destination.Bottom;
                            top = bottom - useMaxHeight;
                        }

                        break;
                    }
                case LayoutAlignment.End when DrawnExtensions.IsFinite(destination.Bottom):
                    {
                        bottom = destination.Bottom;
                        top = bottom - useMaxHeight;
                        if (top < destination.Top)
                        {
                            top = destination.Top;
                        }

                        break;
                    }
                case LayoutAlignment.Start:
                case LayoutAlignment.Fill:
                default:

                bottom = top + useMaxHeight;
                if (bottom > destination.Bottom)
                {
                    bottom = destination.Bottom;
                }
                break;

            }

            var layout = new SKRect(left, top, right, bottom);

            var offsetX = 0f;
            var offsetY = 0f;
            if (DrawnExtensions.IsFinite(availableHeight))
            {
                offsetY = (float)VerticalPositionOffsetRatio * layout.Height;
            }
            if (DrawnExtensions.IsFinite(availableWidth))
            {
                offsetX = (float)HorizontalPositionOffsetRatio * layout.Width;
            }

            layout.Offset(offsetX, offsetY);

            return layout;
        }

        /*
        public SKRect CalculateLayout(SKRect destination, float widthRequest, float heightRequest, float scale)
        {
            if (widthRequest == 0 || heightRequest == 0)
            {
                return new SKRect(0, 0, 0, 0);
            }

            var rectAvailable = DefineAvailableSize(destination, widthRequest, heightRequest, scale);

            var availableWidth = destination.Width;
            var availableHeight = destination.Height;

            var layoutHorizontal = new LayoutOptions(HorizontalOptions.Alignment, HorizontalOptions.Expands);
            var layoutVertical = new LayoutOptions(VerticalOptions.Alignment, HorizontalOptions.Expands);


            //initial fill
            var left = destination.Left;
            var top = destination.Top;
            var right = left + rectAvailable.Pixels.Width;
            var bottom = top + rectAvailable.Pixels.Height;

            //layoutHorizontal
            if (layoutHorizontal.Alignment == LayoutAlignment.Center && DrawnExtensions.IsFinite(availableWidth))
            {
                //center
                left += (availableWidth - rectAvailable.Pixels.Width) / 2.0f;
                right = left + rectAvailable.Pixels.Width;

                if (left < destination.Left)
                {
                    left = (float)(destination.Left);
                    right = left + rectAvailable.Pixels.Width;
                }

                if (right > destination.Right)
                {
                    right = (float)(destination.Right);
                }

            }
            else
            if (layoutHorizontal.Alignment == LayoutAlignment.End)
            {
                //end
                right = destination.Right;
                left = right - rectAvailable.Pixels.Width;
                if (left < destination.Left)
                {
                    left = (float)(destination.Left);
                }
            }
            else
            {
                //start or fill
                right = left + rectAvailable.Pixels.Width;
                if (right > destination.Right)
                {
                    right = (float)(destination.Right);
                }


            }

            //VerticalOptions
            if (layoutVertical.Alignment == LayoutAlignment.Center)
            {
                //center
                top += availableHeight / 2.0f - rectAvailable.Pixels.Height / 2.0f;
                bottom = top + rectAvailable.Pixels.Height;
                if (top < destination.Top)
                {
                    top = (float)(destination.Top);
                    bottom = top + rectAvailable.Pixels.Height;
                }
                else
                if (bottom > destination.Bottom)
                {
                    bottom = (float)(destination.Bottom);
                    top = bottom - rectAvailable.Pixels.Height;
                }
            }
            else
            if (layoutVertical.Alignment == LayoutAlignment.End && DrawnExtensions.IsFinite(destination.Bottom))
            {
                //end
                bottom = destination.Bottom;
                top = bottom - rectAvailable.Pixels.Height;
                if (top < destination.Top)
                {
                    top = (float)(destination.Top);
                }

            }
            else
            {
                //start or fill
                bottom = top + rectAvailable.Pixels.Height;
                if (bottom > destination.Bottom)
                {
                    bottom = (float)(destination.Bottom);
                }

            }

            var ret = new SKRect((float)left, (float)top, (float)right, (float)bottom);

            //Debug.WriteLine($"[Layout] '{Tag}' {ret.Left - destination.Left:0.0}-{destination.Right - ret.Right:0.0} ");

            return ret;
        }
        */


        private ScaledSize _contentSize = new();
        public ScaledSize ContentSize
        {
            get
            {
                return _contentSize;
            }
            protected set
            {
                if (_contentSize != value)
                {
                    _contentSize = value;
                    OnPropertyChanged();
                }
            }
        }


        protected bool WasMeasured;



        protected virtual void OnDrawingSizeChanged()
        {

        }



        protected virtual void AdaptCachedLayout(SKRect destination, float scale)
        {
            //adapt cache to current request
            var newDestination = ArrangedDestination;//.Clone();
            newDestination.Offset(destination.Left, destination.Top);

            Destination = newDestination;
            DrawingRect = GetDrawingRectWithMargins(newDestination, scale);

            IsLayoutDirty = false;
        }

        private SKRect _lastArrangedFor = new();
        private float _lastArrangedWidth;
        private float _lastArrangedHeight;

        public float _lastMeasuredForWidth { get; protected set; }
        public float _lastMeasuredForHeight { get; protected set; }

        /// <summary>
        /// This is the destination in PIXELS with margins applied, using this to paint background
        /// </summary>
        public SKRect DrawingRect
        {
            get => _drawingRect;
            set
            {
                if (value.Equals(_drawingRect)) return;
                _drawingRect = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DebugString));
                OnPropertyChanged(nameof(HitBoxAuto));
            }
        }

        /*
        /// <summary>
        /// Overriding VisualElement property, use DrawingRect instead.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Rect Bounds
        {
            get
            {
                return DrawingRect.ToMauiRectangle();
            }
            private set
            {
                throw new NotImplementedException("Use DrawingRect instead.");
            }
        }
        */

        /// <summary>
        /// ISkiaGestureListener impl
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool HitIsInside(float x, float y)
        {
            var hitbox = HitBoxAuto;

            //if (UseCache != SkiaCacheType.None && RenderObject != null)
            //{
            //    var offsetCacheX = Math.Abs(DrawingRect.Left - RenderObject.Bounds.Left);
            //    var offsetCacheY = Math.Abs(DrawingRect.Top - RenderObject.Bounds.Top);

            //    hitbox.Offset(offsetCacheX,offsetCacheY);
            //}

            return hitbox.ContainsInclusive(x, y); ;
        }

        /// <summary>
        /// This can be absolutely false if we are inside a cached 
        /// rendering object parent that already moved somewhere.
        /// So coords will be of the moment we were first drawn,
        /// while if cached parent moved, our coords might differ.
        /// todo detect if parent is cached somewhere and offset hotbox by cached parent movement offset...
        /// todo think about it baby =) meanwhile just do not set gestures below cached level
        /// </summary>
        public virtual SKRect HitBoxAuto
        {
            get
            {
                var moved = ApplyTransforms(DrawingRect);

                return moved;
            }
        }

        public virtual bool IsGestureForChild(ISkiaGestureListener listener, SKPoint point)
        {
            return IsGestureForChild(listener, point.X, point.Y);
        }

        public virtual bool IsGestureForChild(ISkiaGestureListener listener, float x, float y)
        {
            var hit = listener.HitIsInside(x, y);
            return hit;
        }

        public SKRect ApplyTransforms(SKRect rect)
        {
            //Debug.WriteLine($"[Transforming] {rect}");

            return new SKRect(rect.Left + (float)(UseTranslationX * RenderingScale),
                rect.Top + (float)(UseTranslationY * RenderingScale),
                    rect.Right + (float)(UseTranslationX * RenderingScale),
                rect.Bottom + (float)(UseTranslationY * RenderingScale));
        }

        public virtual SKPoint TranslateInputDirectOffsetToPoints(PointF location, SKPoint childOffsetDirect)
        {
            var thisOffset1 = TranslateInputCoords(childOffsetDirect, false);
            //apply touch coords
            var x1 = location.X + thisOffset1.X;
            var y1 = location.Y + thisOffset1.Y;
            //convert to points
            return new SKPoint(x1 / RenderingScale, y1 / RenderingScale);
        }

        public virtual SKPoint TranslateInputOffsetToPixels(PointF location, SKPoint childOffset)
        {
            var thisOffset = TranslateInputCoords(childOffset);
            return new SKPoint(location.X + thisOffset.X, location.Y + thisOffset.Y);
        }

        /// <summary>
        /// Use this to consume gestures in your control only,
        /// do not use result for passing gestures below
        /// </summary>
        /// <param name="childOffset"></param>
        /// <returns></returns>
        public virtual SKPoint TranslateInputCoords(SKPoint childOffset, bool accountForCache = true)
        {
            var thisOffset = new SKPoint(-(float)(UseTranslationX * RenderingScale), -(float)(UseTranslationY * RenderingScale));

            //inside a cached object coordinates are frozen at the moment the snapshot was taken
            //so we must offset the coordinates to match the current drawing rect
            if (accountForCache && UsingCacheType != SkiaCacheType.None)
            {
                if (RenderObject != null)
                {
                    thisOffset.Offset(RenderObject.TranslateInputCoords(DrawingRect));
                }
                else
                if (UsingCacheType == SkiaCacheType.ImageDoubleBuffered && RenderObjectPrevious != null)
                {
                    thisOffset.Offset(RenderObjectPrevious.TranslateInputCoords(DrawingRect));
                }
            }

            thisOffset.Offset(childOffset);

            return thisOffset;
        }

        public virtual SKPoint CalculatePositionOffset(bool cacheOnly = false,
            bool ignoreCache = false,
            bool useTranlsation = true)
        {
            var thisOffset = SKPoint.Empty;
            if (!cacheOnly && useTranlsation)
            {
                thisOffset = new SKPoint((float)(UseTranslationX * RenderingScale), (float)(UseTranslationY * RenderingScale));
            }
            //inside a cached object coordinates are frozen at the moment the snapshot was taken
            //so we must offset the coordinates to match the current drawing rect
            if (!ignoreCache && UsingCacheType != SkiaCacheType.None)
            {
                if (RenderObject != null)
                {
                    thisOffset.Offset(RenderObject.CalculatePositionOffset(LastDrawnAt));
                }
                else
                if (UsingCacheType == SkiaCacheType.ImageDoubleBuffered && RenderObjectPrevious != null)
                {
                    thisOffset.Offset(RenderObjectPrevious.CalculatePositionOffset(LastDrawnAt));
                }
                //Debug.WriteLine($"[CalculatePositionOffset] was cached!");
            }

            //Debug.WriteLine($"[CalculatePositionOffset] {this} {cacheOnly} returned {thisOffset}");

            return thisOffset;
        }

        long _layoutChanged = 0;

        public SKRect ArrangedDestination { get; protected set; }

        protected virtual void OnLayoutChanged()
        {
            LayoutReady = this.Height > 0 && this.Width > 0;
        }

        public Action<SkiaControl> WhenLayoutIsReady;
        public Action<SkiaControl> WhenDisposing;

        /// <summary>
        /// Layout was changed with dimensions above zero. Rather a helper method, can you more generic OnLayoutChanged().
        /// </summary>
        protected virtual void OnLayoutReady()
        {

        }

        public bool LayoutReady
        {
            get
            {
                return _layoutReady;
            }

            protected set
            {
                if (_layoutReady != value)
                {
                    _layoutReady = value;
                    OnPropertyChanged();
                    if (value)
                    {
                        WhenLayoutIsReady?.Invoke(this);
                        OnLayoutReady();
                    }
                }
            }
        }
        bool _layoutReady;

        public bool CheckIsGhost()
        {
            return IsGhost || Destination == SKRect.Empty;
        }

        /// <summary>
        ///  destination in PIXELS, requests in UNITS. resulting Destination prop will be filed in PIXELS.
        /// DrawUsingRenderObject wil call this among others..
        /// </summary>
        /// <param name="destination">PIXELS</param>
        /// <param name="widthRequest">UNITS</param>
        /// <param name="heightRequest">UNITS</param>
        /// <param name="scale"></param>
        public virtual void Arrange(SKRect destination, float widthRequest, float heightRequest, float scale)
        {
            if (!PreArrange(destination, widthRequest, heightRequest, scale))
            {
                DrawingRect = SKRect.Empty;
                return;
            }

            PostArrange(destination, MeasuredSize.Units.Width, MeasuredSize.Units.Height, scale);
        }

        protected virtual void PostArrange(SKRect destination, float widthRequest, float heightRequest, float scale)
        {
            SKRect arrangingFor = new(0, 0, destination.Width, destination.Height);

            if (!IsLayoutDirty &&
                (ViewportHeightLimit != _arrangedViewportHeightLimit ||
                 ViewportWidthLimit != _arrangedViewportWidthLimit ||
                 scale != _lastArrangedForScale ||
                 !CompareRects(arrangingFor, _lastArrangedFor, 1) ||
                 !AreEqual(_lastArrangedHeight, heightRequest, 1) ||
                 !AreEqual(_lastArrangedWidth, widthRequest, 1)))
            {
                IsLayoutDirty = true;
            }

            if (!IsLayoutDirty)
            {
                AdaptCachedLayout(destination, scale);
                return;
            }

            //var oldDestination = Destination;
            var layout = CalculateLayout(arrangingFor, widthRequest, heightRequest, scale);

            var oldDrawingRect = this.DrawingRect;

            //save to cache
            ArrangedDestination = layout;
            //ArrangedDrawingRect = GetDrawingRectWithMargins(layout, scale);

            AdaptCachedLayout(destination, scale);

            _arrangedViewportHeightLimit = ViewportHeightLimit;
            _arrangedViewportWidthLimit = ViewportWidthLimit;
            _lastArrangedFor = arrangingFor;
            _lastArrangedForScale = scale;
            _lastArrangedHeight = heightRequest;
            _lastArrangedWidth = widthRequest;

            OnLayoutChanged();

            if (!AreEqual(oldDrawingRect.Height, DrawingRect.Height, 1)
                || !AreEqual(oldDrawingRect.Width, DrawingRect.Width, 1))
            {
                OnDrawingSizeChanged();
            }

            //if (!CompareRects(oldDestination, Destination)) //layout can be same but offset might have been changed
            //{
            //    OnLayoutChanged();
            //}

            IsLayoutDirty = false;
        }

        /// <summary>
        /// PIXELS
        /// </summary>
        /// <param name="child"></param>
        /// <param name="availableWidth"></param>
        /// <param name="availableHeight"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public ScaledSize MeasureChild(SkiaControl child, double availableWidth, double availableHeight, float scale)
        {
            if (child == null)
            {
                return ScaledSize.Default;
            }

            child.OnBeforeMeasure(); //could set IsVisible or whatever inside

            if (!child.CanDraw)
                return ScaledSize.Default; //child set himself invisible

            return child.Measure((float)availableWidth, (float)availableHeight, scale);
        }


        protected virtual ScaledSize MeasureContent(
            IEnumerable<SkiaControl> children,
            SKRect rectForChildrenPixels,
            float scale)
        {
            var maxHeight = -1.0f;
            var maxWidth = -1.0f;

            List<SkiaControl> fill = new();
            var autosize = this.NeedAutoSize;
            var hadFixedSize = false;

            void PostProcessMeasuredChild(ScaledSize measured, SkiaControl child, bool ignoreFill)
            {
                if (measured != ScaledSize.Default)
                {
                    var measuredHeight = measured.Pixels.Height;
                    var measuredWidth = measured.Pixels.Width;

                    if (child.ViewportHeightLimit >= 0)
                    {
                        float mHeight = (float)(child.ViewportHeightLimit * scale);
                        if (measuredHeight > mHeight)
                        {
                            float excessHeight = measuredHeight - mHeight;
                            measuredHeight -= excessHeight;
                        }
                    }

                    if (child.ViewportWidthLimit >= 0)
                    {
                        float mWidth = (float)(child.ViewportWidthLimit * scale);
                        if (measuredWidth > mWidth)
                        {
                            float excessWidth = measuredWidth - mWidth;
                            measuredWidth -= excessWidth;
                        }
                    }

                    if (ignoreFill)
                    {
                        if (measuredWidth > maxWidth && (child.HorizontalOptions.Alignment != LayoutAlignment.Fill || child.WidthRequest >= 0))
                            maxWidth = measuredWidth;

                        if (measuredHeight > maxHeight && (child.VerticalOptions.Alignment != LayoutAlignment.Fill || child.HeightRequest >= 0))
                            maxHeight = measuredHeight;
                    }
                    else
                    {
                        if (measuredWidth > maxWidth)
                            maxWidth = measuredWidth;

                        if (measuredHeight > maxHeight)
                            maxHeight = measuredHeight;
                    }

                }
            }

            //PASS 1
            foreach (var child in children)
            {
                child.OnBeforeMeasure(); //could set IsVisible or whatever inside

                if (autosize &&
                    (child.HorizontalOptions.Alignment == LayoutAlignment.Fill
                     && child.VerticalOptions.Alignment == LayoutAlignment.Fill)
                    || (!autosize && (child.HorizontalOptions.Alignment == LayoutAlignment.Fill || child.VerticalOptions.Alignment == LayoutAlignment.Fill))
                    )
                {
                    fill.Add(child); //todo not very correct for the case just 1 dimension is Fill and other one may by bigger that other children!
                    continue;
                }

                hadFixedSize = true;
                var measured = MeasureChild(child, rectForChildrenPixels.Width, rectForChildrenPixels.Height, scale);
                PostProcessMeasuredChild(measured, child, true);
            }

            //PASS 2 for thoses with Fill 
            foreach (var child in fill)
            {
                if (!hadFixedSize) //we had only children with fill so no fixed dimensions yet
                {
                    var measured = MeasureChild(child, rectForChildrenPixels.Width, rectForChildrenPixels.Height, scale);
                    PostProcessMeasuredChild(measured, child, false);
                }
                else
                {
                    var provideWidth = rectForChildrenPixels.Width;
                    if (NeedAutoWidth && maxWidth >= 0)
                    {
                        provideWidth = maxWidth;
                    }
                    var provideHeight = rectForChildrenPixels.Height;
                    if (NeedAutoHeight && maxHeight >= 0)
                    {
                        provideHeight = maxHeight;
                    }
                    var measured = MeasureChild(child, provideWidth, provideHeight, scale);
                    if (maxHeight == 0 || maxWidth == 0)
                    {
                        PostProcessMeasuredChild(measured, child, false);
                    }
                }
            }

            if (HorizontalOptions.Alignment == LayoutAlignment.Fill)
            {
                maxWidth = rectForChildrenPixels.Width;
            }
            if (VerticalOptions.Alignment == LayoutAlignment.Fill)
            {
                maxHeight = rectForChildrenPixels.Height;
            }

            return ScaledSize.FromPixels(maxWidth, maxHeight, scale);
        }

        public virtual void ApplyBindingContext()
        {

            foreach (var content in this.Views)
            {
                content.BindingContext = BindingContext;
            }

            foreach (var content in this.VisualEffects.ToList())
            {
                content.Attach(this);
            }

            if (FillGradient != null)
                FillGradient.BindingContext = BindingContext;

        }

        protected bool BindingContextWasSet { get; set; }
        /// <summary>
        /// First Maui will apply bindings to your controls, then it would call OnBindingContextChanged, so beware on not to break bindings.
        /// </summary>
        protected override void OnBindingContextChanged()
        {
            BindingContextWasSet = true;

            try
            {
                InvalidateViewsList(); //we might get different ZIndex which is bindable..

                ApplyBindingContext();

                //will apply to maui prps like styles, triggers etc
                base.OnBindingContextChanged();
            }
            catch (Exception e)
            {
                Super.Log(e);
            }
        }

        public struct ParentMeasureRequest
        {
            public IDrawnBase Parent { get; set; }
            public float WidthRequest { get; set; }
            public float HeightRequest { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="widthConstraint"></param>
        /// <param name="heightConstraint"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public virtual ScaledSize Measure(float widthConstraint, float heightConstraint, float scale)
        {
            if (IsMeasuring) //basically we need this for cache double buffering to avoid conflicts with background thread
            {

                return MeasuredSize;
            }

            try
            {
                IsMeasuring = true;

                RenderingScale = scale;

                var request = CreateMeasureRequest(widthConstraint, heightConstraint, scale);
                //if (request.IsSame)
                //{
                //    return MeasuredSize;
                //}

                if (!this.CanDraw || request.WidthRequest == 0 || request.HeightRequest == 0)
                {
                    InvalidateCacheWithPrevious();

                    return SetMeasuredAsEmpty(request.Scale);
                }

                var constraints = GetMeasuringConstraints(request);

                ContentSize = MeasureAbsolute(constraints.Content, scale);

                return SetMeasuredAdaptToContentSize(constraints, scale);
            }
            finally
            {
                IsMeasuring = false;
            }

        }

        protected virtual ScaledSize SetMeasuredAsEmpty(float scale)
        {
            return SetMeasured(0, 0, scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static float SmartMax(float a, float b)
        {
            if (float.IsInfinity(a) || !float.IsInfinity(b) && b > a)
                return b;
            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static float SmartMin(float a, float b)
        {
            if (float.IsInfinity(a) || !float.IsInfinity(a) && b < a)
                return b;
            return a;
        }

        public virtual ScaledSize SetMeasuredAdaptToContentSize(MeasuringConstraints constraints,
            float scale)
        {
            var contentWidth = NeedAutoWidth ? ContentSize.Pixels.Width : SmartMax(ContentSize.Pixels.Width, constraints.Request.Width);
            var contentHeight = NeedAutoHeight ? ContentSize.Pixels.Height : SmartMax(ContentSize.Pixels.Height, constraints.Request.Height);

            var width = AdaptWidthConstraintToContentRequest(constraints, contentWidth, HorizontalOptions.Expands);
            var height = AdaptHeightConstraintToContentRequest(constraints, contentHeight, VerticalOptions.Expands);

            var widthCut = ContentSize.Pixels.Width > width;// || ContentSize.WidthCut;
            var heighCut = ContentSize.Pixels.Height > height;// || ContentSize.HeightCut;

            SKSize size = new(width, height);

            var invalid = !CompareSize(size, MeasuredSize.Pixels, 0);
            if (invalid)
            {
                InvalidateCacheWithPrevious();
            }

            return SetMeasured(size.Width, size.Height, scale);
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SKSize GetSizeInPoints(SKSize size, float scale)
        {
            var width = float.PositiveInfinity;
            var height = float.PositiveInfinity;
            if (!double.IsInfinity(size.Width) && size.Width >= 0)
            {
                width = size.Width / scale;
            }
            if (!double.IsInfinity(size.Height) && size.Height >= 0)
            {
                height = size.Height / scale;
            }
            return new SKSize(width, height);
        }

        protected virtual MeasureRequest CreateMeasureRequest(float widthConstraint, float heightConstraint, float scale)
        {

            RenderingScale = scale;

            //LastMeasureRequest = new()
            //{
            //    Parent = this.Parent,
            //    WidthRequest = widthConstraint,
            //    HeightRequest = heightConstraint,
            //};


            if (HorizontalFillRatio != 1 && !double.IsInfinity(widthConstraint) && widthConstraint > 0)
            {
                widthConstraint *= (float)HorizontalFillRatio;
            }
            if (VerticalFillRatio != 1 && !double.IsInfinity(heightConstraint) && heightConstraint > 0)
            {
                heightConstraint *= (float)VerticalFillRatio;
            }

            if (LockRatio < 0)
            {
                var size = Math.Min(heightConstraint, widthConstraint);
                size *= (float)-LockRatio;
                heightConstraint = size;
                widthConstraint = size;
            }
            else
            if (LockRatio > 0)
            {
                var size = Math.Max(heightConstraint, widthConstraint);
                size *= (float)LockRatio;
                heightConstraint = size;
                widthConstraint = size;
            }

            var isSame =
                !NeedMeasure
                && _lastMeasuredForScale == scale
                && AreEqual(_lastMeasuredForHeight, heightConstraint, 1)
                && AreEqual(_lastMeasuredForWidth, widthConstraint, 1);

            if (!isSame)
            {
                _lastMeasuredForWidth = widthConstraint;
                _lastMeasuredForHeight = heightConstraint;
                _lastMeasuredForScale = scale;
            }

            return new MeasureRequest(widthConstraint, heightConstraint, scale)
            {
                IsSame = isSame
            };
        }

        public SKRect GetMeasuringRectForChildren(float widthConstraint, float heightConstraint, double scale)
        {
            var constraintLeft = (Padding.Left + Margins.Left) * scale;
            var constraintRight = (Padding.Right + Margins.Right) * scale;
            var constraintTop = (Padding.Top + Margins.Top) * scale;
            var constraintBottom = (Padding.Bottom + Margins.Bottom) * scale;

            //SKRect rectForChild = new SKRect(0 + (float)constraintLeft,
            //    0 + (float)constraintTop,
            //    widthConstraint - (float)constraintRight,
            //    heightConstraint - (float)constraintBottom);

            SKRect rectForChild = new SKRect(0, 0,
                (float)Math.Round(widthConstraint - (float)(constraintRight + constraintLeft)),
                (float)Math.Round(heightConstraint - (float)(constraintBottom + constraintTop)));


            return rectForChild;
        }

        public virtual ScaledSize MeasureAbsolute(SKRect rectForChildrenPixels, float scale)
        {
            return MeasureAbsoluteBase(rectForChildrenPixels, scale);
        }

        /// <summary>
        /// Base method, not aware of any views provider, not virtual, silly measuring Children.
        /// </summary>
        /// <param name="rectForChildrenPixels"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        protected ScaledSize MeasureAbsoluteBase(SKRect rectForChildrenPixels, float scale)
        {
            if (Views.Count > 0)
            {
                var maxHeight = 0.0f;
                var maxWidth = 0.0f;

                var children = Views;//GetOrderedSubviews();
                return MeasureContent(children, rectForChildrenPixels, scale);
            }
            //empty container
            else
            if (LockRatio == 0 && (NeedAutoHeight || NeedAutoWidth))
            {
                return ScaledSize.CreateEmpty(scale);
                //return SetMeasured(0, 0, scale);
            }

            return ScaledSize.FromPixels(rectForChildrenPixels.Width, rectForChildrenPixels.Height, scale);
        }


        public static SKRect ContractPixelsRect(SKRect rect, float scale, Thickness amount)
        {
            return new SKRect(
                rect.Left + (float)Math.Round((float)amount.Left * scale),
                rect.Top + (float)Math.Round((float)amount.Top * scale),
                rect.Right - (float)Math.Round((float)amount.Right * scale),
                rect.Bottom - (float)Math.Round((float)amount.Bottom * scale)
            );
        }


        public SKRect GetDrawingRectForChildren(SKRect destination, double scale)
        {
            //var constraintLeft = (Padding.Left + Margins.Left) * scale;
            //var constraintRight = (Padding.Right + Margins.Right) * scale;
            //var constraintTop = (Padding.Top + Margins.Top) * scale;
            //var constraintBottom = (Padding.Bottom + Margins.Bottom) * scale;

            var constraintLeft = (Padding.Left + Margins.Left) * scale;
            var constraintRight = (Padding.Right + Margins.Right) * scale;
            var constraintTop = (Padding.Top + Margins.Top) * scale;
            var constraintBottom = (Padding.Bottom + Margins.Bottom) * scale;


            SKRect rectForChild = new SKRect(
                (float)Math.Round(destination.Left + (float)constraintLeft),
                    (float)Math.Round(destination.Top + (float)constraintTop),
                        (float)Math.Round(destination.Right - (float)constraintRight),
                            (float)Math.Round(destination.Bottom - (float)constraintBottom)
                );

            return rectForChild;
        }

        public virtual SKRect GetDrawingRectWithMargins(SKRect destination, double scale)
        {


            var constraintLeft = (float)Math.Round(Margins.Left * scale);
            var constraintRight = (float)Math.Round(Margins.Right * scale);
            var constraintTop = (float)Math.Round(Margins.Top * scale);
            var constraintBottom = (float)Math.Round(Margins.Bottom * scale);

            SKRect rectForChild;

            rectForChild = new SKRect(
                (destination.Left + constraintLeft),
                (destination.Top + constraintTop),
                (destination.Right - constraintRight),
                (destination.Bottom - constraintBottom)
            );

            // Apply ViewportHeightLimit if it's set
            if (ViewportHeightLimit >= 0)
            {
                float maxHeight = (float)Math.Round(ViewportHeightLimit * scale);
                if (rectForChild.Height > maxHeight)
                {
                    float excessHeight = rectForChild.Height - maxHeight;
                    rectForChild.Bottom -= excessHeight;
                }
            }

            // Apply ViewportWidthLimit if it's set
            if (ViewportWidthLimit >= 0)
            {
                float maxWidth = (float)Math.Round(ViewportWidthLimit * scale);
                if (rectForChild.Width > maxWidth)
                {
                    float excessWidth = rectForChild.Width - maxWidth;
                    rectForChild.Right -= excessWidth;
                }
            }

            return rectForChild;
        }





        protected object lockMeasured = new();

        bool debugMe;

        /// <summary>
        /// Flag for internal use, maynly used to avoid conflicts between measuring on ui-thread and in background. If true, measure will return last measured value.
        /// </summary>
        public bool IsMeasuring { get; protected internal set; }

        /// <summary>
        /// Parameters in PIXELS. sets IsLayoutDirty = true;
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        protected virtual ScaledSize SetMeasured(float width, float height, float scale)
        {
            lock (lockMeasured)
            {
                WasMeasured = true;

                NeedMeasure = false;

                IsLayoutDirty = true;

                if (!double.IsInfinity(height) && !double.IsNaN(height))
                {
                    Height = (height / scale) - (Margins.Top + Margins.Bottom);
                }
                else
                {
                    height = -1;
                    Height = height;
                }

                if (!double.IsInfinity(width) && !double.IsNaN(width))
                {
                    Width = (width / scale) - (Margins.Left + Margins.Right);
                }
                else
                {
                    width = -1;
                    Width = width;
                }

                MeasuredSize = ScaledSize.FromPixels(width, height, scale);

                //SetValueCore(RenderingScaleProperty, scale, SetValueFlags.None);

                OnMeasured();

                return MeasuredSize;
            }
        }

        protected virtual void OnMeasured()
        {
            //Debug.WriteLine($"[MEASURED] {this.GetType().Name} {this.Tag} ");
            Measured?.Invoke(this, MeasuredSize);
        }

        /// <summary>
        /// UNITS
        /// </summary>
        public event EventHandler<ScaledSize> Measured;


        public ScaledSize MeasuredSize { get; set; } = new();


        public bool NeedAutoSize
        {
            get
            {
                return NeedAutoHeight || NeedAutoWidth;
            }
        }

        public bool NeedAutoHeight
        {
            get
            {
                return LockRatio == 0 && VerticalOptions.Alignment != LayoutAlignment.Fill && SizeRequest.Height < 0;
            }
        }
        public bool NeedAutoWidth
        {
            get
            {
                return LockRatio == 0 && HorizontalOptions.Alignment != LayoutAlignment.Fill && SizeRequest.Width < 0;
            }
        }


        private bool _isDisposed;

        public bool IsDisposed
        {
            get
            {
                return _isDisposed;
            }
            protected set
            {
                if (value != _isDisposed)
                {
                    _isDisposed = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isDisposing;

        public bool IsDisposing
        {
            get
            {
                return _isDisposing;
            }
            protected set
            {
                if (value != _isDisposing)
                {
                    _isDisposing = value;
                    OnPropertyChanged();
                    if (value)
                        OnWillBeDisposed();
                }
            }
        }

        /// <summary>
        /// The OnDisposing might come with a delay to avoid disposing resources at use.
        /// This method will be called without delay when IsDisposed is set to True.
        /// </summary>
        protected virtual void OnWillBeDisposed()
        {

        }


        public TChild FindView<TChild>(string tag) where TChild : SkiaControl
        {
            if (this.Tag == tag && this is TChild)
                return this as TChild;

            var found = Views.FirstOrDefault(x => x.Tag == tag) as TChild;
            if (found == null)
            {
                //go sub level
                foreach (var view in Views)
                {
                    found = view.FindView<TChild>(tag);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return found;
        }

        public TChild FindView<TChild>() where TChild : SkiaControl
        {

            var found = Views.FirstOrDefault(x => x is TChild) as TChild;
            if (found == null)
            {
                //go sub level
                foreach (var view in Views)
                {
                    found = view.FindView<TChild>();
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return found;
        }

        public SkiaControl FindViewByTag(string tag)
        {
            if (this.Tag == tag)
                return this;

            var found = Views.FirstOrDefault(x => x.Tag == tag);
            if (found == null)
            {
                //go sub level
                foreach (var view in Views)
                {
                    found = view.FindViewByTag(tag);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return found;
        }

        /// <summary>
        /// Avoid setting parent to null before calling this, or set SuperView prop manually for proper cleanup of animations and gestures if any used
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;

            SetWillDisposeWithChildren();

            IsDisposed = true;

            //for the double buffering case it's safer to delay
            Tasks.StartDelayed(TimeSpan.FromSeconds(3), () =>
            {
                RenderObject = null;

                PaintSystem?.Dispose();

                _lastAnimatorManager = null;

                DisposeChildren();

                OnDisposing();

                Parent = null;

                Superview = null;

                CustomizeLayerPaint = null;

                RenderObjectPrevious?.Dispose();
                RenderObjectPrevious = null;


                RenderObjectPreparing?.Dispose();
                RenderObjectPreparing = null;


                _paintWithOpacity?.Dispose();
                _paintWithEffects?.Dispose();

                _preparedClipBounds?.Dispose();
            });
        }



        public static long GetNanoseconds()
        {
            double timestamp = Stopwatch.GetTimestamp();
            double nanoseconds = 1_000_000_000.0 * timestamp / Stopwatch.Frequency;
            return (long)nanoseconds;

            //double nano = 10000L * Stopwatch.GetTimestamp();
            //nano /= TimeSpan.TicksPerMillisecond;
            //nano *= 100L;
            //return (long)nano;
        }




        public virtual void OnBeforeMeasure()
        {

        }

        public virtual void OnBeforeDraw()
        {
            Superview?.UpdateRenderingChains(this);
        }



        /// <summary>
        /// do not ever erase background
        /// </summary>
        public bool IsOverlay { get; set; }


        /// <summary>
        /// Executed after the rendering
        /// </summary>
        public List<IOverlayEffect> PostAnimators { get; } = new(); //to be renamed to post-effects


        public static readonly BindableProperty UpdateWhenReturnedFromBackgroundProperty = BindableProperty.Create(nameof(UpdateWhenReturnedFromBackground),
        typeof(bool),
        typeof(SkiaControl),
        false);
        public bool UpdateWhenReturnedFromBackground
        {
            get { return (bool)GetValue(UpdateWhenReturnedFromBackgroundProperty); }
            set { SetValue(UpdateWhenReturnedFromBackgroundProperty, value); }
        }

        public virtual void OnSuperviewShouldRenderChanged(bool state)
        {
            if (UpdateWhenReturnedFromBackground)
            {
                Update();
            }
            foreach (var view in Views) //will crash? why adapter nor used??
            {
                view.OnSuperviewShouldRenderChanged(state);
            }
        }

        /// <summary>
        /// Normally get a a Measure by parent then parent calls Draw and we can apply the measure result.
        /// But in a case we have measured us ourselves inside PreArrange etc we must call ApplyMeasureResult because this would happen after the Draw and not before.
        /// </summary>
        public virtual void ApplyMeasureResult()
        {
        }

        /// <summary>
        /// Returns false if should not render
        /// </summary>
        /// <returns></returns>
        public virtual bool PreArrange(SKRect destination, float widthRequest, float heightRequest, float scale)
        {
            if (!CanDraw)
                return false;

            if (WillInvalidateMeasure)
            {
                WillInvalidateMeasure = false;
                InvalidateMeasureInternal();
            }

            if (NeedMeasure)
            {
                var adjustedDestination = CalculateLayout(destination, widthRequest, heightRequest, scale);
                ArrangedDestination = adjustedDestination;

                Measure(adjustedDestination.Width, adjustedDestination.Height, scale);
                ApplyMeasureResult();
            }
            else
            {
                _lastArrangedInside = destination;
            }

            return true;
        }


        protected bool IsRendering { get; set; }

        public virtual void Render(SkiaDrawingContext context,
            SKRect destination,
            float scale)
        {
            if (IsDisposing || IsDisposed)
                return;

            IsRendering = true;

            Superview = context.Superview;
            RenderingScale = scale;
            NeedUpdate = false;

            OnBeforeDrawing(context, destination, scale);

            if (WillInvalidateMeasure)
            {
                WillInvalidateMeasure = false;
                InvalidateMeasureInternal();
            }

            if (RenderObjectNeedsUpdate)
            {
                //disposal etc inside setter
                RenderObject = null;
            }

            if (
                //RenderedAtDestination.Width != destination.Width ||
                //RenderedAtDestination.Height != destination.Height ||
                !CompareRectsSize(RenderedAtDestination, destination, 0.5f) ||
                RenderingScale != scale)
            {
                //NeedMeasure = true;
                RenderedAtDestination = destination;
            }

            if (RenderedAtDestination != SKRect.Empty)
            {
                //moved to superview
                //ExecuteAnimators(context.FrameTimeNanos);// Super.GetNanoseconds()

                Draw(context, destination, scale);
            }

            //UpdateLocked = false;

            OnAfterDrawing(context, destination, scale);

            Rendered?.Invoke(this, null);

            IsRendering = false;
        }

        public event EventHandler Rendered;

        protected virtual void OnBeforeDrawing(SkiaDrawingContext context,
            SKRect destination,
            float scale)
        {
            InvalidatedParent = false;
            _invalidatedParentPostponed = false;

            if (EffectsState != null)
            {
                foreach (var stateEffect in EffectsState)
                {
                    stateEffect?.UpdateState();
                }
            }
        }

        protected virtual void OnAfterDrawing(SkiaDrawingContext context,
            SKRect destination,
            float scale)
        {
            if (_invalidatedParentPostponed)
            {
                InvalidatedParent = false;
                InvalidateParent();
            }

            if (UsingCacheType == SkiaCacheType.None)
                NeedUpdate = false; //otherwise CreateRenderingObject will set this to false

            //trying to find exact location on the canvas

            LastDrawnAt = DrawingRect;

            X = LastDrawnAt.Location.X / scale;
            Y = LastDrawnAt.Location.Y / scale;

            ExecutePostAnimators(context, scale);

            if (NeedRemeasuring)
            {
                NeedRemeasuring = false;
            }

            if (UsingCacheType == SkiaCacheType.ImageDoubleBuffered
                && RenderObject != null)
            {
                if (!CompareRectsSize(DrawingRect, RenderObject.Bounds, 0.5f))
                {
                    InvalidateMeasure();
                }
            }
            else
            if (UsingCacheType == SkiaCacheType.ImageComposite
                && RenderObjectPrevious != null)
            {
                if (!CompareRectsSize(DrawingRect, RenderObjectPrevious.Bounds, 0.5f))
                {
                    InvalidateMeasure();
                }
            }

        }



        protected virtual void Draw(SkiaDrawingContext context, SKRect destination, float scale)
        {
            if (IsDisposing || IsDisposed)
                return;

            DrawUsingRenderObject(context,
                SizeRequest.Width, SizeRequest.Height,
                destination, scale);
        }

        /// <summary>
        /// Lock between replacing and using RenderObject
        /// </summary>
        protected object LockDraw = new();

        /// <summary>
        /// Creating new cache lock
        /// </summary>
        protected object LockRenderObject = new();

        public virtual object CreatePaintArguments()
        {
            return null;
        }







        private double _X;
        /// <summary>
        /// Absolute position obtained after this control was drawn on the Canvas, this is not relative to parent control.
        /// </summary>
        public new double X
        {
            get
            {
                return _X;
            }
            protected set
            {
                if (_X != value)
                {
                    _X = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _Y;
        /// <summary>
        /// Absolute position obtained after this control was drawn on the Canvas, this is not relative to parent control.
        /// </summary>
        public new double Y
        {
            get
            {
                return _Y;
            }
            protected set
            {
                if (_Y != value)
                {
                    _Y = value;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Execute post drawing operations, like post-animators etc
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scale"></param>
        protected void FinalizeDraw(SkiaDrawingContext context, double scale)
        {
            if (UsingCacheType == SkiaCacheType.None)
                NeedUpdate = false; //otherwise CreateRenderingObject will set this to false

            //trying to find exact location on the canvas

            var drawnAt = this.DrawingRect.Location;

            LastDrawnAt = drawnAt;

            X = LastDrawnAt.X / scale;
            Y = LastDrawnAt.Y / scale;

            ExecutePostAnimators(context, scale);
        }

        public SKPoint GetPositionOffsetInPoints()
        {
            var thisOffset = CalculatePositionOffset();
            var x = (thisOffset.X) / RenderingScale;
            var y = (thisOffset.Y) / RenderingScale;
            return new((float)x, (float)y);
        }

        public SKPoint GetPositionOffsetInPixels(bool cacheOnly = false, bool ignoreCache = false, bool useTranslation = true)
        {
            var thisOffset = CalculatePositionOffset(cacheOnly, ignoreCache, useTranslation);
            var x = (thisOffset.X);
            var y = (thisOffset.Y);
            return new((float)x, (float)y);
        }

        public SKPoint GetOffsetInsideControlInPoints(PointF location, SKPoint childOffset)
        {
            var thisOffset = TranslateInputCoords(childOffset, false);
            var x = (location.X + thisOffset.X) / RenderingScale;
            var y = (location.Y + thisOffset.Y) / RenderingScale;
            var insideX = x - X;
            var insideY = y - Y;
            return new((float)insideX, (float)insideY);
        }

        public SKPoint GetOffsetInsideControlInPixels(PointF location, SKPoint childOffset)
        {
            var thisOffset = TranslateInputCoords(childOffset, false);
            var x = location.X + thisOffset.X;
            var y = location.Y + thisOffset.Y;
            var insideX = x - X * RenderingScale;
            var insideY = y - Y * RenderingScale;
            return new((float)insideX, (float)insideY);
        }

        /// <summary>
        /// Location on the canvas after last drawing completed
        /// </summary>
        public SKRect LastDrawnAt { get; protected set; }

        public void ExecutePostAnimators(SkiaDrawingContext context, double scale)
        {
            try
            {
                if (PostAnimators.Count == 0)
                {
                    //Debug.WriteLine($"[ExecutePostAnimators] {Tag} NO effects!");
                    return;
                }

                //Debug.WriteLine($"[ExecutePostAnimators] {Tag} {PostAnimators.Count} effects");

                foreach (var effect in PostAnimators.ToList())
                {
                    //if (effect.IsRunning)
                    {
                        if (effect.Render(this, context, scale))
                        {
                            Repaint();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Super.Log(e);
            }

        }



        /// <summary>
        /// Base method will call RenderViewsList.
        /// Return number of drawn views.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="destination"></param>
        /// <param name="scale"></param>
        /// <param name="debug"></param>
        /// <returns></returns>
        protected virtual int DrawViews(SkiaDrawingContext context, SKRect destination, float scale, bool debug = false)
        {
            var children = GetOrderedSubviews();

            return RenderViewsList((IList<SkiaControl>)children, context, destination, scale, debug);
        }



        /// <summary>
        /// Just make us repaint to apply new transforms etc
        /// </summary>
        public virtual void Repaint()
        {
            if (NeedUpdate ||
                Superview == null
                || IsParentIndependent
                || IsDisposed || Parent == null)
                return;

            if (!Parent.UpdateLocked)
            {
                Parent.Update();
            }
        }

        protected SKPaint _paintWithEffects = null;
        protected SKPaint _paintWithOpacity = null;



        private IAnimatorsManager _lastAnimatorManager;

        private Func<List<SkiaControl>> _createChildren;

        /// <summary>
        /// Can customize the SKPaint used for painting the object
        /// </summary>
        public Action<SKPaint, SKRect> CustomizeLayerPaint { get; set; }

        public SK3dView Helper3d;

        /// <summary>
        /// Used to check whether to apply IsClippedToBounds property
        /// </summary>
        public virtual bool WillClipBounds
        {
            get
            {
                return IsClippedToBounds ||
                       (UsingCacheType != SkiaCacheType.None && UsingCacheType != SkiaCacheType.Operations);
            }
        }

        public virtual bool WillClipEffects
        {
            get
            {
                return ClipEffects;
            }
        }

        SKPath _preparedClipBounds = null;


        public void DrawWithClipAndTransforms(
         SkiaDrawingContext ctx,
         SKRect destination,
         SKRect transformsArea,
         bool useOpacity,
         bool useClipping,
         Action<SkiaDrawingContext> draw)
        {
            bool isClipping = (WillClipBounds || Clipping != null) && useClipping;

            if (isClipping)
            {
                _preparedClipBounds ??= new SKPath();
                _preparedClipBounds.Reset();
                _preparedClipBounds.AddRect(destination);
                Clipping?.Invoke(_preparedClipBounds, destination);
            }

            bool applyOpacity = useOpacity && Opacity < 1;
            bool needTransform = HasTransform;

            if (applyOpacity || isClipping || needTransform || CustomizeLayerPaint != null)
            {
                _paintWithOpacity ??= new SKPaint
                {
                    IsAntialias = IsDistorted,
                    FilterQuality = IsDistorted ? SKFilterQuality.Medium : SKFilterQuality.None
                };

                _paintWithOpacity.Color = SKColors.White.WithAlpha((byte)(0xFF * Opacity));

                if (applyOpacity || CustomizeLayerPaint != null)
                {
                    CustomizeLayerPaint?.Invoke(_paintWithOpacity, destination);
                    ctx.Canvas.SaveLayer(_paintWithOpacity);
                }
                else
                {
                    ctx.Canvas.Save();
                }

                if (needTransform)
                {
                    ApplyTransforms(ctx, transformsArea);
                }

                if (isClipping)
                {
                    ctx.Canvas.ClipPath(_preparedClipBounds, SKClipOperation.Intersect, true);
                }

                draw(ctx);
                ctx.Canvas.Restore();
            }
            else
            {
                draw(ctx);
            }
        }

        protected virtual void ApplyTransforms(SkiaDrawingContext ctx, SKRect destination)
        {
            var moveX = (int)Math.Round(UseTranslationX * RenderingScale);
            var moveY = (int)Math.Round(UseTranslationY * RenderingScale);

            float pivotX = (float)(destination.Left + destination.Width * TransformPivotPointX);
            float pivotY = (float)(destination.Top + destination.Height * TransformPivotPointY);

            var centerX = moveX + destination.Left + destination.Width * TransformPivotPointX;
            var centerY = moveY + destination.Top + destination.Height * TransformPivotPointY;

            var skewX = SkewX > 0 ? (float)Math.Tan(Math.PI * SkewX / 180f) : 0f;
            var skewY = SkewY > 0 ? (float)Math.Tan(Math.PI * SkewY / 180f) : 0f;

            if (Rotation != 0)
            {
                ctx.Canvas.RotateDegrees((float)Rotation, (float)centerX, (float)centerY);
            }

            var matrixTransforms = new SKMatrix
            {
                TransX = moveX,
                TransY = moveY,
                Persp0 = Perspective1,
                Persp1 = Perspective2,
                SkewX = skewX,
                SkewY = skewY,
                Persp2 = 1,
                ScaleX = (float)ScaleX,
                ScaleY = (float)ScaleY
            };

            var drawingMatrix = SKMatrix.CreateTranslation((float)-pivotX, (float)-pivotY).PostConcat(matrixTransforms);

            if (CameraAngleX != 0 || CameraAngleY != 0 || CameraAngleZ != 0)
            {
                Helper3d ??= new();

                Helper3d.Save();
                Helper3d.RotateXDegrees(CameraAngleX);
                Helper3d.RotateYDegrees(CameraAngleY);
                Helper3d.RotateZDegrees(CameraAngleZ);
                if (CameraTranslationZ != 0) Helper3d.TranslateZ(CameraTranslationZ);
                drawingMatrix = drawingMatrix.PostConcat(Helper3d.Matrix);
                Helper3d.Restore();

            }

            drawingMatrix = drawingMatrix.PostConcat(SKMatrix.CreateTranslation(pivotX, pivotY))
                                          .PostConcat(ctx.Canvas.TotalMatrix);

            ctx.Canvas.SetMatrix(drawingMatrix);
        }



        public virtual bool NeedMeasure
        {
            get => _needMeasure;
            set
            {
                if (value == _needMeasure) return;
                _needMeasure = value;

                if (value)
                {
                    IsLayoutDirty = true;
                    RenderObjectNeedsUpdate = true;
                }
                //OnPropertyChanged(); disabled atm
            }
        }
        private bool _needMeasure = true;




        /// <summary>
        /// If attached to a SuperView and rendering is in progress will run after it. Run now otherwise.
        /// </summary>
        /// <param name="action"></param>
        protected void SafePostAction(Action action)
        {
            var super = this.Superview;
            if (super != null)
            {
                Superview.PostponeExecutionBeforeDraw(() =>
                {
                    action();
                });

                Repaint();
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// If attached to a SuperView will run only after draw to avoid memory access conflicts. If not attached will run after 3 secs..
        /// </summary>
        /// <param name="action"></param>
        protected void SafeAction(Action action)
        {
            var super = this.Superview;
            if (super == null || !Superview.IsRendering)
            {
                Tasks.StartDelayed(TimeSpan.FromSeconds(3), action);
            }
            else
                Superview.PostponeExecutionAfterDraw(action);
        }


        protected virtual void PaintWithEffects(
            SkiaDrawingContext ctx, SKRect destination, float scale, object arguments)
        {
            if (IsDisposed || IsDisposing)
                return;

            void draw(SkiaDrawingContext context)
            {
                Paint(context, destination, scale, arguments);
            }

            if (!DisableEffects && VisualEffects.Count > 0)
            {
                if (_paintWithEffects == null)
                {
                    _paintWithEffects = new()
                    {
                        IsAntialias = true,
                        FilterQuality = SKFilterQuality.Medium
                    };

                }

                var effectColor = EffectColorFilter;
                var effectImage = EffectImageFilter;

                if (effectImage != null)
                    _paintWithEffects.ImageFilter = effectImage.CreateFilter(destination);
                else
                    _paintWithEffects.ImageFilter = null;//will be disposed internally by effect

                if (effectColor != null)
                    _paintWithEffects.ColorFilter = effectColor.CreateFilter(destination);
                else
                    _paintWithEffects.ColorFilter = null;

                var restore = ctx.Canvas.SaveLayer(_paintWithEffects);

                bool hasDrawnControl = false;

                var renderers = EffectRenderers;

                if (renderers.Count > 0)
                {
                    foreach (var effect in renderers)
                    {
                        var chainedEffectResult = effect.Draw(destination, ctx, draw);
                        if (chainedEffectResult.DrawnControl)
                            hasDrawnControl = true;
                    }
                }

                if (!hasDrawnControl)
                {
                    draw(ctx);
                }

                ctx.Canvas.RestoreToCount(restore);
            }
            else
            {
                draw(ctx);
            }
        }


        /// <summary>
        /// This is the main drawing routine you should override to draw something.
        /// Base one paints background color inside DrawingRect that was defined by Arrange inside base.Draw.
        /// Pass arguments if you want to use some time-frozen data for painting at any time from any thread..
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="destination"></param>
        /// <param name="scale"></param>
        protected virtual void Paint(SkiaDrawingContext ctx, SKRect destination, float scale, object arguments)
        {
            if (destination.Width == 0 || destination.Height == 0 || IsDisposing || IsDisposed)
                return;

            PaintTintBackground(ctx.Canvas, destination);

            WasDrawn = true;
        }


        private bool _wasDrawn;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool WasDrawn
        {
            get
            {
                return _wasDrawn;
            }
            set
            {
                if (_wasDrawn != value)
                {
                    _wasDrawn = value;
                    OnPropertyChanged();
                }
            }
        }

        public Action<SKPath, SKRect> Clipping { get; set; }

        /// <summary>
        /// Create this control clip for painting content.
        /// Pass arguments if you want to use some time-frozen data for painting at any time from any thread..
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public virtual SKPath CreateClip(object arguments, bool usePosition, SKPath path = null)
        {
            path ??= new SKPath();

            if (usePosition)
            {
                path.AddRect(DrawingRect);
            }
            else
            {
                path.AddRect(new(0, 0, DrawingRect.Width, DrawingRect.Height));
            }
            return path;
        }


        public static SKColor DebugRenderingColor = SKColor.Parse("#66FFFF00");

        public double UseTranslationY
        {
            get
            {
                return TranslationY + AddTranslationY;
            }
        }

        public double UseTranslationX
        {
            get
            {
                return TranslationX + AddTranslationX;
            }
        }

        public bool HasTransform
        {
            get
            {
                return
                    UseTranslationY != 0 || UseTranslationX != 0
                                         || ScaleY != 1f || ScaleX != 1f
                                         || Perspective1 != 0f || Perspective2 != 0f
                                         || SkewX != 0 || SkewY != 0
                                         || Rotation != 0
                                         || CameraAngleX != 0 || CameraAngleY != 0 || CameraAngleZ != 0;
            }
        }

        public bool IsDistorted
        {
            get
            {
                return
                    Rotation != 0 || ScaleY != 1f || ScaleX != 1f
                    || Perspective1 != 0f || Perspective2 != 0f
                    || SkewX != 0 || SkewY != 0
                    || CameraAngleX != 0 || CameraAngleY != 0 || CameraAngleZ != 0;
            }
        }


        /// <summary>
        /// Drawing cache, applying clip and transforms as well
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="ctx"></param>
        /// <param name="destination"></param>
        public virtual void DrawRenderObject(CachedObject cache, SkiaDrawingContext ctx, SKRect destination)
        {
            //lock (LockDraw)
            {
                DrawWithClipAndTransforms(ctx, destination, destination, true, true, (ctx) =>
                {
                    if (_paintWithOpacity == null)
                    {
                        _paintWithOpacity = new SKPaint();
                    }

                    _paintWithOpacity.Color = SKColors.White;
                    _paintWithOpacity.IsAntialias = true;
                    _paintWithOpacity.FilterQuality = SKFilterQuality.Medium;

                    cache.Draw(ctx.Canvas, destination, _paintWithOpacity);
                });

            }

        }

        /// <summary>
        /// Use to render Absolute layout. Base method is not supporting templates, override it to implemen your logic.
        /// Returns number of drawn children.
        /// </summary>
        /// <param name="skiaControls"></param>
        /// <param name="context"></param>
        /// <param name="destination"></param>
        /// <param name="scale"></param>
        /// <param name="debug"></param>
        protected virtual int RenderViewsList(IEnumerable<SkiaControl> skiaControls,
            SkiaDrawingContext context,
            SKRect destination, float scale,
            bool debug = false)
        {
            var count = 0;
            foreach (var child in skiaControls)
            {
                if (child != null)
                {
                    child.OnBeforeDraw(); //could set IsVisible or whatever inside
                    if (child.CanDraw) //still visible 
                    {
                        count++;
                        child.Render(context, destination, scale);
                    }
                }
            }
            return count;
        }



        public bool Invalidated { get; set; } = true;

        /// <summary>
        /// For internal use
        /// </summary>

        public bool NeedUpdate
        {
            get
            {
                return _needUpdate;
            }
            set
            {
                if (_needUpdate != value)
                {
                    _needUpdate = value;

                    if (value)
                        RenderObjectNeedsUpdate = true;
                }
            }
        }
        bool _needUpdate;





        DrawnView _superview;

        public DrawnView Superview
        {
            get
            {
                if (_superview == null)
                {
                    var value = GetTopParentView();
                    if (value != _superview)
                    {
                        _superview = value;
                        OnPropertyChanged();
                        SuperViewChanged();
                    }
                }
                return _superview;
            }
            set
            {
                if (value != _superview)
                {
                    _superview = value;
                    OnPropertyChanged();
                    SuperViewChanged();
                }
            }
        }

        public virtual ScaledRect GetOnScreenVisibleArea()
        {
            if (this.UsingCacheType != SkiaCacheType.None)
            {
                //we are going to cache our children so they all must draw
                //regardless of the fact they might still be offscreen
                return ScaledRect.FromPixels(DrawingRect, RenderingScale);
            }

            //go up the tree to find the screen area or some parent will override this
            if (Parent != null)
            {
                return Parent.GetOnScreenVisibleArea();
            }

            if (Superview != null)
            {
                return Superview.GetOnScreenVisibleArea();
            }

            return ScaledRect.FromPixels(Destination, RenderingScale);
        }

        bool _lastUpdatedVisibility;


        protected virtual void UpdateInternal()
        {
            if (IsDisposing || IsDisposed)
                return;

            NeedUpdateFrontCache = true;
            NeedUpdate = true;

            if (UpdateLocked)
                return;

            if (IsParentIndependent)
                return;

            Parent?.UpdateByChild(this);
        }

        /// <summary>
        /// Main method to invalidate cache and invoke rendering
        /// </summary>
        public virtual void Update()
        {
            InvalidateCache();

            UpdateInternal();

            Updated?.Invoke(this, null);
        }

        /// <summary>
        /// Triggered by Update method
        /// </summary>
        public event EventHandler Updated;

        public static MemoryStream StreamFromString(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value ?? ""));
        }

        public static int DeviceUnitsToPixels(double units)
        {
            return (int)(units * GetDensity());
        }

        public static double PixelsToDeviceUnits(double units)
        {
            return units / GetDensity();
        }


        public SKRect Destination { get; protected set; }

        protected SKPaint PaintSystem { get; set; }


        /// <summary>
        /// Pixels, if you see no Scale parameter
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="destination"></param>
        public virtual void PaintTintBackground(SKCanvas canvas, SKRect destination)
        {

            if (BackgroundColor != null && BackgroundColor != Color.Transparent)
            {
                if (PaintSystem == null)
                {
                    PaintSystem = new SKPaint();
                }
                PaintSystem.Style = SKPaintStyle.StrokeAndFill;
                PaintSystem.Color = BackgroundColor.ToSKColor();
                PaintSystem.BlendMode = this.FillBlendMode;

                SetupGradient(PaintSystem, FillGradient, destination);

                canvas.DrawRect(destination, PaintSystem);
            }

        }

        protected SKPath CombineClipping(SKPath add, SKPath path)
        {
            if (path == null)
                return add;
            if (add != null)
                return add.Op(path, SKPathOp.Intersect);
            return null;
        }

        protected void ActionWithClipping(SKRect viewport, SKCanvas canvas, Action draw)
        {
            var clip = new SKPath();
            {
                clip.MoveTo(viewport.Left, viewport.Top);
                clip.LineTo(viewport.Right, viewport.Top);
                clip.LineTo(viewport.Right, viewport.Bottom);
                clip.LineTo(viewport.Left, viewport.Bottom);
                clip.MoveTo(viewport.Left, viewport.Top);
                clip.Close();
            }

            canvas.Save();

            canvas.ClipPath(clip, SKClipOperation.Intersect, true);

            draw();

            canvas.Restore();
        }


        //static int countRedraws = 0;
        protected static void NeedDraw(BindableObject bindable, object oldvalue, object newvalue)
        {

            try
            {
                var control = bindable as SkiaControl;
                {
                    control?.Update();
                }
            }
            catch (Exception e)
            {
                Super.Log(e);
            }

        }

        /// <summary>
        /// Just make us repaint to apply new transforms etc
        /// </summary>
        protected static void NeedRepaint(BindableObject bindable, object oldvalue, object newvalue)
        {
            var control = bindable as SkiaControl;
            {
                if (control != null && !control.IsDisposed)
                {
                    control.Repaint();
                }
            }
        }



        public virtual void CalculateMargins()
        {
            //use Margin property as starting point
            //if specific margin is set (>=0) apply 
            //to final Thickness
            var margin = Margin;

            margin.Left += AddMarginLeft;
            margin.Right += AddMarginRight;
            margin.Top += AddMarginTop;
            margin.Bottom += AddMarginBottom;

            Margins = margin;
        }

        public virtual Thickness GetAllMarginsInPixels(float scale)
        {
            var constraintLeft = Math.Round((Margins.Left + Padding.Left) * scale);
            var constraintRight = Math.Round((Margins.Right + Padding.Right) * scale);
            var constraintTop = Math.Round((Margins.Top + Padding.Top) * scale);
            var constraintBottom = Math.Round((Margins.Bottom + Padding.Bottom) * scale);
            return new(constraintLeft, constraintTop, constraintRight, constraintBottom);
        }

        public virtual Thickness GetMarginsInPixels(float scale)
        {
            var constraintLeft = Math.Round((Margins.Left) * scale);
            var constraintRight = Math.Round((Margins.Right) * scale);
            var constraintTop = Math.Round((Margins.Top) * scale);
            var constraintBottom = Math.Round((Margins.Bottom) * scale);
            return new(constraintLeft, constraintTop, constraintRight, constraintBottom);
        }

        ///// <summary>
        ///// Main method to call when dimensions changed
        ///// </summary>
        //public virtual void InvalidateMeasure()
        //{
        //    if (!IsDisposed)
        //    {
        //        CalculateMargins();
        //        CalculateSizeRequest();

        //        InvalidateWithChildren();
        //        InvalidateParent();

        //        Update();
        //    }
        //}

        public virtual void InvalidateMeasureInternal()
        {
            CalculateMargins();
            CalculateSizeRequest();
            InvalidateWithChildren();
            InvalidateParent();
        }

        protected virtual void CalculateSizeRequest()
        {
            SizeRequest = GetSizeRequest((float)WidthRequest, (float)HeightRequest, false);
        }

        /// <summary>
        /// Will invoke InvalidateInternal on controls and subviews
        /// </summary>
        /// <param name="control"></param>
        public virtual void InvalidateChildrenTree(SkiaControl control)
        {
            if (control != null)
            {
                control.NeedMeasure = true;

                foreach (var view in control.Views.ToList())
                {
                    InvalidateChildrenTree(view as SkiaControl);
                }
            }
        }

        public virtual void InvalidateChildrenTree()
        {
            foreach (var view in Views.ToList())
            {
                InvalidateChildrenTree(view as SkiaControl);
            }
        }

        public virtual void InvalidateWithChildren()
        {
            UpdateLocked = true;

            foreach (var view in Views) //will crash? why adapter nor used??
            {
                InvalidateChildren(view as SkiaControl);
            }

            UpdateLocked = false;

            InvalidateInternal();
        }

        /// <summary>
        /// Will invoke InvalidateInternal on controls and subviews
        /// </summary>
        /// <param name="control"></param>
        void InvalidateChildren(SkiaControl control)
        {
            if (control != null)
            {
                control.InvalidateInternal();

                foreach (var view in control.Views.ToList())
                {
                    InvalidateChildren(view as SkiaControl);
                }
            }
        }

        protected bool WillInvalidateMeasure { get; set; }

        protected static void NeedInvalidateMeasure(BindableObject bindable, object oldvalue, object newvalue)
        {
            if (bindable is SkiaControl control)
            {
                control.InvalidateMeasure();
            }
        }

        protected override void InvalidateMeasure()
        {
            InvalidateMeasureInternal();
            Update();
        }

        protected static void NeedInvalidateViewport(BindableObject bindable, object oldvalue, object newvalue)
        {

            var control = bindable as SkiaControl;
            {
                if (control != null && !control.IsDisposed)
                {
                    control.InvalidateViewport();
                }
            }
        }

        public virtual bool IsTemplated
        {
            get
            {
                return (this.ItemTemplate != null || ItemTemplateType != null);
            }
        }

        public virtual void OnItemTemplateChanged()
        {

        }

        public virtual object CreateContentFromTemplate()
        {
            if (ItemTemplateType != null)
            {
                return Activator.CreateInstance(ItemTemplateType);
            }
            return ItemTemplate.CreateContent();
        }

        protected static void ItemTemplateChanged(BindableObject bindable, object oldvalue, object newvalue)
        {

            var control = bindable as SkiaControl;
            {
                if (control != null && !control.IsDisposed)
                {
                    control.OnItemTemplateChanged();
                }
            }
        }
        static object lockViews = new();

        #region Animation 

        //public async Task PlayRippleAnimationAsync(Color color, double x, double y, bool removePrevious = true)
        //{
        //	var animation = new AfterEffectRipple()
        //	{
        //		X = x,
        //		Y = y,
        //		Color = color.ToSKColor(),
        //	};

        //	await PlayAnimation(animation, 350, null, removePrevious);
        //}


        public IAnimatorsManager GetAnimatorsManager()
        {
            return GetTopParentView() as IAnimatorsManager;

            //var parent = this.Parent;

            //if (parent is IAnimatorsManager manager)
            //{
            //    return manager;
            //}

            //if (parent is SkiaControl control)
            //    return control.GetAnimatorsManager();

            //return null;
        }

        public bool RegisterAnimator(ISkiaAnimator animator)
        {
            var top = GetAnimatorsManager();
            if (top != null)
            {
                _lastAnimatorManager = top;
                top.AddAnimator(animator);
                Repaint();
                return true;
            }
            return false;
        }

        public void UnregisterAnimator(Guid uid)
        {
            var top = GetAnimatorsManager();
            if (top == null)
            {
                top = _lastAnimatorManager;
            }
            top?.RemoveAnimator(uid);
        }

        public IEnumerable<ISkiaAnimator> UnregisterAllAnimatorsByType(Type type)
        {
            if (Superview != null)
                return Superview.UnregisterAllAnimatorsByType(type);

            return Array.Empty<ISkiaAnimator>();
        }

        /// <summary>
        /// Expecting input coordinates in POINTs and relative to control coordinates. Use GetOffsetInsideControlInPoints to help.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="removePrevious"></param>
        public async void PlayRippleAnimation(Color color, double x, double y, bool removePrevious = true)
        {
            if (removePrevious)
            {
                //UnregisterAllAnimatorsByType(typeof(RippleAnimator));
            }

            //Debug.WriteLine($"[RIPPLE] start play for '{Tag}'");
            var animation = new RippleAnimator(this)
            {
                Color = color.ToSKColor(),
                X = x,
                Y = y
            };
            animation.Start();
        }

        public async void PlayShimmerAnimation(Color color, float shimmerWidth = 50, float shimmerAngle = 45, int speedMs = 1000, bool removePrevious = true)
        {
            //Debug.WriteLine($"[SHIMMER] start play for '{Tag}'");
            if (removePrevious)
            {
                //UnregisterAllAnimatorsByType(typeof(ShimmerAnimator));
            }
            var animation = new ShimmerAnimator(this)
            {
                Color = color.ToSKColor(),
                ShimmerWidth = shimmerWidth,
                ShimmerAngle = shimmerAngle,
                Speed = speedMs
            };
            animation.Start();
        }

        #endregion

        #region GRADIENTS

        public SKShader CreateGradientAsShader(SKRect destination, SkiaGradient gradient)
        {
            if (gradient != null && gradient.Type != GradientType.None)
            {
                var colors = new List<SKColor>();
                foreach (var color in gradient.Colors.ToList())
                {
                    var usingColor = color;
                    if (gradient.Light < 1.0)
                    {
                        usingColor = usingColor.MakeDarker(100 - gradient.Light * 100);
                    }
                    else if (gradient.Light > 1.0)
                    {
                        usingColor = usingColor.MakeLighter(gradient.Light * 100 - 100);
                    }

                    var newAlpha = usingColor.A * gradient.Opacity;
                    usingColor = usingColor.WithAlpha(newAlpha);
                    colors.Add(usingColor.ToSKColor());
                }

                float[] colorPositions = null;
                if (gradient.ColorPositions?.Count == colors.Count)
                {
                    colorPositions = gradient.ColorPositions.Select(x => (float)x).ToArray();
                }

                switch (gradient.Type)
                {
                    case GradientType.Sweep:

                    float sweep = (float)Value3;//((float)this.Variable1 % (float)this.Variable2 / 100F) * 360.0F;

                    return SKShader.CreateSweepGradient(
                         new SKPoint(destination.Left + destination.Width / 2.0f,
                            destination.Top + destination.Height / 2.0f),
                        colors.ToArray(),
                        colorPositions,
                        gradient.TileMode, (float)Value1, (float)(Value1 + Value2));

                    case GradientType.Circular:
                    return SKShader.CreateRadialGradient(
                        new SKPoint(destination.Left + destination.Width / 2.0f,
                            destination.Top + destination.Height / 2.0f),
                        Math.Max(destination.Width, destination.Height) / 2.0f,
                        colors.ToArray(),
                        colorPositions,
                        gradient.TileMode);

                    case GradientType.Linear:
                    default:
                    return SKShader.CreateLinearGradient(
                        new SKPoint(destination.Left + destination.Width * gradient.StartXRatio,
                            destination.Top + destination.Height * gradient.StartYRatio),
                        new SKPoint(destination.Left + destination.Width * gradient.EndXRatio,
                            destination.Top + destination.Height * gradient.EndYRatio),
                        colors.ToArray(),
                        colorPositions,
                        gradient.TileMode);
                    break;
                }

            }

            return null;
        }

        /// <summary>
        /// Creates Shader for gradient and sets it to passed SKPaint along with BlendMode
        /// </summary>
        /// <param name="paint"></param>
        /// <param name="gradient"></param>
        /// <param name="destination"></param>
        public bool SetupGradient(SKPaint paint, SkiaGradient gradient, SKRect destination)
        {
            if (gradient != null && paint != null)
            {
                if (paint.Color.Alpha == 0)
                {
                    paint.Color = SKColor.FromHsl(0, 0, 0);
                }

                paint.Color = SKColors.White;
                paint.Shader = CreateGradientAsShader(destination, gradient);
                paint.BlendMode = gradient.BlendMode;

                return true;
            }

            return false;
        }

        #endregion


        #region CHILDREN VIEWS

        private IReadOnlyList<SkiaControl> _orderedChildren;

        public IReadOnlyList<SkiaControl> GetOrderedSubviews(bool recalculate = false)
        {
            if (_orderedChildren == null || recalculate)
            {
                _orderedChildren = Views.OrderBy(x => x.ZIndex).ToList();
            }
            return _orderedChildren;
        }


        public IReadOnlyList<SkiaControl> GetUnorderedSubviews(bool recalculate = false)
        {
            return Views;
        }

        public List<SkiaControl> Views { get; } = new();

        public virtual void DisposeChildren()
        {
            foreach (var child in Views.ToList())
            {
                RemoveSubView(child);
                child.Dispose();
            }
            Views.Clear();
            Invalidate();
        }

        public virtual void SetWillDisposeWithChildren()
        {
            IsDisposing = true;

            foreach (var child in Views.ToList())
            {
                child.SetWillDisposeWithChildren();
            }
        }

        public virtual void ClearChildren()
        {
            foreach (var child in Views.ToList())
            {
                RemoveSubView(child);
            }

            Views.Clear();

            Invalidate();
        }

        public virtual void InvalidateViewsList()
        {
            _orderedChildren = null;
            //NeedMeasure = true;
        }

        protected virtual void OnDrawnChildAdded(SkiaControl child)
        {
            Invalidate();
        }


        protected virtual void OnDrawnChildRemoved(SkiaControl child)
        {
            Invalidate();
        }

        public DateTime GestureListenerRegistrationTime { get; set; }

        public void RegisterGestureListener(ISkiaGestureListener gestureListener)
        {
            lock (LockIterateListeners)
            {
                gestureListener.GestureListenerRegistrationTime = DateTime.UtcNow;
                GestureListeners.Add(gestureListener);
                //Debug.WriteLine($"Added {gestureListener} to gestures of {this.Tag} {this}");
            }
        }

        public void UnregisterGestureListener(ISkiaGestureListener gestureListener)
        {
            lock (LockIterateListeners)
            {
                HadInput.Remove(gestureListener.Uid);
                GestureListeners.Remove(gestureListener);
                //Debug.WriteLine($"Removed {gestureListener} from gestures of {this.Tag} {this}");
            }
        }

        /// <summary>
        /// Children we should check for touch hits
        /// </summary>
        public SortedSet<ISkiaGestureListener> GestureListeners { get; } = new(new DescendingZIndexGestureListenerComparer());

        public virtual void OnParentChanged(IDrawnBase newvalue, IDrawnBase oldvalue)
        {
            if (newvalue != null && newvalue is SkiaControl control)
            {
                Superview = control.Superview;
            }

            if (newvalue != null)
                Update();

            ParentChanged?.Invoke(this, Parent);
        }

        static object lockParent = new();

        public virtual void SetParent(IDrawnBase parent)
        {
            //lock (lockParent)
            {
                if (Parent == parent)
                    return;

                var iAmGestureListener = this as ISkiaGestureListener;

                //clear previous
                if (Parent is IDrawnBase oldParent)
                {
                    //kill gestures
                    if (iAmGestureListener != null)
                    {
                        oldParent.UnregisterGestureListener(iAmGestureListener);
                    }

                    //fill animations
                    Superview?.UnregisterAllAnimatorsByParent(this);

                    oldParent.Views.Remove(this);

                    if (oldParent is SkiaControl skiaParent)
                    {
                        skiaParent.InvalidateViewsList();
                    }
                }

                if (parent == null)
                {
                    Parent = null;
                    //BindingContext = null;

                    //this.SizeChanged -= OnFormsSizeChanged;
                    return;
                }

                if (!parent.Views.Contains(this))
                {
                    parent.Views.Add(this);
                    if (parent is SkiaControl skiaParent)
                    {
                        skiaParent.InvalidateViewsList();
                    }
                }

                Parent = parent;

                if (iAmGestureListener != null)
                {
                    parent.RegisterGestureListener(iAmGestureListener);
                }

                if (parent is IDrawnBase control)
                {
                    //control.InvalidateViewsList();
                    if (BindingContext == null)
                        BindingContext = control.BindingContext;
                }

                //if (this is ISkiaHasChildren)
                //{
                //    ((ISkiaHasChildren)this).LayoutInvalidated = true;
                //}

                //this.SizeChanged += OnFormsSizeChanged;

                InvalidateInternal();
            }

        }


        #endregion

        public virtual void RegisterGestureListenersTree(SkiaControl control)
        {
            if (control.Parent == null)
                return;

            if (control is ISkiaGestureListener listener)
            {
                control.Parent.RegisterGestureListener(listener);
            }
            foreach (var view in Views)
            {
                view.RegisterGestureListenersTree(view);
            }
        }

        public virtual void UnregisterGestureListenersTree(SkiaControl control)
        {
            if (control.Parent == null)
                return;

            if (control is ISkiaGestureListener listener)
            {
                control.Parent.UnregisterGestureListener(listener);
            }
            foreach (var view in Views)
            {
                view.UnregisterGestureListenersTree(view);
            }
        }


        #region Children


        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate),
            typeof(DataTemplate), null,
            propertyChanged: ItemTemplateChanged);
        /// <summary>
        /// Kind of BindableLayout.DrawnTemplate
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public static readonly BindableProperty ItemTemplateTypeProperty = BindableProperty.Create(
            nameof(ItemTemplateType),
            typeof(Type),
            typeof(SkiaControl),
            null
            , propertyChanged: ItemTemplateChanged);

        /// <summary>
        /// ItemTemplate alternative for faster creation
        /// </summary>
        public Type ItemTemplateType
        {
            get { return (Type)GetValue(ItemTemplateTypeProperty); }
            set { SetValue(ItemTemplateTypeProperty, value); }
        }


        #region SELECTABLE TEMPLATES


        public static readonly BindableProperty TemplatesProperty = BindableProperty.Create(
            nameof(Templates),
            typeof(IList<SkiaControl>),
            typeof(SkiaControl),
            defaultValueCreator: (instance) =>
            {
                var created = new ObservableCollection<SkiaControl>();
                ItemTemplateChanged(instance, null, created);
                return created;
            },
            validateValue: (bo, v) => v is IList<SkiaControl>,
            propertyChanged: ItemTemplateChanged,
            coerceValue: CoerceTemplates);

        public IList<SkiaControl> Templates
        {
            get => (IList<SkiaControl>)GetValue(TemplatesProperty);
            set => SetValue(TemplatesProperty, value);
        }

        private static object CoerceTemplates(BindableObject bindable, object value)
        {
            if (!(value is ReadOnlyCollection<SkiaControl> readonlyCollection))
            {
                return value;
            }

            return new ReadOnlyCollection<SkiaControl>(
                readonlyCollection.ToList());
        }

        private void OnSkiaPropertyTemplateCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is SkiaControl control)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    control.OnItemTemplateChanged();
                    break;

                    case NotifyCollectionChangedAction.Reset:
                    case NotifyCollectionChangedAction.Remove:
                    control.OnItemTemplateChanged();
                    break;
                }
            }
        }




        #endregion

        public static readonly BindableProperty ChildrenProperty = BindableProperty.Create(
            nameof(Children),
            typeof(IList<SkiaControl>),
            typeof(SkiaControl),
            defaultValueCreator: (instance) =>
            {
                var created = new ObservableCollection<SkiaControl>();
                ChildrenPropertyChanged(instance, null, created);
                return created;
            },
            validateValue: (bo, v) => v is IList<SkiaControl>,
            propertyChanged: ChildrenPropertyChanged);

        public IList<SkiaControl> Children
        {
            get => (IList<SkiaControl>)GetValue(ChildrenProperty);
            set => SetValue(ChildrenProperty, value);
        }

        protected void AddOrRemoveView(SkiaControl subView, bool add)
        {
            if (subView != null)
            {
                if (add)
                {
                    AddSubView(subView);
                }
                else
                {
                    RemoveSubView(subView);
                }

            }
        }

        public virtual void SetChildren(IEnumerable<SkiaControl> views)
        {
            ClearChildren();
            foreach (var child in views)
            {
                AddOrRemoveView(child, true);
            }
        }

        private static void ChildrenPropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
        {
            if (bindable is SkiaControl skiaControl)
            {

                var enumerableChildren = (IEnumerable<SkiaControl>)newvalue;

                if (oldvalue != null)
                {
                    var oldViews = (IEnumerable<SkiaControl>)oldvalue;

                    if (oldvalue is INotifyCollectionChanged oldCollection)
                    {
                        oldCollection.CollectionChanged -= skiaControl.OnChildrenCollectionChanged;
                    }

                    foreach (var subView in oldViews)
                    {
                        skiaControl.AddOrRemoveView(subView, false);
                    }
                }

                if (skiaControl.ItemTemplate == null)
                {
                    skiaControl.SetChildren(enumerableChildren);
                }

                //foreach (var subView in enumerableChildren)
                //{
                //	subView.SetParent(skiaControl);
                //}

                if (newvalue is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged -= skiaControl.OnChildrenCollectionChanged;
                    newCollection.CollectionChanged += skiaControl.OnChildrenCollectionChanged;
                }

                skiaControl.Update();
            }

        }

        public bool HasItemTemplate
        {
            get
            {
                return ItemTemplate != null;
            }
        }

        private void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (HasItemTemplate)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                foreach (SkiaControl newChildren in e.NewItems)
                {
                    AddOrRemoveView(newChildren, true);
                }
                break;

                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Remove:
                foreach (SkiaControl oldChildren in e.OldItems ?? Array.Empty<SkiaControl>())
                {
                    AddOrRemoveView(oldChildren, false);
                }

                break;
            }

            Update();
        }

        #endregion


        public AddGestures.GestureListener GesturesEffect { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (float X, float Y) RescaleAspect(float width, float height, SKRect dest, TransformAspect stretch)
        {
            float aspectX = 1;
            float aspectY = 1;

            var s1 = dest.Width / width;
            var s2 = dest.Height / height;

            switch (stretch)
            {
                case TransformAspect.None:
                break;

                case TransformAspect.Fit:
                aspectX = dest.Width < width ? dest.Width / width : 1;
                aspectY = dest.Height < height ? dest.Height / height : 1;
                break;

                case TransformAspect.Fill:
                aspectX = width < dest.Width ? s1 : 1;
                aspectY = height < dest.Height ? s2 : 1;
                break;

                case TransformAspect.AspectFit:
                aspectX = Math.Min(s1, s2);
                aspectY = aspectX;
                break;

                case TransformAspect.AspectFill:
                aspectX = width < dest.Width ? Math.Max(s1, s2) : 1;
                aspectY = aspectX;
                break;

                case TransformAspect.Cover:
                aspectX = s1;
                aspectY = s2;
                break;

                case TransformAspect.AspectCover:
                aspectX = Math.Max(s1, s2);
                aspectY = aspectX;
                break;

                case TransformAspect.AspectFitFill:
                var imageAspect = width / height;
                var viewportAspect = dest.Width / dest.Height;

                if (imageAspect > viewportAspect) // Image is wider than viewport
                {
                    aspectX = dest.Width / width;
                    aspectY = aspectX;
                }
                else // Image is taller than viewport
                {
                    aspectY = dest.Height / height;
                    aspectX = aspectY;
                }
                break;
            }

            return (aspectX, aspectY);
        }

        #region HELPERS

        /// <summary>
        /// Creates and sets an ImageFilter for SKPaint
        /// </summary>
        /// <param name="paintShadow"></param>
        /// <param name="shadow"></param>
        public static void AddShadowFilter(SKPaint paintShadow, SkiaShadow shadow, float scale)
        {
            var colorShadow = shadow.Color;
            if (colorShadow.A == 1.0)
            {
                colorShadow = shadow.Color.WithAlpha((float)shadow.Opacity);
            }
            if (shadow.ShadowOnly)
            {
                paintShadow.ImageFilter = SKImageFilter.CreateDropShadowOnly(
                    (float)(shadow.X * scale), (float)(shadow.Y * scale),
                    (float)(shadow.Blur * scale), (float)(shadow.Blur * scale),
                    colorShadow.ToSKColor());
            }
            else
            {
                paintShadow.ImageFilter = SKImageFilter.CreateDropShadow(
                    (float)(shadow.X * scale), (float)(shadow.Y * scale),
                    (float)(shadow.Blur * scale), (float)(shadow.Blur * scale),
                    colorShadow.ToSKColor());
            }
        }

        public static Random Random = new Random();
        protected double _arrangedViewportHeightLimit;
        protected double _arrangedViewportWidthLimit;
        protected float _lastMeasuredForScale;
        private bool _isLayoutDirty;
        private Thickness _margins;
        private SKRect _lastArrangedInside;
        private double _lastArrangedForScale;
        private bool _needUpdateFrontCache;
        private SKRect _drawingRect;

        public static Color GetRandomColor()
        {
            byte r = (byte)Random.Next(256);
            byte g = (byte)Random.Next(256);
            byte b = (byte)Random.Next(256);

            return Color.FromRgb(r, g, b);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static bool CompareRects(SKRect a, SKRect b, float precision)
        //{
        //    return
        //        Math.Abs(a.Left - b.Left) <= precision
        //                 && Math.Abs(a.Top - b.Top) <= precision
        //                             && Math.Abs(a.Right - b.Right) <= precision
        //                             && Math.Abs(a.Bottom - b.Bottom) <= precision;
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static bool CompareRectsSize(SKRect a, SKRect b, float precision)
        //{
        //    return
        //        Math.Abs(a.Width - b.Width) <= precision
        //        && Math.Abs(a.Height - b.Height) <= precision;
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareFloats(float a, float b, float precision)
        {
            return Math.Abs(a - b) <= precision;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareDoubles(double a, double b, double precision)
        {
            return Math.Abs(a - b) <= precision;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareRects(SKRect a, SKRect b, float precision)
        {
            if ((float.IsInfinity(a.Left) && float.IsInfinity(b.Left)) || Math.Abs(a.Left - b.Left) <= precision)
            {
                if ((float.IsInfinity(a.Top) && float.IsInfinity(b.Top)) || Math.Abs(a.Top - b.Top) <= precision)
                {
                    if ((float.IsInfinity(a.Right) && float.IsInfinity(b.Right)) || Math.Abs(a.Right - b.Right) <= precision)
                    {
                        if ((float.IsInfinity(a.Bottom) && float.IsInfinity(b.Bottom)) || Math.Abs(a.Bottom - b.Bottom) <= precision)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareRectsSize(SKRect a, SKRect b, float precision)
        {
            if ((float.IsInfinity(a.Width) && float.IsInfinity(b.Width)) || (Math.Abs(a.Width - b.Width) <= precision))
            {
                if ((float.IsInfinity(a.Height) && float.IsInfinity(b.Height)) || (Math.Abs(a.Height - b.Height) <= precision))
                {
                    return true;
                }
            }
            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareSize(SKSize a, SKSize b, float precision)
        {
            if ((float.IsInfinity(a.Width) && float.IsInfinity(b.Width)) || Math.Abs(a.Width - b.Width) <= precision)
            {
                if ((float.IsInfinity(a.Height) && float.IsInfinity(b.Height)) || Math.Abs(a.Height - b.Height) <= precision)
                {
                    return true;
                }
            }
            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreEqual(double v1, double v2, double precision)
        {
            if (double.IsInfinity(v1) && double.IsInfinity(v2) || Math.Abs(v1 - v2) <= precision)
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreEqual(float v1, float v2, float precision)
        {
            if (float.IsInfinity(v1) && float.IsInfinity(v2) || Math.Abs(v1 - v2) <= precision)
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreVectorsEqual(Vector2 v1, Vector2 v2, float precision)
        {
            if ((float.IsInfinity(v1.X) && float.IsInfinity(v2.X)) || Math.Abs(v1.X - v2.X) <= precision)
            {
                if ((float.IsInfinity(v1.Y) && float.IsInfinity(v2.Y)) || Math.Abs(v1.Y - v2.Y) <= precision)
                {
                    return true;
                }
            }
            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DirectionType GetDirectionType(Vector2 velocity, DirectionType defaultDirection, float ratio)
        {
            float x = Math.Abs(velocity.X);
            float y = Math.Abs(velocity.Y);

            if (x > y && x / y > ratio)
            {
                //Debug.WriteLine($"[DirectionType] H {x:0.0},{y:0.0} = {x / y:0.00}");
                return DirectionType.Horizontal;
            }
            else if (y > x && y / x >= ratio)
            {
                return DirectionType.Vertical;
            }
            else
            {
                return defaultDirection;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DirectionType GetDirectionType(Vector2 start, Vector2 end, float ratio)
        {
            // Calculate the absolute differences between the X and Y coordinates
            float x = Math.Abs(end.X - start.X);
            float y = Math.Abs(end.Y - start.Y);

            // Compare the differences to determine the dominant direction
            if (x > y && x / y > ratio)
            {
                return DirectionType.Horizontal;
            }
            else if (y > x && y / x >= ratio)
            {
                return DirectionType.Vertical;
            }
            else
            {
                return DirectionType.None; // The direction is neither horizontal nor vertical (the vectors are equal or diagonally aligned)
            }
        }



        /// <summary>
        /// Ported from Avalonia: AreClose - Returns whether or not two floats are "close".  That is, whether or 
        /// not they are within epsilon of each other.
        /// </summary> 
        /// <param name="value1"> The first float to compare. </param>
        /// <param name="value2"> The second float to compare. </param>
        public static bool AreClose(float value1, float value2)
        {
            //in case they are Infinities (then epsilon check does not work)
            if (value1 == value2) return true;
            float eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0f) * float.Epsilon;
            float delta = value1 - value2;
            return (-eps < delta) && (eps > delta);
        }


        /// <summary>
        /// Ported from Avalonia: AreClose - Returns whether or not two doubles are "close".  That is, whether or 
        /// not they are within epsilon of each other.
        /// </summary> 
        /// <param name="value1"> The first double to compare. </param>
        /// <param name="value2"> The second double to compare. </param>
        public static bool AreClose(double value1, double value2)
        {
            //in case they are Infinities (then epsilon check does not work)
            if (value1 == value2) return true;
            double eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * double.Epsilon;
            double delta = value1 - value2;
            return (-eps < delta) && (eps > delta);
        }

        /// <summary>
        /// Avalonia: IsOne - Returns whether or not the double is "close" to 1.  Same as AreClose(double, 1),
        /// but this is faster.
        /// </summary>
        /// <param name="value"> The double to compare to 1. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOne(double value)
        {
            return Math.Abs(value - 1.0) < 10.0 * double.Epsilon;
        }



        #endregion
    }

    public static class Snapping
    {
        /// <summary>
        /// Used by the layout system to round a position translation value applying scale and initial anchor. Pass POINTS only, it wont do its job when receiving pixels!
        /// </summary>
        /// <param name="initialPosition"></param>
        /// <param name="translation"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SnapPointsToPixel(float initialPosition, float translation, double scale)
        {
            // Scale the initial and the translation
            float scaledInitial = initialPosition * (float)scale;
            float scaledTranslation = translation * (float)scale;

            // Add the scaled initial to the scaled translation
            float scaledTotal = scaledInitial + scaledTranslation;

            // Round to the nearest integer (you could also use Floor or Ceiling or Round, play with it 
            float snappedTotal = (float)Math.Round(scaledTotal);

            // Subtract the scaled initial position to get the snapped, scaled translation
            float snappedScaledTranslation = snappedTotal - scaledInitial;

            // Divide by scale to get back to your original units
            float snappedTranslation = snappedScaledTranslation / (float)scale;

            return snappedTranslation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SnapPixelsToPixel(float initialPosition, float translation)
        {
            // Sum the initial position and the translation
            float total = initialPosition + translation;

            // Round to the nearest integer to snap to the nearest pixel
            float snappedTotal = (float)Math.Round(total);

            // Find out how much we've adjusted the translation by
            float snappedTranslation = snappedTotal - initialPosition;

            return snappedTranslation;
        }



    }
}