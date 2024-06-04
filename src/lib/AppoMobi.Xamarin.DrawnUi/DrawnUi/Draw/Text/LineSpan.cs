namespace DrawnUi.Maui.Draw;

public struct LineSpan
{
    public LineSpan()
    {
        Size = SKSize.Empty;
        Text = string.Empty;
        Span = null;
        Glyphs = null;
        NeedsShaping = false;
    }

    public string Text { get; set; }

    public TextSpan Span { get; set; }

    public LineGlyph[] Glyphs { get; set; }

    public bool NeedsShaping { get; set; }

    public SKSize Size { get; set; } 

    //public SKRect DrawingRect { get; set; }

    public static LineSpan Default
    {
        get
        {
            return new LineSpan();
        }
    }
}