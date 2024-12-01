﻿using DrawnUi.Maui.Infrastructure.Models;
using DrawnUi.Maui.Infrastructure.Xaml;
using System.Collections.Concurrent;
using System.IO;
using Xamarin.Essentials;

namespace DrawnUi.Maui.Draw;

public interface IHasBanner
{
    /// <summary>
    /// Main image
    /// </summary>
    public string Banner { get; set; }

    /// <summary>
    /// Indicates that it's already preloading
    /// </summary>
    public bool BannerPreloadOrdered { get; set; }
}

public enum LoadPriority
{
    Low,
    Normal,
    High
}

public partial class SkiaImageManager : IDisposable
{

    #region HELPERS

    public virtual async Task PreloadImage(ImageSource source, CancellationTokenSource cancel = default)
    {
        try
        {
            if (cancel == null)
            {
                cancel = new();
            }

            if (source != null && !cancel.IsCancellationRequested)
            {
                await Preload(source, cancel);
            }
        }
        catch (Exception e)
        {
            Super.Log(e);
        }
    }

    public virtual async Task PreloadImage(string source, CancellationTokenSource cancel = default)
    {
        try
        {
            if (cancel == null)
            {
                cancel = new();
            }

            if (!string.IsNullOrEmpty(source) && !cancel.IsCancellationRequested)
            {
                await Preload(FrameworkImageSourceConverter.FromInvariantString(source), cancel);
            }
        }
        catch (Exception e)
        {
            Super.Log(e);
        }
    }

