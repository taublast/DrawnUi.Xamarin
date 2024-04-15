﻿using AppoMobi.Forms.Gestures;
using Xamarin.Essentials;

namespace DrawnUi.Maui.Models;

public class Screen : BindableObject
{
    public static DisplayInfo DisplayInfo => DeviceDisplay.MainDisplayInfo;

    public static bool KeepScreenOn => DeviceDisplay.KeepScreenOn;

    private double _BottomInset;
    public double BottomInset
    {
        get { return _BottomInset; }
        set
        {
            if (_BottomInset != value)
            {
                _BottomInset = value;
                OnPropertyChanged();
            }
        }
    }

    private double _TopInset;
    public double TopInset
    {
        get { return _TopInset; }
        set
        {
            if (_TopInset != value)
            {
                _TopInset = value;
                OnPropertyChanged();
            }
        }
    }

    private double _LeftInset;
    public double LeftInset
    {
        get { return _LeftInset; }
        set
        {
            if (_LeftInset != value)
            {
                _LeftInset = value;
                OnPropertyChanged();
            }
        }
    }

    private double _RightInset;
    public double RightInset
    {
        get { return _RightInset; }
        set
        {
            if (_RightInset != value)
            {
                _RightInset = value;
                OnPropertyChanged();
            }
        }
    }

    private double _Density;
    public double Density
    {
        get
        {
            return _Density;
        }
        set
        {
            if (_Density != value)
            {
                _Density = value;
                TouchEffect.Density = (float)value;
                OnPropertyChanged();
            }
        }
    }

    private double _WidthDip;
    public double WidthDip
    {
        get { return _WidthDip; }
        set
        {
            if (_WidthDip != value)
            {
                _WidthDip = value;
                OnPropertyChanged();
            }
        }
    }

    private double _HeightDip;
    public double HeightDip
    {
        get { return _HeightDip; }
        set
        {
            if (_HeightDip != value)
            {
                _HeightDip = value;
                OnPropertyChanged();
            }
        }
    }





}