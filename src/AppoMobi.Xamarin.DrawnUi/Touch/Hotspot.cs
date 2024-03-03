
using AppoMobi.Forms.Gestures;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Windows.Input;


namespace AppoMobi.Framework.Forms.UI.Touch;

public class Hotspot : ContentView, ITouchView
{
    public event EventHandler<TouchActionEventArgs> Tapped;
    public event EventHandler<TouchActionEventArgs> Up;
    public event EventHandler<TouchActionEventArgs> Down;
    public event EventHandler<TouchActionEventArgs> Panned;
    public event EventHandler<TouchActionEventArgs> Panning;
    public event EventHandler<TouchActionEventArgs> Swiped;

    public virtual void OnUp(TouchActionEventArgs args)
    {
        Up?.Invoke(this, args);
        CommandUp?.Execute(args);
    }

    public virtual void OnDown(TouchActionEventArgs args)
    {
        Down?.Invoke(this, args);
        CommandDown?.Execute(args);
    }

    protected void SyncTouchMode(TouchHandlingStyle mode)
    {
        if (_touchHandler != null)
        {
            _touchHandler.TouchMode = mode;
            //            _touchHandler.NativeCommand
        }
    }


    //-------------------------------------------------------------
    // IsDraggable
    //-------------------------------------------------------------
    private const string nameIsDraggable = "IsDraggable";
    public static readonly BindableProperty IsDraggableProperty = BindableProperty.Create(nameIsDraggable, typeof(bool), typeof(Hotspot), false);
    public bool IsDraggable
    {
        get { return (bool)GetValue(IsDraggableProperty); }
        set { SetValue(IsDraggableProperty, value); }
    }

    //-------------------------------------------------------------
    // TouchMode
    //-------------------------------------------------------------
    private const string nameTouchMode = "TouchMode";

    public static readonly BindableProperty TouchModeProperty = BindableProperty.Create(nameTouchMode,
        typeof(TouchHandlingStyle), typeof(Hotspot),
        TouchHandlingStyle.Default);

    public TouchHandlingStyle TouchMode
    {
        get { return (TouchHandlingStyle)GetValue(TouchModeProperty); }
        set { SetValue(TouchModeProperty, value); }
    }

    //-------------------------------------------------------------
    // CommandLongPressingParameter
    //-------------------------------------------------------------
    private const string nameCommandLongPressingParameter = "CommandLongPressingParameter";

    public static readonly BindableProperty CommandLongPressingParameterProperty = BindableProperty.Create(
        nameCommandLongPressingParameter, typeof(object), typeof(ITouchView),
        null);
    public object CommandLongPressingParameter
    {
        get { return GetValue(CommandLongPressingParameterProperty); }
        set { SetValue(CommandLongPressingParameterProperty, value); }
    }

    //-------------------------------------------------------------
    // CommandTapped
    //-------------------------------------------------------------
    private const string nameCommandTapped = "CommandTapped";
    public static readonly BindableProperty CommandTappedProperty = BindableProperty.Create(nameCommandTapped, typeof(ICommand), typeof(ITouchView),
        null);
    public ICommand CommandTapped
    {
        get { return (ICommand)GetValue(CommandTappedProperty); }
        set { SetValue(CommandTappedProperty, value); }
    }

    //-------------------------------------------------------------
    // CommandSwiped
    //-------------------------------------------------------------
    private const string nameCommandSwiped = "CommandSwiped";
    public static readonly BindableProperty CommandSwipedProperty = BindableProperty.Create(nameCommandSwiped, typeof(ICommand), typeof(ITouchView),
        null);
    public ICommand CommandSwiped
    {
        get { return (ICommand)GetValue(CommandSwipedProperty); }
        set { SetValue(CommandSwipedProperty, value); }
    }

    //-------------------------------------------------------------
    // CommandUp
    //-------------------------------------------------------------
    private const string nameCommandUp = "CommandUp";
    public static readonly BindableProperty CommandUpProperty = BindableProperty.Create(nameCommandUp, typeof(ICommand), typeof(ITouchView),
        null);
    public ICommand CommandUp
    {
        get { return (ICommand)GetValue(CommandUpProperty); }
        set { SetValue(CommandUpProperty, value); }
    }

    //-------------------------------------------------------------
    // CommandDown
    //-------------------------------------------------------------
    private const string nameCommandDown = "CommandDown";
    public static readonly BindableProperty CommandDownProperty = BindableProperty.Create(nameCommandDown, typeof(ICommand), typeof(ITouchView),
        null);
    public ICommand CommandDown
    {
        get { return (ICommand)GetValue(CommandDownProperty); }
        set { SetValue(CommandDownProperty, value); }
    }

