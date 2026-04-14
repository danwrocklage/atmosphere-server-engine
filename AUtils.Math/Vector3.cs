using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace AUtils.Math;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public ref struct Vector3
{
    private const float TOLERANCE = 0.0001f;

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    /// <summary>
    /// Gets a value indicting whether this instance is normalized.
    /// </summary>
    public readonly bool IsNormalized => MathF.Abs(X * X + Y * Y + Z * Z - 1f) < TOLERANCE;

    /// <summary>
    /// Gets or sets the component at the specified index.
    /// </summary>
    /// <value>The value of the X, Y, or Z component, depending on the index.</value>
    /// <param name="index">The index of the component to access. Use 0 for the X component, 1 for the Y component, and 2 for the Z component.</param>
    /// <returns>The value of the component at the specified index.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 2].</exception>
    public float this[int index]
    {
        get =>
            index switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                _ => throw new ArgumentOutOfRangeException(nameof(index),
                    "Indices for Vector3 run from 0 to 2, inclusive.")
            };

        set
        {
            switch (index)
            {
                case 0: X = value; break;
                case 1: Y = value; break;
                case 2: Z = value; break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index),
                        "Indices for Vector3 run from 0 to 2, inclusive.");
            }
        }
    }

    /// <summary>
    /// Calculates the length of the vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// Calculates the squared length of the vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float LengthSquared() => X * X + Y * Y + Z * Z;

    /// <summary>
    /// Converts the vector into a unit vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Normalize()
    {
        var length = Length();
        if (!(length > TOLERANCE))
            return;
        var inv = 1.0f / length;
        X *= inv;
        Y *= inv;
        Z *= inv;
    }
    
    public float[] ToArray() => new[] {X, Y, Z};

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
    /// </returns>
    public override int GetHashCode() => X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode();

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

    /// <summary>
    /// Determines whether the specified is equal to this instance.
    /// </summary>
    public bool Equals(Vector3 other) =>
        MathF.Abs(other.X - X) < TOLERANCE &&
        MathF.Abs(other.Y - Y) < TOLERANCE &&
        MathF.Abs(other.Z - Z) < TOLERANCE;

    /// <summary>
    /// Calculates the cross product of two vectors.
    /// </summary>
    public static Vector3 Cross(in Vector3 left, in Vector3 right) =>
        new()
        {
            X = left.Y * right.Z - left.Z * right.Y,
            Y = left.Z * right.X - left.X * right.Z,
            Z = left.X * right.Y - left.Y * right.X
        };

    /// <summary>
    /// Calculates the dot product of two vectors.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Vector3 left, Vector3 right) => left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    public static Vector3 FromArray(float[] values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return values.Length switch
        {
            0 => new Vector3(),
            1 => new Vector3 {X = values[0]},
            2 => new Vector3 {X = values[0], Y = values[1]},
            _ => new Vector3 {X = values[0], Y = values[1], Z = values[2]}
        };
    }
    
    #region Operators

    /// <summary>
    /// Adds two vectors.
    /// </summary>
    /// <param name="left">The first vector to add.</param>
    /// <param name="right">The second vector to add.</param>
    /// <returns>The sum of the two vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator +(Vector3 left, Vector3 right) =>
        new() {X = left.X + right.X, Y = left.Y + right.Y, Z = left.Z + right.Z};

    /// <summary>
    /// Assert a vector (return it unchanged).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator +(Vector3 value) => value;

    /// <summary>
    /// Subtracts two vectors.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator -(Vector3 left, Vector3 right) =>
        new() {X = left.X - right.X, Y = left.Y - right.Y, Z = left.Z - right.Z};

    /// <summary>
    /// Reverses the direction of a given vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator -(Vector3 value) => new() {X = -value.X, Y = -value.Y, Z = -value.Z};

    /// <summary>
    /// Scales a vector by the given value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(float scale, Vector3 value) =>
        new() {X = value.X * scale, Y = value.Y * scale, Z = value.Z * scale};

    /// <summary>
    /// Scales a vector by the given value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(Vector3 value, float scale) =>
        new() {X = value.X * scale, Y = value.Y * scale, Z = value.Z * scale};

    /// <summary>
    /// Modulates a vector with another by performing component-wise multiplication.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(Vector3 left, Vector3 right) =>
        new() {X = left.X * right.X, Y = left.Y * right.Y, Z = left.Z * right.Z};

    /// <summary>
    /// Adds a vector with the given value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator +(Vector3 value, float scale) =>
        new() {X = value.X + scale, Y = value.Y + scale, Z = value.Z + scale};

    /// <summary>
    /// Substracts a vector by the given value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator -(Vector3 value, float scale) =>
        new() {X = value.X - scale, Y = value.Y - scale, Z = value.Z - scale};

    /// <summary>
    /// Divides a numerator by a vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator /(float numerator, Vector3 value) => new()
        {X = numerator / value.X, Y = numerator / value.Y, Z = numerator / value.Z};

    /// <summary>
    /// Scales a vector by the given value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator /(Vector3 value, float scale) =>
        new() {X = value.X / scale, Y = value.Y / scale, Z = value.Z / scale};

    /// <summary>
    /// Divides a vector by the given vector, component-wise.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator /(Vector3 value, Vector3 by) =>
        new() {X = value.X / by.X, Y = value.Y / by.Y, Z = value.Z / by.Z};

    /// <summary>
    /// Tests for equality between two objects.
    /// </summary>
    public static bool operator ==(Vector3 left, Vector3 right) => left.Equals(right);

    /// <summary>
    /// Tests for inequality between two objects.
    /// </summary>
    public static bool operator !=(Vector3 left, Vector3 right) => !left.Equals(right);

    #endregion

    public static implicit operator Point3(Vector3 source) => new() {X = source.X, Y = source.Y, Z = source.Z};
    public static implicit operator Vector3(Point3 source) => new() {X = source.X, Y = source.Y, Z = source.Z};
}