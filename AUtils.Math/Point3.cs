using System.Diagnostics;
using System.Text;
using AUtils.Sil;

namespace AUtils.Math;

[DebuggerDisplay("Point: {X} {Y} {Z}")]
[Sil(125)]
public struct Point3
{
    private const float TOLERANCE = 0.00001f;

    public static readonly Point3 Empty = new (0, 0, 0);
    
    public float X { get; set; }
    
    public float Y { get; set; }
    
    public float Z { get; set; }

    public Point3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static implicit operator Point(Point3 source) => new(source.X, source.Y);

    public static Point3 FromArray(float[] values)
    {
        if (values == null) 
            throw new ArgumentNullException(nameof(values));
        
        return values.Length switch
        {
            0 => new Point3(),
            1 => new Point3 {X = values[0]},
            2 => new Point3 {X = values[0], Y = values[1]},
            _ => new Point3 {X = values[0], Y = values[1], Z = values[2]}
        };
    }

    public float[] ToArray() => new[] {X, Y, Z};

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(X);
        builder.Append(' ');
        builder.Append(Y);
        builder.Append(' ');
        builder.Append(Z);
        
        return builder.ToString();
    }
    
    public static bool operator ==(Point3 source, Point3 other) => 
        System.Math.Abs(source.X - other.X) < TOLERANCE && System.Math.Abs(source.Y - other.Y) < TOLERANCE && System.Math.Abs(source.Z - other.Z) < TOLERANCE;
    
    public static bool operator !=(Point3 source, Point3 other) => 
        System.Math.Abs(source.X - other.X) > TOLERANCE || System.Math.Abs(source.Y - other.Y) > TOLERANCE || System.Math.Abs(source.Z - other.Z) > TOLERANCE;

    public bool Equals(Point3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

    public override bool Equals(object obj) => obj is Point3 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
}