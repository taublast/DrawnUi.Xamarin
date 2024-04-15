﻿namespace DrawnUi.Maui.Controls;

/// <summary>
/// This cell can watch binding context property changing
/// </summary>
public class SkiaDynamicDrawnCell : SkiaDrawnCell
{
	protected override void FreeContext()
	{
		if (_lastContext != null)
		{
			_lastContext.PropertyChanged -= ContextPropertyChanged;
		}
		base.FreeContext();
	}

	protected override void AttachContext()
	{
		base.AttachContext();

		if (_lastContext != null)
			_lastContext.PropertyChanged += ContextPropertyChanged;
	}

	protected virtual void ContextPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		//if (e.PropertyName == nameof(OptionItem.Selected) || e.PropertyName == nameof(OptionItem.Title))
		//{
		//	SetContent();
		//}
	}
}