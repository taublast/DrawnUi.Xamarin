﻿using AppoMobi.Specials;
using DrawnUi.Maui.Infrastructure.Extensions;
using ExCSS;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Xamarin.Essentials;
using Animation = SkiaSharp.Skottie.Animation;
using Color = Xamarin.Forms.Color;

namespace DrawnUi.Maui.Controls;

public class SkiaLottie : AnimatedFramesRenderer
{
    /// <summary>
    ///     To avoid reloading same files multiple times..
    /// </summary>
    public static ConcurrentDictionary<string, string> CachedAnimations = new();

    public override void Stop()
    {
        base.Stop();

        SeekToDefaultFrame();
    }

    protected override void OnFinished()
    {
        base.OnFinished();

        SeekToDefaultFrame();
    }

    public virtual void SeekToDefaultFrame()
    {
        if (Animator != null)
        {
            if (IsOn)
                Seek(DefaultFrameWhenOn);
            else
                Seek(DefaultFrame);
        }
    }


    protected override void OnAnimatorSeeking(double frame)
    {
        if (LottieAnimation != null)
        {
            if (frame < 0)
            {
                frame = LottieAnimation.OutPoint;
            }
            LottieAnimation.SeekFrame(frame);
        }

        base.OnAnimatorSeeking(frame);
    }

    private static void ApplyDefaultFrameWhenOnProperty(BindableObject bindable, object oldvalue, object newvalue)
    {
        if (bindable is SkiaLottie control && !control.IsPlaying)
        {
            if (control.Animator != null && control.IsOn)
                control.Seek((int)newvalue);
        }
    }

    private static void ApplyDefaultFrameProperty(BindableObject bindable, object oldvalue, object newvalue)
    {
        if (bindable is SkiaLottie control && !control.IsPlaying)
        {
            if (control.Animator != null && !control.IsOn)
                control.Seek((int)newvalue);
        }
    }

    private static void ApplyIsOnProperty(BindableObject bindable, object oldvalue, object newvalue)
    {
        if (bindable is SkiaLottie control)
        {
            if (control.Animator != null)
            {
                var value = (bool)newvalue;
                if (control.IsPlaying || !value)
                    control.Stop();
                else
                {
                    if (control.ApplyIsOnWhenNotPlaying)
                        control.SeekToDefaultFrame();
                }
            }
        }
    }

    public static readonly BindableProperty ApplyIsOnWhenNotPlayingProperty = BindableProperty.Create(nameof(ApplyIsOnWhenNotPlaying),
    typeof(bool),
    typeof(SkiaLottie),
    true);
    public bool ApplyIsOnWhenNotPlaying
    {
        get { return (bool)GetValue(ApplyIsOnWhenNotPlayingProperty); }
        set { SetValue(ApplyIsOnWhenNotPlayingProperty, value); }
    }


    public static readonly BindableProperty DefaultFrameProperty = BindableProperty.Create(nameof(DefaultFrame),
        typeof(int),
        typeof(SkiaLottie),
        0, propertyChanged: ApplyDefaultFrameProperty
    );
    /// <summary>
    /// For the case IsOn = False. What frame should we display at start or when stopped. 0 (START) is default, can specify other number. if value is less than 0 then will seek to the last available frame (END).
    /// </summary>
    public int DefaultFrame
    {
        get => (int)GetValue(DefaultFrameProperty);
        set => SetValue(DefaultFrameProperty, value);
    }

    public static readonly BindableProperty IsOnProperty = BindableProperty.Create(nameof(IsOn),
    typeof(bool),
    typeof(SkiaLottie),
    false, propertyChanged: ApplyIsOnProperty);
    public bool IsOn
    {
        get { return (bool)GetValue(IsOnProperty); }
        set { SetValue(IsOnProperty, value); }
    }

    public static readonly BindableProperty DefaultFrameWhenOnProperty = BindableProperty.Create(nameof(DefaultFrameWhenOn),
        typeof(int),
        typeof(SkiaLottie),
        0, propertyChanged: ApplyDefaultFrameWhenOnProperty
    );
    /// <summary>
    /// For the case IsOn = True. What frame should we display at start or when stopped. 0 (START) is default, can specify other number. if value is less than 0 then will seek to the last available frame (END).
    /// </summary>
    public int DefaultFrameWhenOn
    {
        get => (int)GetValue(DefaultFrameWhenOnProperty);
        set => SetValue(DefaultFrameWhenOnProperty, value);
    }

