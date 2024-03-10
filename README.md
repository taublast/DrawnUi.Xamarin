# DrawnUI For Xamarin Forms
A Light version of [DrawnUI for .Net MAUI](https://github.com/taublast/DrawnUi.Maui) ported to Xamarin.Forms. 

## Use case
To remove UI lags in legacy projects and prepare them to be easily ported to MAUI by replacing all used UI-drawing related nugets (shadows, cards, gradients etc..) with just DrawnUI. 

## What's New
1.0.0.20
* Added `SkiaLottie`
* Fixes from MAUI nuget 1.0.8.7

## Features

Supports gestures and many virtual controls from the [original MAUI library](https://github.com/taublast/DrawnUi.Maui), main limitation of the Light version is lack of SkiaShell and similar.

Will not support Xamarin built-in Xaml HotReload, contrary to MAUI, due to Xamarin architecture.

For `SkiaLottie` and `SkiaSvg` the `Source` property will read files from shared project you need to setup `Super.AppAssembly = typeof(App).Assembly;` at app startup. Then you could use a shared file inlcuded with build action as `Embeeded resource`, for example (SkiaSvg): `Source = "Resources\Lottie\plus.json"`.

To read from native projects for all controls use usual prefix `file://`:
* iOS: place files inside `Resources` folder as `AndroidResource`.
* Android: place files inside `Resources/raw` folder as `BoundleResource`.

Nuget must be installed into your app shared+native projects.

_Uses a modified [Glidex.Forms](https://github.com/jonathanpeppers/glidex) project created by Jonathan Peppers for caching images on Android._
