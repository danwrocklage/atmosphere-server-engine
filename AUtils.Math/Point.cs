using System.Diagnostics;
using System.Text;
using AUtils.Sil;

namespace AUtils.Math;

[DebuggerDisplay("{X} {Y}")]
[Sil(126)]
public struct Point
{
    private const float TOLERANCE = 0.00001f;
    
    public Point(float x, float y)
    {
        X = x;
        Y = y;
    }
    
    public float X { get; set; }
    
    public float Y { get; set; }

    public static bool operator ==(Point source, Point other) => 
        System.Math.Abs(source.X - other.X) < TOLERANCE && System.Math.Abs(source.Y - other.Y) < TOLERANCE;
    
    public static bool operator !=(Point source, Point other) => 
        System.Math.Abs(source.X - other.X) > TOLERANCE || System.Math.Abs(source.Y - other.Y) > TOLERANCE;

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(X);
        builder.Append(' ');
        builder.Append(Y);
        
        return builder.ToString();
    }
    
    public bool Equals(Point other) => X.Equals(other.X) && Y.Equals(other.Y);

    public override bool Equals(object obj) => obj is Point other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y);
}