    public static readonly BindableProperty SpeedRatioProperty = BindableProperty.Create(nameof(SpeedRatio),
        typeof(double),
        typeof(SkiaLottie),
        1.0, propertyChanged: (b, o, n) =>
        {
            if (b is SkiaLottie control) control.ApplySpeed();
        });

    public static readonly BindableProperty ColorTintProperty = BindableProperty.Create(
        nameof(ColorTint),
        typeof(Color),
        typeof(SkiaLottie),
        Color.Transparent, propertyChanged: ApplySourceProperty);

    public static readonly BindableProperty SourceProperty = BindableProperty.Create(nameof(Source),
        typeof(string),
        typeof(SkiaLottie),
        string.Empty,
        propertyChanged: ApplySourceProperty);

    private readonly object _lockSource = new();
    private Animation _lottieAnimation;

    private readonly SemaphoreSlim _semaphoreLoadFile = new(1, 1);

    public Animation LottieAnimation
    {
        get => _lottieAnimation;

        set
        {
            if (_lottieAnimation != value)
            {
                _lottieAnimation = value;
                OnPropertyChanged();
            }
        }
    }

    public double SpeedRatio
    {
        get => (double)GetValue(SpeedRatioProperty);
        set => SetValue(SpeedRatioProperty, value);
    }

    public Color ColorTint
    {
        get => (Color)GetValue(ColorTintProperty);
        set => SetValue(ColorTintProperty, value);
    }

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    protected override void RenderFrame(SkiaDrawingContext ctx, SKRect destination, float scale,
        object arguments)
    {
        if (IsDisposed)
            return;

        if (LottieAnimation is Animation lottie)
            lock (_lockSource)
            {
                lottie.Render(ctx.Canvas, DrawingRect);
            }
    }

    /// <summary>
    ///     This is not replacing current animation, use SetAnimation for that.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns>
    public async Task<Animation> LoadSource(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        await _semaphoreLoadFile.WaitAsync();

        try
        {
            string json = null;
            if (CachedAnimations.TryGetValue(fileName, out json))
            {
                Debug.WriteLine($"Loaded {fileName} from cache");
            }
            else
            {
                if (Uri.TryCreate(fileName, UriKind.Absolute, out var uri) && uri.Scheme != "file")
                {
                    var client = new WebClient();
                    var data = await client.DownloadDataTaskAsync(uri);
                    json = Encoding.UTF8.GetString(data);
                }
                else
                {
                    if (fileName.SafeContainsInLower(SkiaImageManager.NativeFilePrefix))
                    {
                        var fullFilename = fileName.Replace(SkiaImageManager.NativeFilePrefix, "");
                        using var stream = new FileStream(fullFilename, FileMode.Open);
                        using var reader = new StreamReader(stream);
                        json = await reader.ReadToEndAsync();
                    }
                    else
                    {

                        //using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);

                        var assembly = Super.AppAssembly;
                        if (assembly == null)
                        {
                            var callingAssemblyMethod = typeof(Assembly).GetTypeInfo().GetDeclaredMethod("GetCallingAssembly");
                            assembly = (Assembly)callingAssemblyMethod.Invoke(null, new object[0]);
                        }
                        var fullPath = $"{assembly.GetName().Name}.{fileName.Replace(@"\", ".").Replace(@"/", ".")}";
                        using var stream = assembly.GetManifestResourceStream(fullPath);

                        using var reader = new StreamReader(stream);
                        json = await reader.ReadToEndAsync();
                    }

                    CachedAnimations.TryAdd(fileName, json);
                }
            }

            if (ColorTint != Color.Transparent) json = ApplyTint(json, ColorTint);

            if (ProcessJson != null) json = ProcessJson(json);

            return await LoadAnimationFromJson(OnJsonLoaded(json));
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[SkiaLottie] LoadSource failed to load animation {fileName}");
            Trace.WriteLine(e);
            return null;
        }
        finally
        {
            _semaphoreLoadFile.Release();
        }
    }

    public static Stream StreamFromResourceUrl(string url, Assembly assembly = null)
    {
        var uri = new Uri(url);

        var parts = uri.OriginalString.Substring(11).Split('?');
        var resourceName = parts.First();

        if (parts.Count() > 1)
        {
            var name = Uri.UnescapeDataString(uri.Query.Substring(10));
            var assemblyName = new AssemblyName(name);
            assembly = Assembly.Load(assemblyName);
        }

        if (assembly == null)
        {
            var callingAssemblyMethod = typeof(Assembly).GetTypeInfo().GetDeclaredMethod("GetCallingAssembly");
            assembly = (Assembly)callingAssemblyMethod.Invoke(null, new object[0]);
        }

        var fullPath = $"{assembly.GetName().Name}.{resourceName}";

        Console.WriteLine($"[StreamFromResourceUrl] loading {fullPath}..");

        return assembly.GetManifestResourceStream(fullPath);
    }

