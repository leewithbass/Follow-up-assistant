namespace FloatBrowser.App.Domain;

public class WindowStateSnapshot
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
    public double Opacity { get; set; }
    public bool Topmost { get; set; }
    public bool Borderless { get; set; }
}