    public virtual async Task PreloadImages(IEnumerable<string> list, CancellationTokenSource cancel = default)
    {
        try
        {
            if (cancel == null)
            {
                cancel = new();
            }

            if (list != null && !cancel.IsCancellationRequested)
            {
                var tasks = new List<Task>();
                foreach (var source in list)
                {
                    if (!cancel.IsCancellationRequested)
                    {
                        tasks.Add(Preload(source, cancel));
                    }
                }

                // Await all the preload tasks at once.
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel.Token);

                if (tasks.Count > 0)
                {

                    var cancellationCompletionSource = new TaskCompletionSource<bool>();
                    cts.Token.Register(() => cancellationCompletionSource.TrySetResult(true));

                    var whenAnyTask = Task.WhenAny(Task.WhenAll(tasks), cancellationCompletionSource.Task);

                    await whenAnyTask;

                    cts.Token.ThrowIfCancellationRequested();
                }
            }
        }
        catch (Exception e)
        {
            Super.Log(e);
        }

    }

    public virtual async Task PreloadBanners<T>(IList<T> list, CancellationTokenSource cancel = default) where T : IHasBanner
    {
        try
        {
            if (cancel == null)
            {
                cancel = new();
            }

            if (list.Count > 0 && !cancel.IsCancellationRequested)
            {
                var tasks = new List<Task>();
                foreach (var item in list)
                {
                    if (!cancel.IsCancellationRequested && !item.BannerPreloadOrdered)
                    {
                        item.BannerPreloadOrdered = true;
                        // Add the task to the list without awaiting it immediately.
                        tasks.Add(Preload(item.Banner, cancel));
                    }
                }

                //await Task.WhenAll(tasks);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel.Token);

                if (tasks.Count > 0)
                {

                    var cancellationCompletionSource = new TaskCompletionSource<bool>();
                    cts.Token.Register(() => cancellationCompletionSource.TrySetResult(true));

                    var whenAnyTask = Task.WhenAny(Task.WhenAll(tasks), cancellationCompletionSource.Task);

                    await whenAnyTask;

                    cts.Token.ThrowIfCancellationRequested();
                }
            }
        }
        catch (Exception e)
        {
            Super.Log(e);
        }

    }

    #endregion

    /// <summary>
    /// If set to true will not return clones for same sources, but will just return the existing cached SKBitmap reference. Useful if you have a lot on images reusing same sources, but you have to be carefull not to dispose the shared image. SkiaImage is aware of this setting and will keep a cached SKBitmap from being disposed.
    /// </summary>
    public static bool ReuseBitmaps = false;

    /// <summary>
    /// Caching provider setting
    /// </summary>
    public static int CacheLongevitySecs = 1800; //30mins

    /// <summary>
    /// Convention for local files saved in native platform. Shared resources from Resources/Raw/ do not need this prefix.
    /// </summary>
    public static string NativeFilePrefix = "file://";

    public static string ResFilePrefix = "resources://";

    public event EventHandler CanReload;

    private readonly IEasyCachingProvider _cachingProvider;

    public static bool LogEnabled = false;

    public static void TraceLog(string message)
    {
        if (LogEnabled)
        {
#if WINDOWS
            Trace.WriteLine(message);
#else
            Console.WriteLine("*******************************************");
            Console.WriteLine(message);
#endif
        }
    }

    static SkiaImageManager _instance;
    private static int _loadingTasksCount;
    private static int _queuedTasksCount;

    public static SkiaImageManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new SkiaImageManager();

            return _instance;
        }
    }

    public SkiaImageManager()
    {
        _cachingProvider = new SimpleCachingProvider();

        var connected = Connectivity.NetworkAccess;
        if (connected != NetworkAccess.Internet
            && connected != NetworkAccess.ConstrainedInternet)
        {
            IsOffline = true;
        }

        Tasks.StartDelayed(TimeSpan.FromMilliseconds(100), () =>
        {
            LaunchProcessQueue();
        });
    }


    private SemaphoreSlim semaphoreLoad = new(16, 16);

    private readonly object lockPending = new object();

    private readonly object lockObject = new object();

    private bool _isLoadingLocked;
    public bool IsLoadingLocked
    {
        get => _isLoadingLocked;
        set
        {
            if (_isLoadingLocked != value)
            {
                _isLoadingLocked = value;
            }
        }
    }


    public void CancelAll()
    {
        //lock (lockObject)
        {
            while (_queue.Count > 0)
            {
                if (_queue.TryDequeue(out var item, out LoadPriority priority))
                    item.Cancel.Cancel();
            }
        }
    }

    public record QueueItem
    {
        public QueueItem(ImageSource source, CancellationTokenSource cancel, TaskCompletionSource<SKBitmap> task)
        {
            Source = source;
            Cancel = cancel;
            Task = task;
        }

        public ImageSource Source { get; init; }
        public CancellationTokenSource Cancel { get; init; }
        public TaskCompletionSource<SKBitmap> Task { get; init; }
    }

    private readonly SortedDictionary<LoadPriority, Queue<QueueItem>> _priorityQueue = new();

    private readonly PriorityQueue<QueueItem, LoadPriority> _queue = new();

    private readonly ConcurrentDictionary<string, Task<SKBitmap>> _trackLoadingBitmapsUris = new();
    private readonly ConcurrentDictionary<string, ConcurrentStack<QueueItem>> _pendingLoadsLow = new();
    private readonly ConcurrentDictionary<string, ConcurrentStack<QueueItem>> _pendingLoadsNormal = new();
    private readonly ConcurrentDictionary<string, ConcurrentStack<QueueItem>> _pendingLoadsHigh = new();

    private ConcurrentDictionary<string, ConcurrentStack<QueueItem>> GetPendingLoadsDictionary(LoadPriority priority)
    {
        return priority switch
        {
            LoadPriority.Low => _pendingLoadsLow,
            LoadPriority.Normal => _pendingLoadsNormal,
            LoadPriority.High => _pendingLoadsHigh,
            _ => _pendingLoadsNormal,
        };
    }


    /// <summary>
    /// Direct load, without any queue or manager cache, for internal use. Please use LoadImageManagedAsync instead.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public virtual Task<SKBitmap> LoadImageAsync(ImageSource source, CancellationToken token)
    {
        return Super.Native.LoadImageOnPlatformAsync(source, token);
    }

    /// <summary>
    /// Uses queue and manager cache
    /// </summary>
    /// <param name="source"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public virtual Task<SKBitmap> LoadImageManagedAsync(ImageSource source, CancellationTokenSource token, LoadPriority priority = LoadPriority.Normal)
    {

        var tcs = new TaskCompletionSource<SKBitmap>();

        string uri = null;

        if (!source.IsEmpty)
        {
            if (source is UriImageSource sourceUri)
            {
                uri = sourceUri.Uri.ToString();
            }
            else
            if (source is FileImageSource sourceFile)
            {
                uri = sourceFile.File;
            }
            else
            if (source is ImageSourceResourceStream stream)
            {
                uri = stream.Url;
            }

            // 1 Try to get from cache
            var cacheKey = uri;

            var cachedBitmap = _cachingProvider.Get<SKBitmap>(cacheKey);
            if (cachedBitmap.HasValue)
            {
                if (ReuseBitmaps)
                {
                    tcs.TrySetResult(cachedBitmap.Value);
                }
                else
                {
                    tcs.TrySetResult(cachedBitmap.Value.Copy());
                }
                TraceLog($"ImageLoadManager: Returning cached bitmap for UriImageSource {uri}");

                //if (pendingLoads.Any(x => x.Value.Count != 0))
                //{
                //    RunProcessQueue();
                //}

                return tcs.Task;
            }
            TraceLog($"ImageLoadManager: Not found cached UriImageSource {uri}");

            // 2 put to queue
            var tuple = new QueueItem(source, token, tcs);

            if (uri == null)
            {
                //no queue, maybe stream
                TraceLog($"ImageLoadManager: DIRECT ExecuteLoadTask !!!");
                Tasks.StartDelayedAsync(TimeSpan.FromMilliseconds(1), async () =>
                {
                    await ExecuteLoadTask(tuple);
                });
            }
            else
            {
                var urlAlreadyLoading = _trackLoadingBitmapsUris.ContainsKey(uri);
                if (urlAlreadyLoading)
                {
                    // we're currently loading the same image, save the task to pendingLoads
                    TraceLog($"ImageLoadManager: Same image already loading, pausing task for UriImageSource {uri}");

                    var pendingLoads = GetPendingLoadsDictionary(priority);
                    var stack = pendingLoads.GetOrAdd(uri, _ => new ConcurrentStack<QueueItem>());
                    stack.Push(tuple);
                }
                else
                {
                    // We're about to load this image, so add its Task to the loadingBitmaps dictionary
                    _trackLoadingBitmapsUris[uri] = tcs.Task;

                    lock (lockObject)
                    {
                        _queue.Enqueue(tuple, priority);
                    }

                    TraceLog($"ImageLoadManager: Enqueued {uri} (queue {_queue.Count})");
                }

            }



        }

        return tcs.Task;
    }

    void LaunchProcessQueue()
    {
        Task.Run(async () =>
        {
            ProcessQueue();

        }).ConfigureAwait(false);
    }


    private async Task ExecuteLoadTask(QueueItem queueItem)
    {
        if (queueItem != null)
        {
            //do not limit local file loads
            bool useSemaphore = queueItem.Source is UriImageSource;

            try
            {
                if (useSemaphore)
                    await semaphoreLoad.WaitAsync();

                TraceLog($"ImageLoadManager: LoadImageOnPlatformAsync {queueItem.Source}");

                SKBitmap bitmap = await Super.Native.LoadImageOnPlatformAsync(queueItem.Source, queueItem.Cancel.Token);

                // Add the loaded bitmap to the context cache
                if (bitmap != null)
                {
                    if (queueItem.Source is UriImageSource sourceUri)
                    {
                        string uri = sourceUri.Uri.ToString();
                        // Add the loaded bitmap to the cache
                        _cachingProvider.Set(uri, bitmap, TimeSpan.FromSeconds(CacheLongevitySecs));
                        TraceLog($"ImageLoadManager: Loaded bitmap for UriImageSource {uri}");
                        // Remove the Task from the loadingBitmaps dictionary now that we're done loading this image
                        _trackLoadingBitmapsUris.TryRemove(uri, out _);
                    }
                    else
                    if (queueItem.Source is FileImageSource sourceFile)
                    {
                        string uri = sourceFile.File;

                        // Add the loaded bitmap to the cache
                        _cachingProvider.Set(uri, bitmap, TimeSpan.FromSeconds(CacheLongevitySecs));
                        TraceLog($"ImageLoadManager: Loaded bitmap for FileImageSource {uri}");
                        // Remove the Task from the loadingBitmaps dictionary now that we're done loading this image
                        _trackLoadingBitmapsUris.TryRemove(uri, out _);
                    }

                    if (ReuseBitmaps)
                    {
                        queueItem.Task.TrySetResult(bitmap);
                    }
                    else
                    {
                        queueItem.Task.TrySetResult(bitmap.Copy());
                    }

                    //process pending requests
                    string pendingUri = null;
                    if (queueItem.Source is UriImageSource pendingSourceUri)
                    {
                        pendingUri = pendingSourceUri.Uri.ToString();
                    }
                    else if (queueItem.Source is FileImageSource sourceFile)
                    {
                        pendingUri = sourceFile.File;
                    }

                    if (pendingUri != null)
                    {
                        foreach (LoadPriority priority in Enum.GetValues(typeof(LoadPriority)))
                        {
                            var pendingLoads = GetPendingLoadsDictionary((LoadPriority)priority);
                            if (pendingLoads.TryGetValue(pendingUri, out var stack))
                            {
                                QueueItem pendingQueueItem;
                                while (stack.TryPop(out pendingQueueItem))
                                {
                                    if (ReuseBitmaps)
                                    {
                                        pendingQueueItem.Task.TrySetResult(bitmap);
                                    }
                                    else
                                    {
                                        pendingQueueItem.Task.TrySetResult(bitmap.Copy());
                                    }
                                    // Optional: Log or handle the unpaused task
                                }
                                // Clean up the dictionary entry if the stack is empty
                                if (stack.IsEmpty)
                                {
                                    pendingLoads.TryRemove(pendingUri, out _);
                                }
                            }
                        }
                    }
                }
                else
                {
                    //might happen when task was canceled
                    queueItem.Task.TrySetCanceled();

                    FreedQueuedItem(queueItem);
                }


            }
            catch (Exception ex)
            {
                //TraceLog($"ImageLoadManager: Exception {ex}");

                if (ex is OperationCanceledException || ex is System.Threading.Tasks.TaskCanceledException)
                {
                    queueItem.Task.TrySetCanceled();
                }
                else
                {
                    Super.Log(ex);
                    queueItem.Task.TrySetException(ex);
                }

                FreedQueuedItem(queueItem);
            }
            finally
            {
                if (useSemaphore)
                    semaphoreLoad.Release();
            }
        }
    }

    void FreedQueuedItem(QueueItem queueItem)
    {
        if (queueItem.Source is UriImageSource sourceUri)
        {
            _trackLoadingBitmapsUris.TryRemove(sourceUri.Uri.ToString(), out _);
        }
        else
        if (queueItem.Source is FileImageSource sourceFile)
        {
            _trackLoadingBitmapsUris.TryRemove(sourceFile.File, out _);
        }
    }

    public bool IsDisposed { get; protected set; }

    private QueueItem GetPendingItemLoadsForPriority(LoadPriority priority)
    {
        var pendingLoads = GetPendingLoadsDictionary(priority);
        foreach (var pendingPair in pendingLoads)
        {
            try
            {
                if (pendingPair.Value.Count != 0)
                {
                    if (pendingPair.Value.TryPop(out var nextTcs))
                    {
                        TraceLog($"ImageLoadManager: [UNPAUSED] task for {pendingPair.Key}");
                        return nextTcs;
                    }
                }
            }
            catch
            {
            }
        }
        return null;
    }

    private async void ProcessQueue()
    {
        while (!IsDisposed)
        {
            try
            {
                if (IsLoadingLocked)
                {
                    TraceLog($"ImageLoadManager: Loading Locked!");
                    await Task.Delay(50);
                    continue;
                }
                QueueItem queueItem = null;

                lock (lockPending)
                {
                    queueItem = GetPendingItemLoadsForPriority(LoadPriority.High);
                    if (queueItem == null && semaphoreLoad.CurrentCount > 1)
                        queueItem = GetPendingItemLoadsForPriority(LoadPriority.Normal);
                    if (queueItem == null && semaphoreLoad.CurrentCount > 7)
                        queueItem = GetPendingItemLoadsForPriority(LoadPriority.Low);

                    // If we didn't find a task in pendingLoads, try the main queue.
                    lock (lockObject)
                    {
                        if (queueItem == null && _queue.TryDequeue(out queueItem, out LoadPriority priority))
                        {
                            //if (queueItem!=null)
                            //    TraceLog($"[DEQUEUE]: {queueItem.Source} (queue {_queue.Count})");
                        }
                    }

                    Monitor.PulseAll(lockPending);
                }

                if (queueItem != null)
                {
                    //the only really async that works okay 
                    Tasks.StartDelayedAsync(TimeSpan.FromMilliseconds(1), async () =>
                    {
                        await ExecuteLoadTask(queueItem);
                    });
                }
                else
                {
                    await Task.Delay(50);
                }
            }
            catch (Exception e)
            {
                Super.Log(e);
            }
            finally
            {

            }

        }


    }


    public void UpdateInCache(string uri, SKBitmap bitmap, int cacheLongevityMinutes)
    {
        _cachingProvider.Set(uri, bitmap, TimeSpan.FromMinutes(cacheLongevityMinutes));
    }

    /// <summary>
    /// Returns false if key already exists
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="bitmap"></param>
    /// <param name="cacheLongevityMinutes"></param>
    /// <returns></returns>
    public bool AddToCache(string uri, SKBitmap bitmap, int cacheLongevitySecs)
    {
        if (_cachingProvider.Exists(uri))
            return false;

        _cachingProvider.Set(uri, bitmap, TimeSpan.FromSeconds(cacheLongevitySecs));
        return true;
    }

    /// <summary>
    /// Return bitmap from cache if existing, respects the `ReuseBitmaps` flag.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public SKBitmap GetFromCache(string url)
    {
        var bitmap = GetFromCacheInternal(url);
        if (bitmap != null)
            return ReuseBitmaps ? bitmap : bitmap.Copy();
        return null;
    }

    /// <summary>
    /// Used my manager for cache organization. You should use `GetFromCache` for custom controls instead.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public SKBitmap GetFromCacheInternal(string url)
    {
        return _cachingProvider.Get<SKBitmap>(url)?.Value;
    }

    public async Task Preload(ImageSource source, CancellationTokenSource cts)
    {
        if (source.IsEmpty)
        {
            TraceLog($"Preload: Empty source");
            return;
        }
        string uri = GetUriFromImageSource(source);

        if (string.IsNullOrEmpty(uri))
        {
            TraceLog($"Preload: Invalid source {uri}");
            return;
        }

        var cacheKey = uri;

        // Check if the image is already cached or being loaded
        if (_cachingProvider.Get<SKBitmap>(cacheKey).HasValue || _trackLoadingBitmapsUris.ContainsKey(uri))
        {
            TraceLog($"Preload: Image already cached or being loaded for Uri {uri}");
            return;
        }

        var tcs = new TaskCompletionSource<SKBitmap>();
        var tuple = new QueueItem(source, cts, tcs);

        try
        {
            _queue.Enqueue(tuple, LoadPriority.Low);

            // Await the loading to ensure it's completed before returning
            await tcs.Task;
        }
        catch (Exception ex)
        {
            TraceLog($"Preload: Exception {ex}");
        }
    }

    public static string GetUriFromImageSource(ImageSource source)
    {
        if (source is UriImageSource uriSource)
        {
            return uriSource.Uri.ToString();
        }
        if (source is FileImageSource fileSource)
        {
            return fileSource.File;
        }
        if (source is ImageSourceResourceStream resourceStream)
        {
            return resourceStream.Url;
        }
        if (source is StreamImageSource)
        {
            return Guid.NewGuid().ToString();
        }
        return null;
    }


    public void Dispose()
    {
        IsDisposed = true;

        semaphoreLoad?.Dispose();

        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }

    public bool IsOffline { get; protected set; }

    private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        var connected = e.NetworkAccess;
        bool isOffline = connected != NetworkAccess.Internet
                        && connected != NetworkAccess.ConstrainedInternet;
        if (IsOffline && !isOffline)
        {
            CanReload?.Invoke(this, null);
        }
        IsOffline = isOffline;
    }

    public static async Task<SKBitmap> LoadFromFile(string filename, CancellationToken cancel)
    {

        try
        {
            cancel.ThrowIfCancellationRequested();

            SKBitmap bitmap = SkiaImageManager.Instance.GetFromCacheInternal(filename);
            if (bitmap != null)
            {
                TraceLog($"ImageLoadManager: Loaded local bitmap from cache {filename}");
                return bitmap;
            }

            TraceLog($"ImageLoadManager: Loading local {filename}");

            cancel.ThrowIfCancellationRequested();

            if (filename.SafeContainsInLower(SkiaImageManager.NativeFilePrefix))
            {
                var fullFilename = filename.Replace(SkiaImageManager.NativeFilePrefix, "");
                using var stream = new FileStream(fullFilename, FileMode.Open);
                cancel.Register(stream.Close);
                bitmap = SKBitmap.Decode(stream);
            }
            else
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(filename);
                using var reader = new StreamReader(stream);
                bitmap = SKBitmap.Decode(stream);
            }

            cancel.ThrowIfCancellationRequested();

            if (bitmap != null)
            {
                TraceLog($"ImageLoadManager: Loaded local bitmap {filename}");

                if (SkiaImageManager.Instance.AddToCache(filename, bitmap, SkiaImageManager.CacheLongevitySecs))
                {
                    return ReuseBitmaps ? bitmap : bitmap.Copy();
                }
            }
            else
            {
                TraceLog($"ImageLoadManager: FAILED to load local {filename}");
            }

            return bitmap;

        }
        catch (OperationCanceledException)
        {
            TraceLog("ImageLoadManager loading was canceled.");
            return null;
        }
        catch (Exception e)
        {
            Super.Log(e);
        }

        return null;

    }

}

