using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AUtils.Math;

namespace AUtils.SpacialDatabase;

[DebuggerDisplay("Spacial Db (Cells = {mCells.Count})")]
public class SpacialDatabase<T> : IEnumerable<SpacialCell<T>>
{
    private readonly ConcurrentDictionary<(ushort, ushort), SpacialCell<T>> mCells = new();
    private readonly ConcurrentDictionary<T, (ushort, ushort)> mItemsCellIndex = new();

    public SpacialDatabase(float cellSize)
    {
        CellSize = cellSize;
    }

    public float CellSize { get; }

    public void AddOrUpdate(T item, Point3 position)
    {
        var cell = GetCellIndex(position);
        
        if (mItemsCellIndex.TryGetValue(item, out var oldCell) && oldCell != cell)
            this[oldCell.Item1, oldCell.Item2].Remove(item);

        this[cell.Item1, cell.Item2]
            .AddOrUpdate(item, new Point3(position.X, position.Y, position.Z));
        
        mItemsCellIndex.AddOrUpdate(item, cell, (_, _) => GetCellIndex(position));
    }

    public Point3? Get(T item)
    {
        if (!mItemsCellIndex.TryGetValue(item, out var cell))
            return null;
        return this[cell.Item1, cell.Item2].Get(item);
    }

    public void Remove(T item)
    {
        if (mItemsCellIndex.TryRemove(item, out var cell))
            this[cell.Item1, cell.Item2].Remove(item);
    }

    public IEnumerable<T> GetByRadius(Point3 center, double radius)
    {
        var (xIndex, yIndex) = GetCellIndex(center);
        var rIndex = (ushort) System.Math.Floor(radius / CellSize);
        return Array.Empty<T>();
    }

    public IReadOnlyDictionary<T, Point3> GetByRect(Point3 center, float centerToTop, float centerToLeft)
    {
        var selected = new Rect(center, centerToTop, centerToLeft, true);
        
        var (xStartIndex, yStartIndex) = GetCellIndex(selected.TopLeft);
        var (xEndIndex, yEndIndex) = GetCellIndex(selected.BottomRight);

        var result = new Dictionary<T, Point3>();
        for (var i = xStartIndex; i <= xEndIndex; i++)
        {
            for (var j = yStartIndex; j <= yEndIndex; j++)
            {
                var bucket = this[i, j];
                if(bucket.IsEmpty)
                    continue;

                var cellRect = new Rect(new Point(i * CellSize, j * CellSize),
                    new Point((i + 1) * CellSize, (j + 1) * CellSize));

                var topLeft = new Point
                {
                    X = MathF.Max(i * CellSize, selected.TopLeft.X),
                    Y = MathF.Max(j * CellSize, selected.TopLeft.Y)
                };
                var bottomRight = new Point
                {
                    X = MathF.Min((i + 1) * CellSize, selected.BottomRight.X),
                    Y = MathF.Min((j + 1) * CellSize, selected.BottomRight.Y)
                };
                var resultRect = new Rect(topLeft, bottomRight);

                var cellResult = resultRect == cellRect ? 
                    (Dictionary<T, Point3>)bucket : 
                    bucket.GetByRect(new Rect(topLeft, bottomRight));
                
                foreach (var item in cellResult)
                    result.Add(item.Key, item.Value);
            }
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (ushort X, ushort Y) GetCellIndex(Point position)
    {
        var xIndex = (ushort) MathF.Floor(position.X / CellSize);
        var yIndex = (ushort) MathF.Floor(position.Y / CellSize);
        return (xIndex, yIndex);
    }

    public SpacialCell<T> this[ushort x, ushort y] => 
        mCells.GetOrAdd((x, y), _ => SpacialCell<T>.Empty);

    public IEnumerator<SpacialCell<T>> GetEnumerator() => mCells.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}