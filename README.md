# DrawnUI For Xamarin Forms
A Light version of [DrawnUI for .Net MAUI](https://github.com/taublast/DrawnUi.Maui) ported to Xamarin.Forms. 

 ___Please star ⭐ if you like it!___

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

 ## The How To

 This light Xamarin version has some limitations and some controls are missing in comparision to the [original MAUI Library](https://github.com/taublast/DrawnUi.Maui). Meanwhile please refer to the [original library documentation](https://github.com/taublast/DrawnUi.Maui/wiki).

 There is also a sample project in this repo with a drawn About page, would add more with time.
 
## To Note

Will not support Xamarin built-in Xaml HotReload, contrary to MAUI, due to Xamarin architecture.

`SkiaLabel` `FontSize` property accepts `double` only, setting something like `FontSize="Title"` will result in a `XFC0000` error.

Loading resources is different from MAUI version:

* `SkiaImage` read files from native projects by default. With "resource://" prefix you can load file from shared project, for example `   Source="resource://Resources.Images.breath.jpg"`. You can of course pass an internet url too.

`SkiaLottie` and `SkiaSvg` `Source` property will always read files from shared project, files must be inlcuded with build action as `Embeeded resource`, for example `Source = "Resources\Lottie\plus.json"`. You can specify an internet url too.

_Uses a modified [Glidex.Forms](https://github.com/jonathanpeppers/glidex) project created by Jonathan Peppers for caching images on Android._