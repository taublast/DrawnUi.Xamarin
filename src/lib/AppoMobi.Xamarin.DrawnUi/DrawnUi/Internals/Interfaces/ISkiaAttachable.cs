using DrawnUi.Maui.Draw;

public interface IWithContent
{
    SkiaControl Content { get; set; }
}

public interface ISelectableOption : IHasTitleWithId, ICanBeSelected
{
    public bool IsReadOnly { get; }
}

public interface ISelectableRangeOption : ISelectableOption, ISelectableRange
{

}


public interface ISelectableRange : ICanBeSelected
{
    bool SelectionStart { get; set; }
    bool SelectionEnd { get; set; }
}
