﻿using DrawnUi.Maui.Draw;
using DrawnUi.Maui.Draw;
using DrawnUi.Maui.Infrastructure.Helpers;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xamarin.Essentials;

namespace DrawnUi.Maui.Draw;

public class SnappingLayout : SkiaLayout
{
	#region EVENTS

	public event EventHandler OnViewportReady;

	public event EventHandler<bool> OnTransitionChanged;

	#endregion

	#region SCROLLING



	/// <summary>
	/// There are the bounds the scroll offset can go to.. This are NOT the bounds of the whole content.
	/// </summary>
	protected SKRect ContentOffsetBounds { get; set; }

	public virtual Vector2 ClampOffset(float x, float y, bool rubber)
	{
		if (!rubber)
		{
			var clampedX = Math.Max(ContentOffsetBounds.Left, Math.Min(ContentOffsetBounds.Right, x));
			var clampedY = Math.Max(ContentOffsetBounds.Top, Math.Min(ContentOffsetBounds.Bottom, y));

			return new Vector2(clampedX, clampedY);
		}

		return ClampOffsetWithRubberBand(x, y);
	}

	/// <summary>
	/// Called for manual finger panning
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <returns></returns>
	protected virtual Vector2 ClampOffsetWithRubberBand(float x, float y)
	{
		var clampedElastic = RubberBandUtils.ClampOnTrack(new Vector2(x, y), ContentOffsetBounds, (float)RubberEffect);

		return clampedElastic;
	}

	/// <summary>
	/// Using this instead of RenderingViewport
	/// </summary>
	protected SKRect Viewport { get; set; }

	/// <summary>
	/// There are the bounds the scroll offset can go to.. This are NOT the bounds of the whole content.
	/// </summary>
	public virtual SKRect GetContentOffsetBounds()
	{
		var width = ContentSize.Units.Width - Viewport.Width / RenderingScale;
		var height = ContentSize.Units.Height - Viewport.Height / RenderingScale;

		if (height < 0)
			height = 0;

		if (width < 0)
			width = 0;

		var rect = new SKRect(-width, -height, 0, 0);
		return rect;
	}

	public IList<Vector2> SnapPoints { get; set; } = new List<Vector2>();

	public virtual Vector2 FindNearestAnchor(Vector2 current)
	{
		// Order SnapPoints by distance from current
		IEnumerable<Vector2> orderedSnapPoints = SnapPoints.OrderBy(item => Vector2.Distance(item, current));

		// If there is at least one point, return the nearest one. 
		// Otherwise return the current position.
		return orderedSnapPoints.Any() ? orderedSnapPoints.First() : current;
	}

	protected virtual Vector2 FindNearestAnchorInternal(Vector2 current, Vector2 velocity)
	{
		// Order SnapPoints by distance from current
		IEnumerable<Vector2> orderedSnapPoints = SnapPoints.OrderBy(item => Vector2.Distance(item, current)).ToList();

		// If there is at least one point, return the nearest one. 
		// Otherwise return the current position.
		return orderedSnapPoints.Any() ? orderedSnapPoints.First() : current;
	}

	/// <summary>
	/// Return an anchor depending on direction and strength of of the velocity
	/// </summary>
	/// <param name="origin"></param>
	/// <param name="velocity"></param>
	/// <returns></returns>
	public virtual Vector2 SelectNextAnchor(Vector2 origin, Vector2 velocity)
	{
		// Normalize the direction vector
		Vector2 normDirection = Vector2.Normalize(velocity);

		// Order SnapPoints by distance from current
		var orderedSnapPoints = SnapPoints.OrderBy(item => Vector2.Distance(item, origin));

		foreach (Vector2 anchor in orderedSnapPoints)
		{
			// Calculate the direction to the current anchor
			Vector2 currentDirection = Vector2.Normalize(anchor - origin);

			// Check if the anchor is in the desired direction
			if (Vector2.Dot(normDirection, currentDirection) > 0)
			{
				// This anchor is in the desired direction, so return it
				return anchor;
			}
		}

		// If no suitable anchor is found, return the current position
		return origin;
	}

	/// <summary>
	/// Part of the distance between snap points the velocity need to cover to trigger going to the next snap point
	/// </summary>
	public double SnapDistanceRatio { get; set; } = 0.2;

