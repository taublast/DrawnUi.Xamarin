using DrawnUiSample.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace DrawnUiSample.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}