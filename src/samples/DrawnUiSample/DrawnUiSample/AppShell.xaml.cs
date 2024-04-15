using DrawnUiSample.ViewModels;
using DrawnUiSample.Views;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace DrawnUiSample
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
            Routing.RegisterRoute(nameof(NewItemPage), typeof(NewItemPage));
        }

    }
}