	public virtual void ScrollToNearestAnchor(Vector2 location, Vector2 velocity)
	{
		if (SnapPoints.Count == 0)
		{
			return;
		}

		var origin = FindNearestAnchorInternal(location, velocity);

		// Find the anchor that the current velocity would move towards
		Vector2 projectionAnchor = SelectNextAnchor(origin, velocity);

		// Calculate the distance between the origin and the projectionAnchor
		float distance = Vector2.Distance(origin, projectionAnchor);

		// Calculate the projected position along the direction of the velocity
		Vector2 projectedPosition = origin + velocity;

		// Calculate the distance to the anchor in the opposite direction
		Vector2 oppositeAnchor = SelectNextAnchor(origin, -velocity);
		float oppositeDistance = Vector2.Distance(origin, oppositeAnchor);

		Vector2 targetAnchor;
		if (Vector2.Distance(origin, projectedPosition) <= distance * SnapDistanceRatio && oppositeDistance < distance)
		{
			targetAnchor = oppositeAnchor;
		}
		else
		{
			targetAnchor = projectionAnchor;
		}

		if (Vector2.Distance(location, targetAnchor) >= 0.5) //todo move threshold to options
		{
			ScrollToOffset(targetAnchor, velocity, CanAnimate);
		}

		UpdateReportedPosition();
	}

	public virtual bool CanAnimate
	{
		get
		{
			return CanDraw && LayoutReady;
		}
	}

	protected bool ScrollLocked { get; set; }

	protected SpringWithVelocityVectorAnimator VectorAnimatorSpring;

	protected RangeVectorAnimator _animatorRange;
	private Vector2 _currentPosition;

	protected Vector2 CurrentSnap { get; set; } = new(-1, -1);

	/// <summary>
	/// todo calc upon measured size + prop for speed
	/// </summary>
	/// <param name="displacement"></param>
	/// <returns></returns>
	protected virtual Vector2 GetAutoVelocity(Vector2 displacement)
	{
		//todo calc upon measured size + prop for speed
		var v = (float)AutoVelocityMultiplyPts * RenderingScale;
		return new Vector2(-v * Math.Sign(displacement.X), -v * Math.Sign(displacement.Y));
	}


	/// <summary>
	/// In Units
	/// </summary>lo
	/// <param name="offset"></param>
	/// <param name="animate"></param>
	protected virtual bool ScrollToOffset(Vector2 targetOffset, Vector2 velocity, bool animate)
	{
		if (ScrollLocked || targetOffset == CurrentSnap)
		{
			return false;
		}

		if (animate && Height > 0)
		{
			VectorAnimatorSpring?.Stop();

			var start = new Vector2((float)TranslationX, (float)TranslationY);
			var end = new Vector2((float)targetOffset.X, (float)targetOffset.Y);

			var displacement = start - end;

			if (velocity == Vector2.Zero)
				velocity = GetAutoVelocity(displacement);

			//if (Math.Abs(displacement.Length()) >= 0.5) //todo move threshold to options

			if (displacement != Vector2.Zero)
			{
				if (Bounces)
				{
					var spring = new Spring((float)(1 * (1 + RubberDamping)), 200, (float)(0.5f * (1 + RubberDamping)));
					VectorAnimatorSpring.Initialize(end, displacement, velocity, spring);

					VectorAnimatorSpring.Start();
				}
				else
				{
					var direction = GetDirectionType(start, end, 0.8f);
					var speed = 0.3;
					if (direction == DirectionType.Vertical)
					{
						speed *= (Math.Abs(end.Y - start.Y) / Height);
					}
					else
					if (direction == DirectionType.Horizontal)
					{
						speed *= (Math.Abs(end.X - start.X) / Height);
					}
					_animatorRange.Initialize(start, end, (float)speed, Easing.CubicInOut);
					_animatorRange.Start();
				}
			}

			//_animatorFling.Stop();
			//var contentOffset = new Vector2((float)TranslationX, (float)TranslationY);
			//FlingTo(contentOffset, targetOffset, 0.200f);
		}
		else
		{
			ApplyPosition(targetOffset);
		}

		CurrentSnap = targetOffset;

		return true;

	}


	protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		base.OnPropertyChanged(propertyName);

