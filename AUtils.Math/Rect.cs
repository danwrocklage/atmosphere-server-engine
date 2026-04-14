using System.Diagnostics;

namespace AUtils.Math;

[DebuggerDisplay("{TopLeft} - {BottomRight}")]
public readonly struct Rect
{
    public Rect(in Point topLeft, in Point bottomRight)
    {
        TopLeft = topLeft;
        BottomRight = bottomRight;
    }
    
    public Rect(in Point center, float centerToTop, float centerToLeft, bool onlyPositive = false)
    {
        centerToTop = MathF.Abs(centerToTop);
        centerToLeft = MathF.Abs(centerToLeft);

        var topLeftX = center.X - centerToLeft;
        var topLeftY = center.Y - centerToTop;
        TopLeft = new Point
        {
            X = onlyPositive && topLeftX < 0 ? 0 : topLeftX, 
            Y = onlyPositive && topLeftY < 0 ? 0 : topLeftY
        };
        BottomRight = new Point {X = center.X + centerToLeft, Y = center.Y + centerToTop};
    }
    
    public Point TopLeft { get; }
    
    public Point BottomRight { get; }

    public float Width => BottomRight.X - TopLeft.X;

    public float Height => BottomRight.Y - TopLeft.Y;

    public Point TopRight => new() {X = BottomRight.X, Y = TopLeft.Y};
    
    public Point BottomLeft => new() {X = TopLeft.X, Y = BottomRight.Y};

    public bool Contains(Point point) =>
        point.X >= TopLeft.X && point.X <= BottomRight.X &&
        point.Y >= TopLeft.Y && point.Y <= BottomRight.Y;
    
    public static bool operator ==(Rect source, Rect other) => 
        source.TopLeft == other.TopLeft && source.BottomRight == other.BottomRight;

    public static bool operator !=(Rect source, Rect other) => 
        source.TopLeft != other.TopLeft || source.BottomRight != other.BottomRight;
    
    public bool Equals(Rect other) => TopLeft.Equals(other.TopLeft) && BottomRight.Equals(other.BottomRight);

    public override bool Equals(object obj) => obj is Rect other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(TopLeft, BottomRight);
}