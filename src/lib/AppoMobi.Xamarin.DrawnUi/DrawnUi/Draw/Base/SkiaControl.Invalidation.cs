﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawnUi.Maui.Draw
{
	public partial class SkiaControl
	{
		#region INVALIDATION

		/// <summary>
		/// Soft invalidation, without requiring update. So next time we try to draw this one it will recalc everything.
		/// </summary>
		public virtual void InvalidateInternal()
		{
			InvalidateViewsList();
			IsLayoutDirty = true;
			NeedMeasure = true;
			InvalidateCacheWithPrevious();
		}

		/// <summary>
		/// Base calls InvalidateInternal and InvalidateParent
		/// </summary>
		public virtual void Invalidate()
		{
			InvalidateInternal();

			InvalidateParent();
		}

		public virtual bool ShouldInvalidateByChildren
		{
			get
			{
				return NeedAutoSize;
			}
		}

		public bool IsParentIndependent { get; set; }

		public void InvalidateParents()
		{
			if (IsParentIndependent)
				return;

			if (Parent != null)
			{
				if (Parent.ShouldInvalidateByChildren)
					Parent.Invalidate();
				Parent.InvalidateParents();
			}
		}

		public bool InvalidatedParent;
		private bool _invalidatedParentPostponed;

		public virtual void InvalidateParent()
		{
			if (IsParentIndependent)
				return;

			if (InvalidatedParent)
			{
				if (IsRendering)
					_invalidatedParentPostponed = true;
				else
				{
					Superview?.SetChildAsDirty(this);
				}
				return;
			}

			InvalidatedParent = true;

			var parent = Parent;
			if (parent != null)
			{
				if (parent is SkiaControl skia)
				{

					if (skia.IgnoreChildrenInvalidations && skia.UsingCacheType == SkiaCacheType.None)
					{
						return;
					}

					if (skia.ShouldInvalidateByChildren || skia.UsingCacheType != SkiaCacheType.None)
					{
						parent.InvalidateByChild(this);
					}
					else
					{
						parent.Update();
					}
				}
				else
				{
					parent.InvalidateByChild(this);
				}

			}
		}

		/// <summary>
		/// To be able to fast track dirty children
		/// </summary>
		/// <param name="child"></param>
		public virtual void InvalidateByChild(SkiaControl child)
		{
			DirtyChildren.Add(child);

			Invalidate();
		}

		/// <summary>
		/// Indicated that wants to be re-measured without invalidating cache
		/// </summary>
		public virtual void InvalidateViewport()
		{
			if (IsParentIndependent)
				return;

			if (!IsDisposed)
			{
				NeedMeasure = true;
				NeedMeasure = true;
				IsLayoutDirty = true; //force recalc of DrawingRect
				Parent?.InvalidateViewport();
			}
		}

		protected readonly ControlsTracker DirtyChildren = new();

		protected HashSet<SkiaControl> DirtyChildrenInternal { get; set; } = new();

		public virtual void UpdateByChild(SkiaControl control)
		{
			DirtyChildren.Add(control);

			UpdateInternal();
		}

		#endregion




	}
}
