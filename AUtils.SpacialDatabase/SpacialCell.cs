using System.Collections;
using System.Diagnostics;
using AUtils.Math;

namespace AUtils.SpacialDatabase;

[DebuggerDisplay("Cell (Items = {mItems.Count})")]
public class SpacialCell<T> : IEnumerable<KeyValuePair<T, Point3>>
{
    public static SpacialCell<T> Empty => new();

    private readonly Dictionary<T, Point3> mItems = new();

    public bool IsEmpty => mItems.Count == 0;

    public void AddOrUpdate(T item, Point3 point)
    {
        if (!mItems.ContainsKey(item))
            mItems.Add(item, point);
        else
            mItems[item] = point;
    }

    public IReadOnlyCollection<T> Items => mItems.Keys;

    public Dictionary<T, Point3> GetByRect(Rect rect)
    {
        var result = new Dictionary<T, Point3>();
        foreach (var point in mItems)
        {
            if (rect.Contains(point.Value))
                result.Add(point.Key, point.Value);
        }

        return result;
    }

    public Point3 Get(T item) => 
        mItems.TryGetValue(item, out var point) ? point : default;

    public void Remove(T item) => mItems.Remove(item);
    public IEnumerator<KeyValuePair<T, Point3>> GetEnumerator() => 
        ((IEnumerable<KeyValuePair<T, Point3>>) mItems).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static explicit operator Dictionary<T, Point3>(SpacialCell<T> spacialCell) => spacialCell.mItems;
}