namespace AppoMobi.Xamarin.DrawnUi.DrawnUi.Controls.RadioButtons;

public interface ISkiaRadioButton : ISkiaControl
{
    public void SetValueInternal(bool value);

    public bool GetValueInternal();

    public SkiaControl GroupParent { get; }

    public string GroupName { get; set; }
}