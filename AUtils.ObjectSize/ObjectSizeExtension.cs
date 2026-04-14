using System.Collections;
using System.Runtime.InteropServices;

namespace AUtils.ObjectSize;

public static class ObjectSizeExtension
{
    private static readonly int sObjectSize = IntPtr.Size == 8 ? 24 : 12;
    private static readonly int sPointerSize = IntPtr.Size;

    /// <summary>
    /// Return size of object in heap
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static long GetMemorySize<T>(T obj)
    {
        if (obj == null)
            return sPointerSize;
        
        long memorySize = 0;
        var objType = typeof(T) == typeof(object) ? obj.GetType() : typeof(T);

        if (objType.IsValueType)
        {
            memorySize = Marshal.SizeOf(obj);
        }
        else if (obj is string stringObj)
        {
            memorySize = stringObj.Length * 2 + 6 + sObjectSize;
        }
        else if (obj is Array arr)
        {
            var elementType = objType.GetElementType();
            if (elementType.IsValueType)
            {
                long elementSize = Marshal.SizeOf(elementType);
                long elementCount = arr.LongLength;
                memorySize += elementSize * elementCount;
            }
            else
            {
                foreach (var element in arr)
                    memorySize += GetMemorySize(element) + sPointerSize;
            }

            memorySize += sObjectSize;
        }
        else if (obj is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var itemType = item.GetType();
                memorySize += item != null ? GetMemorySize(item) : 0;
                if (itemType.IsClass)
                    memorySize += sPointerSize;
            }

            memorySize += sObjectSize;
        }
        else if (objType.IsClass)
        {
            var properties = objType.GetProperties();
            foreach (var property in properties)
            {
                var valueObject = property.GetValue(obj);
                memorySize += valueObject != null ? GetMemorySize(valueObject) : 0;
                if (property.GetType().IsClass)
                    memorySize += sPointerSize;
            }

            memorySize += sObjectSize;
        }

        return memorySize;
    }
}