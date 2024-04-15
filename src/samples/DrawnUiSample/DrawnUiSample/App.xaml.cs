using DrawnUiSample.Services;
using DrawnUiSample.Views;
using System;
using DrawnUi.Maui.Views;
using DrawnUi.Maui.Draw;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DrawnUiSample
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new AppShell();
            
            var check = new SkiaImage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
