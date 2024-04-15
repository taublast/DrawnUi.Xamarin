﻿namespace DrawnUi.Maui.Draw;

/// <summary>
/// Used by the canvas, do not need this for drawn controls
/// </summary>
public enum GesturesMode
{
    /// <summary>
    /// Default
    /// </summary>
    Disabled,

    /// <summary>
    /// Gestures attached
    /// </summary>
    Enabled,

    /// <summary>
    /// Lock input for self, useful inside scroll view, panning controls like slider etc
    /// </summary>
    Lock,

    /// <summary>
    /// Tries to let other views consume the touch event if this view doesn't handle it
    /// </summary>
    Share,
}