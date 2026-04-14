using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ACore.Abstractions.Transport;

/// <summary>
/// Wrapper around received data. Just for remember to return buffer to array pool back
/// </summary>
[DebuggerDisplay("[{mMemory.Length}] {mOwner != null ? \"(pooled)\" : \"\"}")]
public readonly struct Packet : IDisposable, IEquatable<Packet>
{
    private readonly IMemoryOwner<byte> mOwner;
    private readonly Memory<byte> mMemory;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Packet(Memory<byte> data)
    {
        mMemory = data;
        mOwner = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Packet(IMemoryOwner<byte> owner)
    {
        mOwner = owner ?? throw new ArgumentNullException(nameof(owner));
        mMemory = mOwner.Memory;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Packet(IMemoryOwner<byte> owner, Memory<byte> resized)
    {
        mOwner = owner ?? throw new ArgumentNullException(nameof(owner));
        mMemory = resized;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Packet Slice(int start, int length) => new (mOwner, mMemory.Slice(start, length));

    public void Dispose()
    {
        mOwner?.Dispose();
    }

    public ReadOnlyMemory<byte> Data => mMemory;

    public static Packet Empty { get; } = new (Memory<byte>.Empty);

    public static implicit operator ReadOnlyMemory<byte>(Packet packet) => packet.Data;

    public static implicit operator Packet(Memory<byte> memory) => new(memory);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool Equals(Packet other)
    {
        return Equals(mOwner, other.mOwner) && mMemory.Equals(other.mMemory);
    }

    public override bool Equals(object obj)
    {
        return obj is Packet other && Equals(other);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode()
    {
        return HashCode.Combine(mOwner, mMemory);
    }
}