		if (propertyName.IsEither(nameof(TranslationX), nameof(TranslationY)))
		{
			CurrentPosition = new((float)TranslationX, (float)TranslationY);
			MainThread.BeginInvokeOnMainThread(() =>
			{
				InTransition = !CheckTransitionEnded();
			});
		}
	}

	protected Vector2 _appliedPosition;
	public virtual void ApplyPosition(Vector2 position)
	{
		_appliedPosition = position;

		TranslationX = position.X;
		TranslationY = position.Y;

		MainThread.BeginInvokeOnMainThread(() =>
		{
			InTransition = !CheckTransitionEnded();
		});

		SendScrolled();
	}

	public virtual void SendScrolled()
	{
		Scrolled?.Invoke(this, _appliedPosition);
	}

	public event EventHandler<Vector2> Scrolled;

	public virtual bool CheckTransitionEnded()
	{
		var equal = AreVectorsEqual(CurrentPosition, CurrentSnap, 1);
		//Debug.WriteLine($"[AreVectorsEqual] {CurrentPosition} {CurrentSnap} {equal}");
		return equal;
	}

	public Vector2 CurrentPosition
	{
		get => _currentPosition;
		protected set
		{
			if (value.Equals(_currentPosition)) return;
			_currentPosition = value;
			OnPropertyChanged();
			//Debug.WriteLine($"[X] {value.X:0.0}");
		}
	}

	public virtual void UpdateReportedPosition()
	{

	}

	#endregion

	#region PROPERTIES

	public virtual void ApplyOptions()
	{

	}

	public static readonly BindableProperty AnimatedProperty = BindableProperty.Create(
		nameof(Animated),
		typeof(bool),
		typeof(SnappingLayout),
		true);

	public bool Animated
	{
		get { return (bool)GetValue(AnimatedProperty); }
		set { SetValue(AnimatedProperty, value); }
	}


	protected static void NeedApplyOptions(BindableObject bindable, object oldvalue, object newvalue)
	{
		if (bindable is SnappingLayout control)
		{
			control.ApplyOptions();
		}
	}

	public static readonly BindableProperty RespondsToGesturesProperty = BindableProperty.Create(
		nameof(RespondsToGestures),
		typeof(bool),
		typeof(SnappingLayout),
		true);

	/// <summary>
	/// Can be open/closed by gestures along with code-behind, default is true
	/// </summary>
	public bool RespondsToGestures
	{
		get { return (bool)GetValue(RespondsToGesturesProperty); }
		set { SetValue(RespondsToGesturesProperty, value); }
	}

	public static readonly BindableProperty IgnoreWrongDirectionProperty = BindableProperty.Create(
		nameof(IgnoreWrongDirection),
		typeof(bool),
		typeof(SnappingLayout),
		false);

	/// <summary>
	/// Will ignore gestures of the wrong direction, like if this Orientation is Horizontal will ignore gestures with vertical direction velocity
	/// </summary>
	public bool IgnoreWrongDirection
	{
		get { return (bool)GetValue(IgnoreWrongDirectionProperty); }
		set { SetValue(IgnoreWrongDirectionProperty, value); }
	}


	public static readonly BindableProperty BouncesProperty = BindableProperty.Create(
		nameof(Bounces),
		typeof(bool),
		typeof(SnappingLayout),
		false);

	public bool Bounces
	{
		get { return (bool)GetValue(BouncesProperty); }
		set { SetValue(BouncesProperty, value); }
	}


	public static readonly BindableProperty InTransitionProperty = BindableProperty.Create(
		nameof(InTransition),
		typeof(bool),
		typeof(SnappingLayout),
		false, BindingMode.OneWayToSource,
		propertyChanged: (b, o, n) =>
		{
			if (b is SnappingLayout control)
			{
				var changed = (bool)n;
				control.OnTransitionChanged?.Invoke(control, changed);
				if (!changed)
				{
					control.SendScrolled();
				}
			}
		});

	public bool InTransition
	{
		get { return (bool)GetValue(InTransitionProperty); }
		set { SetValue(InTransitionProperty, value); }
	}


	public static readonly BindableProperty RubberDampingProperty = BindableProperty.Create(
		nameof(RubberDamping),
		typeof(double),
		typeof(SnappingLayout),
		0.8);

	/// <summary>
	/// If Bounce is enabled this basically controls how less the scroll will bounce when displaced from limit by finger or inertia. Default is 0.8.
	/// </summary>
	public double RubberDamping
	{
		get { return (double)GetValue(RubberDampingProperty); }
		set { SetValue(RubberDampingProperty, value); }
	}

	public static readonly BindableProperty AutoVelocityMultiplyPtsProperty = BindableProperty.Create(
		nameof(AutoVelocityMultiplyPts),
		typeof(double),
		typeof(SnappingLayout),
		25.0);

	/// <summary>
	/// If velocity is near 0 define how much we multiply the auto-velocity used to animate snappoing point. For example when in carousel you cancel the swipe and release finger..
	/// </summary>
	public double AutoVelocityMultiplyPts
	{
		get { return (double)GetValue(AutoVelocityMultiplyPtsProperty); }
		set { SetValue(AutoVelocityMultiplyPtsProperty, value); }
	}

	public static readonly BindableProperty RubberEffectProperty = BindableProperty.Create(
		nameof(RubberEffect),
		typeof(double),
		typeof(SnappingLayout),
		0.15);

	/// <summary>
	/// If Bounce is enabled this basically controls how far from the limit can the scroll be elastically offset by finger or inertia. Default is 0.15.
	/// </summary>
	public double RubberEffect
	{
		get { return (double)GetValue(RubberEffectProperty); }
		set { SetValue(RubberEffectProperty, value); }
	}

	#endregion
}