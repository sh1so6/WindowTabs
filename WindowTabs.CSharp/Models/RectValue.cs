namespace WindowTabs.CSharp.Models
{
    internal sealed class RectValue
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public int Right => X + Width;

        public int Bottom => Y + Height;

        public bool ContainsRect(RectValue other)
        {
            return other != null
                && other.Right > X
                && other.X < Right
                && other.Bottom > Y
                && other.Y < Bottom;
        }
    }
}