    /// <summary>
    ///     Called by LoadAnimationFromResources after file was loaded so we can modify json if needed before it it consumed.
    ///     Return json to be used.
    ///     This is not called by LoadAnimationFromJson.
    /// </summary>
    protected virtual string OnJsonLoaded(string json)
    {
        return json;
    }

    /// <summary>
    ///     This is not replacing current animation, use SetAnimation for that.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public async Task<Animation> LoadAnimationFromJson(string json)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            var data = SKData.CreateCopy(bytes);
            if (Animation.TryCreate(data, out var animation))
                return animation;
            throw new Exception("SkiaSharp.Skottie.Animation.TryParse failed.");
        }
        catch (Exception e)
        {
            Trace.WriteLine("[SkiaLottie] failed to load animation from json");
            Trace.WriteLine(e);
        }

        return null;
    }

    public void SetAnimation(Animation animation, bool disposePrevious)
    {
        lock (_lockSource)
        {
            if (animation == null || animation == LottieAnimation || IsDisposed) return;

            var wasPlaying = IsPlaying;
            Animation kill = null;

            if (wasPlaying)
            {
                kill = LottieAnimation;
                Stop();
            }

            LottieAnimation = animation;

            if (IsOn)
            {
                OnAnimatorSeeking(DefaultFrameWhenOn);
            }
            else
            {
                OnAnimatorSeeking(DefaultFrame);
            }

            //Debug.WriteLine($"[SkiaLottie] Loaded animation: Version:{Animation.Version} Duration:{Animation.Duration} Fps:{Animation.Fps} InPoint:{Animation.InPoint} OutPoint:{Animation.OutPoint}");

            InitializeAnimator(); //autoplay applied inside

            if (wasPlaying && !IsPlaying) Start();

            if (kill != null && disposePrevious)
                Tasks.StartDelayed(TimeSpan.FromSeconds(2), () => { kill.Dispose(); });
        }
    }


    private void Free(Animation skottieAnimation)
    {
        skottieAnimation.Dispose();
    }

    public override void OnDisposing()
    {
        lock (_lockSource)
        {
            base.OnDisposing();

            if (LottieAnimation != null)
            {
                Free(LottieAnimation);
                LottieAnimation = null;
            }
        }
    }

    protected override void OnAnimatorUpdated(double value)
    {
        base.OnAnimatorUpdated(value);

        if (LottieAnimation != null)
        {
            LottieAnimation.SeekFrame(value);
            Update();
        }
    }

    protected virtual void ApplySpeed()
    {
        if (LottieAnimation == null)
            return;

        var speed = 1.0;
        if (SpeedRatio < 1)
            speed = LottieAnimation.Duration.TotalMilliseconds * (1 + SpeedRatio);
        else
            speed = LottieAnimation.Duration.TotalMilliseconds / SpeedRatio;
        Animator.Speed = speed;
    }

    protected override void OnAnimatorInitializing()
    {
        if (LottieAnimation != null)
        {
            ApplySpeed();

            Animator.mValue = LottieAnimation.InPoint;
            Animator.mMinValue = LottieAnimation.InPoint;
            Animator.mMaxValue = LottieAnimation.OutPoint;
            Animator.Distance = Animator.mMaxValue - Animator.mMinValue;
        }
    }

    protected override bool CheckCanStartAnimator()
    {
        return LottieAnimation != null;
    }

    protected override void OnAnimatorStarting()
    {
        Animator.SetValue(LottieAnimation.InPoint);
    }

    public virtual void GoToStart()
    {
        if (LottieAnimation != null)
        {
            LottieAnimation.Seek(0.0);
            Update();
        }
    }

    public virtual void GoToEnd()
    {
        if (LottieAnimation != null)
        {
            LottieAnimation.Seek(100.0);
            Update();
        }
    }


    private static void ApplySourceProperty(BindableObject bindable, object oldvalue, object newvalue)
    {
        if (bindable is SkiaLottie control)
            Tasks.StartDelayedAsync(TimeSpan.FromMilliseconds(1), async () =>
            {
                var animation = await control.LoadSource(control.Source);
                if (animation != null) control.SetAnimation(animation, true);
            });
    }


    #region PROCESSING

    public Func<string, string> ProcessJson;

    public static string ApplyTint(string json, Color tint)
    {
        var lottieJson = JObject.Parse(json);
        Dictionary<JToken, Color> colorMap = new();
        Dictionary<Color, int> colorFrequencyMap = new();
        Dictionary<JToken, float> strokeWidthMap = new();

        FindProperties(lottieJson, colorMap, colorFrequencyMap, strokeWidthMap);

        // Determine the main color based on frequency

        var mainColor = Color.Transparent;
        if (colorFrequencyMap.Count > 0)
        {
            var maxFrequency = colorFrequencyMap.Values.Max();
            var mostFrequentColors = colorFrequencyMap.Where(x => x.Value == maxFrequency).Select(x => x.Key).ToList();
            if (mostFrequentColors.Count == 1)
                mainColor = mostFrequentColors.First();
            else
                // Use saturation as a tie-breaker
                mainColor = mostFrequentColors.Aggregate((maxColor, nextColor) =>
                    CalculateSaturation(maxColor) > CalculateSaturation(nextColor) ? maxColor : nextColor);
        }

        Dictionary<JToken, string> modifiedColorMap = new();

        foreach (var entry in colorMap)
            if (mainColor != Color.Transparent)
            {
                if (entry.Value == mainColor)
                {
                    var alpha = entry.Value.A;
                    modifiedColorMap[entry.Key] = ColorToString(tint.WithAlpha(alpha));
                }
            }
            else
            {
                var alpha = entry.Value.A;
                modifiedColorMap[entry.Key] = ColorToString(tint.WithAlpha(alpha));
            }

        foreach (var entry in modifiedColorMap) entry.Key.Replace(JToken.Parse(entry.Value));

        return lottieJson.ToString();
    }

    public static void FindProperties(JToken token, Dictionary<JToken, Color> colorMap,
        Dictionary<Color, int> colorFrequencyMap, Dictionary<JToken, float> strokeWidthMap)
    {
        if (token is JObject obj)
            foreach (var property in obj.Properties())
            {
                if (property.Name == "k")
                    if (IsColorArray(property.Value))
                    {
                        var color = StringToXamarinColor(property.Value.ToString());
                        colorMap[property.Value] = color;

                        if (colorFrequencyMap.ContainsKey(color))
                            colorFrequencyMap[color]++;
                        else
                            colorFrequencyMap[color] = 1;
                    }

                FindProperties(property.Value, colorMap, colorFrequencyMap, strokeWidthMap);
            }
    }


    public static Color StringToXamarinColor(string colorString)
    {
        // Remove whitespace and square brackets from the string
        colorString = colorString.Replace(" ", "").Replace("[", "").Replace("]", "");

        // Split the string into individual color components
        var components = colorString.Split(',');

        // Parse the color components as floats
        var r = float.Parse(components[0], CultureInfo.InvariantCulture);
        var g = float.Parse(components[1], CultureInfo.InvariantCulture);
        var b = float.Parse(components[2], CultureInfo.InvariantCulture);
        var a = float.Parse(components[3], CultureInfo.InvariantCulture);

        // Create and return a Xamarin.Forms Color object
        return new Color(r, g, b, a);
    }

    public static string ColorToString(Color color)
    {
        // Extract the color components as floats
        var r = color.R;
        var g = color.G;
        var b = color.B;
        var a = color.A;

        // Format the string color representation
        var colorString =
            $"[\n  {r.ToString(CultureInfo.InvariantCulture)},\n  {g.ToString(CultureInfo.InvariantCulture)},\n  {b.ToString(CultureInfo.InvariantCulture)},\n  {a.ToString(CultureInfo.InvariantCulture)}\n]";

        return colorString;
    }


    public static float CalculateSaturation(Color color)
    {
        var max = Math.Max(Math.Max(color.R, color.G), color.B);
        var min = Math.Min(Math.Min(color.R, color.G), color.B);
        var delta = max - min;
        var sum = max + min;

        if (Math.Abs(delta) < 0.001) return 0;

        var saturation = sum <= 1 ? delta / sum : delta / (2 - sum);
        return (float)saturation;
    }

    private static bool IsColorArray(JToken token)
    {
        if (token is JArray arr && arr.Count == 4)
        {
            foreach (var item in arr)
                if (item.Type != JTokenType.Float && item.Type != JTokenType.Integer)
                    return false;
            return true;
        }

        return false;
    }

    private static bool IsStrokeWidthProperty(JObject parent, JProperty property)
    {
        if (parent.TryGetValue("ty", out var type) && type.Value<string>() == "st") return true;
        return false;
    }

    #endregion
}