﻿using System.Linq;

namespace DrawnUi.Maui.Draw;

public class ControlsTracker
{
	protected readonly Dictionary<Guid, SkiaControl> _dic = new();
	protected List<SkiaControl> Sorted { get; set; }
	protected bool _isDirty = true;

	public void Clear()
	{
		lock (_dic)
		{
			_dic.Clear();
			_isDirty = true;
		}
	}

	public List<SkiaControl> GetList()
	{
		lock (_dic)
		{
			if (_isDirty)
			{
				Sorted = _dic.Values
					.ToList();
				_isDirty = false;
			}
			return Sorted;
		}
	}

	public void Add(SkiaControl item)
	{
		lock (_dic)
		{
			_dic[item.Uid] = item;
			_isDirty = true;
		}
	}

	public void Remove(SkiaControl item)
	{
		lock (_dic)
		{
			if (_dic.Remove(item.Uid))
			{
				_isDirty = true;
			}
		}
	}

	public void Invalidate()
	{
		lock (_dic)
		{
			_isDirty = true;
		}
	}
}