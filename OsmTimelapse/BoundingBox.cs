using System;
using System.Text;

namespace OsmTimelapse
{
    public readonly struct BoundingBox
    {
        public (uint x, uint y) Origin { get;}
        public uint Width { get; }
        public uint Height { get; }

        public (uint x, uint y) TopLeft => Origin;
        public (uint x, uint y) TopRight => (Origin.x + Width, Origin.y);
        public (uint x, uint y) BottomLeft => (Origin.x, Origin.y + Height);
        public (uint x, uint y) BottomRight => (Origin.x + Width, Origin.y + Height);

        public int Area => (int) Height * (int) Width;

        public BoundingBox((uint x, uint y) a, (uint x, uint y) b)
        {
            var minX = Math.Min(a.x, b.x);
            var maxX = Math.Max(a.x, b.x);
            var minY = Math.Min(a.y, b.y);
            var maxY = Math.Max(a.y, b.y);

            Origin = (minX, minY);
            // Adding 1 to the dimensions ensures the right and bottom edge tiles are also included.
            // The max x/y values are inclusive because the bottom-right coordinate falls in that tile.
            Width = maxX - minX + 1;
            Height = maxY - minY + 1;
        }

        public override bool Equals(object obj)
        {
            return obj is BoundingBox box && Equals(box) && base.Equals(obj);
        }

        private bool Equals(BoundingBox other)
        {
            return Origin.Equals(other.Origin) && Width == other.Width && Height == other.Height;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Origin, Width, Height);
        }

        public override string ToString()
        {
            // (x,y) - - (x,y)
            // |             |
            // (x,y) - - (x,y)
            // TODO clean this up with string.format (or interpolation $ with formatting)

            var top = new [] {$"({TopLeft.x},{TopLeft.y})", $"({TopRight.x},{TopRight.y})"};
            var bottom = new [] {$"({BottomLeft.x},{BottomLeft.y})", $"({BottomRight.x},{BottomRight.y})"};
            var topLength = top[0].Length + top[1].Length;
            var bottomLength = bottom[0].Length + bottom[1].Length;
            var width = Math.Max(topLength, bottomLength) + 5;

            var boxString = new StringBuilder();
            // Line 1
            boxString.Append(' ').Append(top[0]).Append(" - ");
            for (var i = 0; i < width - (topLength + 5); i++)
            {
                boxString.Append(' ');
            }

            boxString.Append("- ").Append(top[1]).AppendLine();
            
            // Line 2
            boxString.Append(' ').Append('|');
            for (var i = 0; i < width - 2; i++)
            {
                boxString.Append(' ');
            }

            boxString.Append('|').AppendLine();
            
            // Line 3
            boxString.Append(' ').Append(bottom[0]).Append(" - ");
            for (var i = 0; i < width - (bottomLength + 5); i++)
            {
                boxString.Append(' ');
            }

            boxString.Append("- ").Append(bottom[1]);

            return $"Bounding box (size: {Width}x{Height}, area: {Area:#,#}):\n{boxString}";
        }

        public static bool operator ==(BoundingBox left, BoundingBox right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoundingBox left, BoundingBox right)
        {
            return !(left == right);
        }
    }
}