    //-------------------------------------------------------------
    // CommandTappedParameter
    //-------------------------------------------------------------
    private const string nameCommandTappedParameter = "CommandTappedParameter";
    public static readonly BindableProperty CommandTappedParameterProperty = BindableProperty.Create(nameCommandTappedParameter, typeof(object), typeof(ITouchView),
        null);
    public object CommandTappedParameter
    {
        get { return GetValue(CommandTappedParameterProperty); }
        set { SetValue(CommandTappedParameterProperty, value); }
    }

    //-------------------------------------------------------------
    // CommandLongPressing
    //-------------------------------------------------------------
    private const string nameCommandLongPressing = "CommandLongPressing";
    public static readonly BindableProperty CommandLongPressingProperty = BindableProperty.Create(nameCommandLongPressing, typeof(ICommand), typeof(ITouchView),
        null);
    public ICommand CommandLongPressing
    {
        get { return (ICommand)GetValue(CommandLongPressingProperty); }
        set { SetValue(CommandLongPressingProperty, value); }
    }

    public Hotspot()
    {
        AttachGestures();
    }


    private bool lockTap;

    private TouchEffect _touchHandler;

    private void OnTapped(object sender, TouchActionEventArgs args)
    {
        if (InputTransparent)
            return;

        if (lockTap && TouchEffect.LockTimeTimeMsDefault > 0)
            return;

        lockTap = true;

        //invoke action
        Tapped?.Invoke(this, args);
        if (CommandTapped != null)
        {
            Debug.WriteLine($"[HOTSPOT] Executing tap command..");
            CommandTapped.Execute(CommandTappedParameter);
        }

        if (TouchEffect.LockTimeTimeMsDefault > 0)
        {
            Device.StartTimer(TimeSpan.FromMilliseconds(TouchEffect.LockTimeTimeMsDefault), () =>
            {
                lockTap = false;
                return false;
            });
        }
        else
        {
            lockTap = false;
        }
    }

    //private void OnUp(object sender, TouchActionEventArgs args)
    //{
    //    //Debug.WriteLine($"[TOUCH] UP");

    //    TouchDown = false;
    //}

    //private void OnDown(object sender, TouchActionEventArgs args)
    //{
    //    //Debug.WriteLine($"[TOUCH] DOWN");
    //    TouchDown = true;
    //}

    private void OnTouch(object sender, TouchActionEventArgs args)
    {
        Debug.WriteLine($"[TOUCH] {args.Type} {JsonConvert.SerializeObject(args)}");
    }

    public bool HasGestures
    {
        get
        {
            return _touchHandler != null;
        }
    }

    void AttachGestures()
    {
        if (HasGestures)
            return;

        _touchHandler = new TouchEffect
        {
            Capture = true
        };
        _touchHandler.LongPressing += OnLongPressing;
        _touchHandler.Tapped += OnTapped;
        _touchHandler.Down += OnDown1;
        _touchHandler.Up += OnUp1;
        _touchHandler.Panning += OnPanning;
        _touchHandler.Panned += OnPanned;
        _touchHandler.Swiped += OnSwiped;

        _touchHandler.TouchMode = this.TouchMode;
        _touchHandler.Draggable = this.IsDraggable;

        this.Effects.Add(_touchHandler);
    }

    public virtual void OnSwiped(object sender, TouchActionEventArgs args)
    {
        Swiped?.Invoke(this, args);
        CommandSwiped?.Execute(args);
    }

    public virtual void OnPanned(object sender, TouchActionEventArgs args)
    {
        Panned?.Invoke(this, args);
    }

    public virtual void OnPanning(object sender, TouchActionEventArgs args)
    {
        Panning?.Invoke(this, args);
    }

    protected void OnUp1(object sender, TouchActionEventArgs args)
    {
        if (TransformView != null)
        {
            if (animating)
            {
                if (Reaction == HotspotReaction.Minify)
                {
                    TransformView.Scale = _savedScale;
                }
                else
                if (Reaction == HotspotReaction.Zoom)
                {
                    TransformView.Scale = _savedScale;
                }
                animating = false;
            }

            TransformView.Opacity = _savedOpacity;
            TransformView.BackgroundColor = _savedColor;
        }

        TouchDown = false;

        if (!wentOut && !_panning)
        {
            //TappedSmartCommand?.Execute(CommandTappedParameter);
        }

        if (!_panning)
            wentOut = false;

        OnUp(args);
    }

    protected bool disposed;
    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;


