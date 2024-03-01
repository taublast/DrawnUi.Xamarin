using AppoMobi.Xamarin.DrawnUi.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace AppoMobi.Xamarin.DrawnUi.Views
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