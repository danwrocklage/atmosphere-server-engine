using System.Globalization;

namespace ACore.Patching;

/// <summary>
/// Application state modification item
/// </summary>
public abstract class Patch
{
    /// <summary>
    /// Unique sortable patch name. Must be format yyyyMMdd_number
    /// </summary>
    public abstract string Order { get; }
        
    /// <summary>
    /// Patch category
    /// </summary>
    public abstract string Category { get; }
        
    /// <summary>
    /// Apply patch method
    /// </summary>
    public abstract Task Up();

    /// <summary>
    /// Revert patch method
    /// </summary>
    public abstract Task Down();

    internal bool TryParseOrder(out (DateTime Date, int Number) result)
    {
        result = (default, default);
        if (string.IsNullOrEmpty(Order))
            return false;
            
        var dateAndNum = Order.Split('_');
        if (dateAndNum.Length != 2)
            return false;

        return 
            DateTime.TryParseExact(
                dateAndNum[0], 
                "yyyyMMdd", 
                CultureInfo.InvariantCulture, 
                DateTimeStyles.None, 
                out result.Date) && 
            int.TryParse(dateAndNum[1], out result.Number);
    }
}