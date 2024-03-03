# DrawnUI For Xamarin Forms
A Light version of [DrawnUI for .Net MAUI](https://github.com/taublast/DrawnUi.Maui) ported to Xamarin.Forms. 

Use case: to remove UI lags in legacy projects and prepare them to be easily ported to MAUI by replacing all used UI-drawing related nugets (shadows, cards, gradients etc..) with just Drawn UI.

Supports gestures and many virtual controls, main limitation of the Light version is lack of SkiaShell and similar.

Will not support Xamarin built-in Xaml HotReload, contrary to MAUI, due to Xamarin architecture.

_Uses a modified [Glidex.Forms](https://github.com/jonathanpeppers/glidex) project created by Jonathan Peppers for caching images on Android._