        DetachGestures();
    }

    protected void DetachGestures()
    {
        if (!HasGestures)
            return;

        _touchHandler.LongPressing -= OnLongPressing;
        _touchHandler.Tapped -= OnTapped;
        _touchHandler.Down -= OnDown1;
        _touchHandler.Up -= OnUp1;
        _touchHandler.Panning -= OnPanning;
        _touchHandler.Panned -= OnPanned;
        _touchHandler.Swiped -= OnSwiped;

        _touchHandler.Dispose();
    }

    private bool _panning;
    private bool wentOut;

    //private void OnPanned(object sender, PanEventArgs e)
    //{
    //    //        Debug.WriteLine("[TOUCH] OnPanned");


    //    if (e.Center.X < 0 || e.Center.X > Width || e.Center.Y < 0 || e.Center.Y > Height)
    //        wentOut = true;

    //    if (_panning)
    //    {
    //        if (!wentOut)
    //        {
    //            //TappedSmartCommand?.Execute(CommandTappedParameter);
    //        }

    //        _panning = false;
    //        wentOut = false;
    //    }

    //}

    //private void OnPanning(object sender, PanEventArgs e)
    //{
    //    //   Debug.WriteLine("[TOUCH] OnPanning");

    //    _panning = true;
    //}

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
    }

    private bool _isRendererSet;
    protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        //Debug.WriteLine($"[1] {propertyName}");

        base.OnPropertyChanged(propertyName);

        if (propertyName == "Renderer") //TRICK NOT WORKING INSIDE SHELL !!!!
        {
            _isRendererSet = !_isRendererSet;
            if (!_isRendererSet)
            {
                Dispose();
            }
        }
        else
        if (propertyName == nameof(IsDraggable))
        {
            if (_touchHandler != null)
            {
                _touchHandler.Draggable = this.IsDraggable;
            }
        }
        else
        if (propertyName == nameof(TouchMode))
        {
            SyncTouchMode(this.TouchMode);
        }
    }



    private void OnLongPressing(object sender, TouchActionEventArgs args)
    {

        //Debug.WriteLine($"[TOUCH] LongPressing!");

        CommandLongPressing?.Execute(CommandLongPressingParameter);

    }


    //-------------------------------------------------------------
    // Reaction
    //-------------------------------------------------------------
    private const string nameReaction = "Reaction";
    public static readonly BindableProperty ReactionProperty = BindableProperty.Create(nameReaction,
        typeof(HotspotReaction), typeof(Hotspot), HotspotReaction.None);
    public HotspotReaction Reaction
    {
        get { return (HotspotReaction)GetValue(ReactionProperty); }
        set { SetValue(ReactionProperty, value); }
    }


    //-------------------------------------------------------------
    // TintColor
    //-------------------------------------------------------------
    private const string nameTintColor = "TintColor";
    public static readonly BindableProperty TintColorProperty = BindableProperty.Create(nameTintColor, typeof(Color), typeof(Hotspot), Color.Transparent); //, BindingMode.TwoWay
    public Color TintColor
    {
        get { return (Color)GetValue(TintColorProperty); }
        set { SetValue(TintColorProperty, value); }
    }

    //-------------------------------------------------------------
    // TransformView
    //-------------------------------------------------------------
    private const string nameTransformView = "TransformView";
    public static readonly BindableProperty TransformViewProperty = BindableProperty.Create(nameTransformView, typeof(View), typeof(Hotspot), null); //, BindingMode.TwoWay
    public View TransformView
    {
        get { return (View)GetValue(TransformViewProperty); }
        set { SetValue(TransformViewProperty, value); }
    }


    //-------------------------------------------------------------
    // DownOpacity
    //-------------------------------------------------------------
    private const string nameDownOpacity = "DownOpacity";
    public static readonly BindableProperty DownOpacityProperty = BindableProperty.Create(nameDownOpacity, typeof(double), typeof(Hotspot), 0.75); //, BindingMode.TwoWay

    private double _savedOpacity;
    private double _savedScale;

    public double DownOpacity
    {
        get { return (double)GetValue(DownOpacityProperty); }
        set { SetValue(DownOpacityProperty, value); }
    }

    protected Color _savedColor;

    private bool _TouchDown;
    public bool TouchDown
    {
        get { return _TouchDown; }
        set
        {
            if (_TouchDown != value)
            {
                _TouchDown = value;
                OnPropertyChanged();
            }
        }
    }

    private bool animating;

    private void OnDown1(object sender, TouchActionEventArgs args)
    {

        if (TransformView != null)
        {
            if (!animating)
            {
                animating = true;

                if (_savedOpacity != DownOpacity)
                    _savedOpacity = TransformView.Opacity;

                _savedScale = TransformView.Scale;

                if (_savedColor != TintColor)
                    _savedColor = TransformView.BackgroundColor;

                if (Reaction == HotspotReaction.Tint)
                {
                    if (TintColor != Color.Transparent)
                    {
                        TransformView.BackgroundColor = TintColor;
                    }
                }
                else
                if (Reaction == HotspotReaction.Minify)
                {
                    TransformView.Opacity = DownOpacity;
                    TransformView.Scale = 0.985;
                    if (TintColor != Color.Transparent)
                    {
                        TransformView.BackgroundColor = TintColor;
                    }
                }
                else
                if (Reaction == HotspotReaction.Zoom)
                {
                    TransformView.Scale = 1.1;
                    if (TintColor != Color.Transparent)
                    {
                        TransformView.BackgroundColor = TintColor;
                    }
                }
            }
        }

        TouchDown = true;

        OnDown(args);
    }
}