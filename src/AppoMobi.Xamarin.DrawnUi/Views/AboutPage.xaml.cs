using System;
using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AppoMobi.Xamarin.DrawnUi.Views
{
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception e)
            {
                Super.DisplayException(this, e);
            }
        }


        public void OnTapped(object sender, EventArgs e)
        {
            // Handle the tap event
            Console.WriteLine("Was tapped");
        }

        public EventHandler Tapped
        {
            get
            {
                return OnTapped;
            }
        }

        public ICommand SimpleCommand
        {
            get
            {
                return new Command((c) =>
                {
                    Super.Log("SIMPLE COMMAND");
                });
            }
        }
    }
}