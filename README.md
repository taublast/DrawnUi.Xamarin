# DrawnUI For Xamarin Forms
A Light version of [DrawnUI for .Net MAUI](https://github.com/taublast/DrawnUi.Maui) ported to Xamarin.Forms. 

## Use case
To remove UI lags in Xamarin projects by replacing native views with drawn controls. At the same time to prepare projects to be more easily ported to MAUI as all UI-related nugets like shadows, cards, gradients etc.. will be replaced with just DrawnUI.

## Installation

Install the nuget package `__AppoMobi.Xamarin.DrawnUi__` into shared/native Xamarin app projects.  Then initialize the library:

* **iOS** - inside your `AppDelegate.cs` in the `FinishedLaunching` method:
```csharp
 AppoMobi.Xamarin.DrawnUi.Apple.DrawnUi.Initialize<App>();
 ```

* **Android** - inside `MainActivity.cs` in the `OnCreate` method:
```csharp
AppoMobi.Xamarin.DrawnUi.Droid.DrawnUi.Initialize<App>(this);
 ```

Also in Xamarin for many Canvases around the app it's better to use  
```csharp
Super.CanUseHardwareAcceleration = false; //RIP XAMARIN
 ```
For single `Canvas` this should not be needed.

 ## The How To

This light Xamarin version has some limitations and some controls are missing in comparision to the [original MAUI Library](https://github.com/taublast/DrawnUi.Maui). Meanwhile please refer to the [original library documentation](https://github.com/taublast/DrawnUi.Maui/wiki).

There is also a sample project in this repo with a drawn About page:

![image](https://github.com/user-attachments/assets/3e622b4d-d628-499b-9eec-2f6648041aae) | 

  
## Xamarin Limitations

Contrary to MAUI version:

* Xamarin XAML HotReload is found to be working occasionally. I still struggle to understand why and where it breaks and why it's suddenly working.. Good news in MAUI we do not have this issue, there it breaks for foundable reasons, though should create a ticker for all cases i just personally know why it breaks but we have zero info about it in console, log etc.. :)

* For same reasons `ItemTemplate` is not set when defined in `<DataTemplate>` XAML so set it like this:

```xml
    <draw:SkiaLayout
        CommandChildTapped="{Binding CommandOpenFile}"
        HorizontalOptions="Fill"
        ItemTemplate="{Binding CreateUploadCell}"
        ItemsSource="{Binding Uploads}"
        Type="Column" />
```

and 
```csharp
    public DataTemplate CreateUploadCell => new DataTemplate(() =>
    {
        return new CellUpload();
    });

```

`SkiaLabel` `FontSize` property accepts `double` only, setting something like `FontSize="Title"` will result in a `XFC0000` error.

* Loading resources is different from MAUI version:

1. `SkiaLottie` and `SkiaSvg` `Source` property will always read files from shared project, files must be inlcuded with build action as `Embeeded resource`, for example `Source = "Resources\Lottie\plus.json"`. You can specify an internet url too.

2. `SkiaImage` read files from native projects by default. With "resource://" prefix you can load file from shared project, for example `   Source="resource://Resources.Images.breath.jpg"`. You can of course pass an internet url too.

## To Note

_Uses a modified [Glidex.Forms](https://github.com/jonathanpeppers/glidex) project created by Jonathan Peppers for caching images on Android._

___Please star ⭐ if you like it!___
