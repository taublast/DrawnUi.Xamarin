global using Newtonsoft.Json;
global using System;
global using System.Threading;
global using System.Threading.Tasks;
using AppoMobi.Xamarin.DrawnUi.Services;

using AppoMobi.Xamarin.DrawnUi.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AppoMobi.Xamarin.DrawnUi
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new AppShell();
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
