namespace WindowTabs.CSharp.Models
{
    internal sealed class WindowPlacementValue
    {
        public int Flags { get; set; }

        public int ShowCommand { get; set; }

        public PointValue MaxPosition { get; set; } = new PointValue();

        public PointValue MinPosition { get; set; } = new PointValue();

        public RectValue NormalPosition { get; set; } = new RectValue();
    }
}