/*
public partial class SkiaImageManager : IDisposable
{
    private readonly IEasyCachingProvider _cachingProvider = new SimpleCachingProvider();

    public record QueueItem
    {
        public QueueItem(ImageSource source, CancellationTokenSource cancel, TaskCompletionSource<SKBitmap> task)
        {
            Source = source;
            Cancel = cancel;
            Task = task;
        }

        public ImageSource Source { get; set; }
        public CancellationTokenSource Cancel { get; set; }
        public TaskCompletionSource<SKBitmap> Task { get; set; }
    }

    /// <summary>
    /// If set to true will not return clones for same sources, but will just return the existing cached SKBitmap reference. Useful if you have a lot on images reusing same sources, but you have to be carefull not to dispose the shared image. SkiaImage is aware of this setting and will keep a cached SKBitmap from being disposed.
    /// </summary>
    public static bool ReuseBitmaps = false;

    /// <summary>
    /// Caching provider setting
    /// </summary>
    public static int CacheLongevitySecs = 1800; //30mins

    /// <summary>
    /// Convention for local files saved in native platform. Shared resources from Resources/Raw/ do not need this prefix.
    /// </summary>
    public static string NativeFilePrefix = "file://";

    public event EventHandler CanReload;

    public static bool LogEnabled = false;

    public static void TraceLog(string message)
    {
        if (LogEnabled)
        {
#if WINDOWS
            Trace.WriteLine(message);
#else
            Console.WriteLine("*******************************************");
            Console.WriteLine(message);
#endif
        }
    }

    static SkiaImageManager _instance;
    private static int _loadingTasksCount;
    private static int _queuedTasksCount;

    public static SkiaImageManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new SkiaImageManager();

            return _instance;
        }
    }

    public SkiaImageManager()
    {

        var connected = Connectivity.NetworkAccess;
        if (connected != NetworkAccess.Internet
            && connected != NetworkAccess.ConstrainedInternet)
        {
            IsOffline = true;
        }

        Tasks.StartDelayed(TimeSpan.FromMilliseconds(100), () =>
        {
            LaunchProcessQueue();
        });
    }


    private SemaphoreSlim semaphoreLoad = new(16, 16);

    private readonly object lockObject = new object();

    private bool _isLoadingLocked;
    public bool IsLoadingLocked
    {
        get => _isLoadingLocked;
        set
        {
            if (_isLoadingLocked != value)
            {
                _isLoadingLocked = value;
            }
        }
    }


    public void CancelAll()
    {
        lock (lockObject)
        {
            while (_queue.Count > 0)
            {
                if (_queue.TryDequeue(out var item))
                    item.Cancel.Cancel();
            }
        }
    }

    private readonly ConcurrentQueue<QueueItem> _queue = new();

    private readonly ConcurrentDictionary<string, Task<SKBitmap>> _trackLoadingBitmapsUris = new();

    //todo avoid conflicts, cannot use concurrent otherwise will loose data
    private readonly Dictionary<string, Stack<QueueItem>> pendingLoads = new();

    public Task<SKBitmap> Enqueue(ImageSource source, CancellationTokenSource token)
    {

        var tcs = new TaskCompletionSource<SKBitmap>();

        string uri = null;

        if (!source.IsEmpty)
        {
            if (source is UriImageSource sourceUri)
            {
                uri = sourceUri.Uri.ToString();
            }
            else
            if (source is FileImageSource sourceFile)
            {
                uri = sourceFile.File;
            }

            // 1 Try to get from cache
            var cacheKey = uri;

            var cachedBitmap = _cachingProvider.Get<SKBitmap>(cacheKey);
            if (cachedBitmap.HasValue)
            {
                if (ReuseBitmaps)
                {
                    tcs.TrySetResult(cachedBitmap.Value);
                }
                else
                {
                    tcs.TrySetResult(cachedBitmap.Value.Copy());
                }
                TraceLog($"ImageLoadManager: Returning cached bitmap for UriImageSource {uri}");

                //if (pendingLoads.Any(x => x.Value.Count != 0))
                //{
                //    RunProcessQueue();
                //}

                return tcs.Task;
            }
            TraceLog($"ImageLoadManager: Not found cached UriImageSource {uri}");

            // 2 put to queue
            var tuple = new QueueItem(source, token, tcs);

            if (uri == null)
            {
                //no queue, maybe stream
                TraceLog($"ImageLoadManager: DIRECT ExecuteLoadTask !!!");
                Tasks.StartDelayedAsync(TimeSpan.FromMilliseconds(1), async () =>
                {
                    await ExecuteLoadTask(tuple);
                });
            }
            else
            {
                var urlAlreadyLoading = _trackLoadingBitmapsUris.ContainsKey(uri);
                if (urlAlreadyLoading)
                {
                    lock (pendingLoads)
                    {
                        // we're currently loading the same image, save the task to pendingLoads
                        TraceLog($"ImageLoadManager: Same image already loading, pausing task for UriImageSource {uri}");
                        if (pendingLoads.TryGetValue(uri, out var stack))
                        {
                            stack.Push(tuple);
                        }
                        else
                        {
                            var pendingStack = new Stack<QueueItem>();
                            pendingStack.Push(tuple);
                            pendingLoads[uri] = pendingStack;
                        }

                        Monitor.PulseAll(pendingLoads);
                    }
                }
                else
                {
                    // We're about to load this image, so add its Task to the loadingBitmaps dictionary
                    _trackLoadingBitmapsUris[uri] = tcs.Task;
                    lock (lockObject)
                    {
                        _queue.Enqueue(tuple);
                    }

                    TraceLog($"ImageLoadManager: Enqueued {uri} (queue {_queue.Count})");
                }

            }



        }

        return tcs.Task;
    }

    void LaunchProcessQueue()
    {
        Task.Run(async () =>
        {
            ProcessQueue();

        }).ConfigureAwait(false);
    }


    private async Task ExecuteLoadTask(QueueItem queueItem)
    {
        if (queueItem != null)
        {
            //do not limit local file loads
            bool useSemaphore = queueItem.Source is UriImageSource;

            try
            {
                if (useSemaphore)
                    await semaphoreLoad.WaitAsync();

                TraceLog($"ImageLoadManager: LoadSKBitmapAsync {queueItem.Source}");

                SKBitmap bitmap = await Super.Native.LoadSKBitmapAsync(queueItem.Source, queueItem.Cancel.Token);


                // Add the loaded bitmap to the context cache
                if (bitmap != null)
                {
                    if (queueItem.Source is UriImageSource sourceUri)
                    {
                        string uri = sourceUri.Uri.ToString();
                        // Add the loaded bitmap to the cache
                        _cachingProvider.Set(uri, bitmap, TimeSpan.FromSeconds(CacheLongevitySecs));
                        TraceLog($"ImageLoadManager: Loaded bitmap for UriImageSource {uri}");
                        // Remove the Task from the loadingBitmaps dictionary now that we're done loading this image
                        _trackLoadingBitmapsUris.TryRemove(uri, out _);
                    }
                    else
                    if (queueItem.Source is FileImageSource sourceFile)
                    {
                        string uri = sourceFile.File;

                        // Add the loaded bitmap to the cache
                        _cachingProvider.Set(uri, bitmap, TimeSpan.FromSeconds(CacheLongevitySecs));
                        TraceLog($"ImageLoadManager: Loaded bitmap for FileImageSource {uri}");
                        // Remove the Task from the loadingBitmaps dictionary now that we're done loading this image
                        _trackLoadingBitmapsUris.TryRemove(uri, out _);
                    }

                    if (ReuseBitmaps)
                    {
                        queueItem.Task.TrySetResult(bitmap);
                    }
                    else
                    {
                        queueItem.Task.TrySetResult(bitmap.Copy());
                    }

                }
                else
                {
                    TraceLog($"ImageLoadManager: BITMAP NULL for {queueItem.Source}");
                }


            }
            catch (Exception ex)
            {
                Super.Log($"ImageLoadManager: Exception {ex}");

                if (ex is OperationCanceledException)
                {
                    queueItem.Task.TrySetCanceled();
                }
                else
                {
                    queueItem.Task.TrySetException(ex);
                }

                if (queueItem.Source is UriImageSource sourceUri)
                {
                    _trackLoadingBitmapsUris.TryRemove(sourceUri.Uri.ToString(), out _);
                }
                else
                if (queueItem.Source is FileImageSource sourceFile)
                {
                    _trackLoadingBitmapsUris.TryRemove(sourceFile.File, out _);
                }
            }
            finally
            {
                if (useSemaphore)
                    semaphoreLoad.Release();
            }
        }
    }


    public bool IsDisposed { get; protected set; }


    private async void ProcessQueue()
    {
        while (!IsDisposed)
        {
            try
            {

                QueueItem queueItem = null;

                if (IsLoadingLocked)
                {
                    TraceLog($"ImageLoadManager: Loading Locked!");
                    await Task.Delay(50);
                    continue;
                }

                lock (pendingLoads)
                {
                    foreach (var pendingPair in pendingLoads)
                    {
                        if (pendingPair.Value.Count > 0)
                        {
                            var nextTcs = pendingPair.Value.Pop();

                            string uri = pendingPair.Key;

                            //_trackLoadingBitmapsUris[uri] = nextTcs.Item3.Task;

                            queueItem = nextTcs;

                            TraceLog($"ImageLoadManager: [UNPAUSED] task for {uri}");

                            break; // We only want to move one task to the main queue at a time.
                        }
                    }

                    Monitor.PulseAll(pendingLoads);
                }

                // If we didn't find a task in pendingLoads, try the main queue.
                lock (lockObject)
                {
                    if (queueItem == null && _queue.TryDequeue(out queueItem))
                    {
                        TraceLog($"[DEQUEUE]: {queueItem.Source} (queue {_queue.Count})");
                    }
                }

                if (queueItem != null)
                {
                    //the only really async that works okay 
                    Tasks.StartDelayedAsync(TimeSpan.FromMilliseconds(1), async () =>
                    {
                        await ExecuteLoadTask(queueItem);
                    });
                }
                else
                {
                    await Task.Delay(50);
                }
            }
            catch (Exception e)
            {
                Super.Log(e);
            }
            finally
            {

            }

        }


    }


    public void UpdateInCache(string uri, SKBitmap bitmap, int cacheLongevityMinutes)
    {
        _cachingProvider.Set(uri, bitmap, TimeSpan.FromMinutes(cacheLongevityMinutes));
    }

    /// <summary>
    /// Returns false if key already exists
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="bitmap"></param>
    /// <param name="cacheLongevityMinutes"></param>
    /// <returns></returns>
    public bool AddToCache(string uri, SKBitmap bitmap, int cacheLongevitySecs)
    {
        if (_cachingProvider.Exists(uri))
            return false;

        _cachingProvider.Set(uri, bitmap, TimeSpan.FromSeconds(cacheLongevitySecs));
        return true;
    }

    public SKBitmap GetFromCache(string url)
    {
        return _cachingProvider.Get<SKBitmap>(url)?.Value;
    }

    public async Task Preload(string uri, CancellationTokenSource cts)
    {
        if (string.IsNullOrEmpty(uri))
        {
            TraceLog($"Preload: Invalid Uri {uri}");
            return;
        }

        ImageSource source = new UriImageSource()
        {
            Uri = new Uri(uri)
        };

        var cacheKey = uri;

        // Check if the image is already cached or being loaded
        if (_cachingProvider.Get<SKBitmap>(cacheKey).HasValue || _trackLoadingBitmapsUris.ContainsKey(uri))
        {
            TraceLog($"Preload: Image already cached or being loaded for Uri {uri}");
            return;
        }

        var tcs = new TaskCompletionSource<SKBitmap>();

        var tuple = new QueueItem(source, cts, tcs);

        lock (lockObject)
        {
            _queue.Enqueue(tuple);
        }

        try
        {
            // Await the loading to ensure it's completed before returning
            await tcs.Task;
        }
        catch (Exception ex)
        {
            TraceLog($"Preload: Exception {ex}");
        }
    }

    private string GetUriFromImageSource(ImageSource source)
    {
        if (source is StreamImageSource)
            return Guid.NewGuid().ToString();
        else if (source is UriImageSource sourceUri)
            return sourceUri.Uri.ToString();
        else if (source is FileImageSource sourceFile)
            return sourceFile.File;

        return null;
    }



#if ((NET7_0 || NET8_0) && !ANDROID && !IOS && !MACCATALYST && !WINDOWS && !TIZEN)

    public static async Task<SKBitmap> LoadSKBitmapAsync(ImageSource source, CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

#endif

    public void Dispose()
    {
        IsDisposed = true;

        semaphoreLoad?.Dispose();

        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }

    public bool IsOffline { get; protected set; }

    private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        var connected = e.NetworkAccess;
        bool isOffline = connected != NetworkAccess.Internet
                        && connected != NetworkAccess.ConstrainedInternet;
        if (IsOffline && !isOffline)
        {
            CanReload?.Invoke(this, null);
        }
        IsOffline = isOffline;
    }

    public static async Task<SKBitmap> LoadFromFile(string filename, CancellationToken cancel)
    {

        try
        {
            cancel.ThrowIfCancellationRequested();

            SKBitmap bitmap = SkiaImageManager.Instance.GetFromCache(filename);
            if (bitmap != null)
            {
                TraceLog($"ImageLoadManager: Loaded local bitmap from cache {filename}");
                return bitmap;
            }

            TraceLog($"ImageLoadManager: Loading local {filename}");

            cancel.ThrowIfCancellationRequested();

            if (filename.SafeContainsInLower(SkiaImageManager.NativeFilePrefix))
            {
                var fullFilename = filename.Replace(SkiaImageManager.NativeFilePrefix, "");
                using var stream = new FileStream(fullFilename, FileMode.Open);
                cancel.Register(stream.Close);  // Register cancellation to close the stream
                bitmap = SKBitmap.Decode(stream);
            }
            else
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(filename);  // Pass cancellation token
                using var reader = new StreamReader(stream);
                bitmap = SKBitmap.Decode(stream);
            }

            cancel.ThrowIfCancellationRequested();

            if (bitmap != null)
            {
                TraceLog($"ImageLoadManager: Loaded local bitmap {filename}");

                if (SkiaImageManager.Instance.AddToCache(filename, bitmap, SkiaImageManager.CacheLongevitySecs))
                {
                    return ReuseBitmaps ? bitmap : bitmap.Copy();
                }
            }
            else
            {
                TraceLog($"ImageLoadManager: FAILED to load local {filename}");
            }

            return bitmap;

        }
        catch (OperationCanceledException)
        {
            TraceLog("ImageLoadManager loading was canceled.");
            return null;
        }
        catch (Exception e)
        {
            Super.Log(e);
        }

        return null;

    }

}
*/
