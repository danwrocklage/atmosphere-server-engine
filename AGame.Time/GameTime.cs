using AUtils.Sil;
using MongoDB.Bson.Serialization;

namespace AGame.Time;

/// <summary>
/// Game time description
/// </summary>
[Sil(120)]
public struct GameTime : IEquatable<GameTime>
{
    private const char DATE_DELIMITER = '.';
    private const char TIME_DELIMITER = ':';

    internal const int WORLD_SPEED = 12;
    internal const int MINUTES_IN_HOUR = 60; 
    internal const int HOURS_IN_DAY = 24;
    internal const int DAYS_IN_SEASON = 60;

    internal static readonly int SeasonsCount = Enum.GetNames<Season>().Length;

    static GameTime()
    {
        BsonSerializer.RegisterSerializer(new GameTimeBsonSerializer());
    }

    private Season mSeason;
    private byte mDay;
    private byte mHour;
    private byte mMinutes;

    private long TotalMinutes => 
        Minutes + 
        Hour * MINUTES_IN_HOUR + 
        Day * HOURS_IN_DAY * MINUTES_IN_HOUR + 
        (byte) Season * DAYS_IN_SEASON * HOURS_IN_DAY * MINUTES_IN_HOUR + 
        Year * SeasonsCount * DAYS_IN_SEASON * HOURS_IN_DAY * MINUTES_IN_HOUR;
        
    public uint Year { get; set; }

    public Season Season
    {
        get => mSeason;
        set
        {
            if ((byte) value < 1 || (byte) value >= SeasonsCount)
                throw new ArgumentException();
            mSeason = value;
        }
    }

    public byte Day
    {
        get => mDay;
        set
        {
            if (value is < 1 or > DAYS_IN_SEASON)
                throw new ArgumentException();
            mDay = value;
        }
    }
        
    public byte Hour
    {
        get => mHour;
        set
        {
            if (value >= HOURS_IN_DAY)
                throw new ArgumentException();
            mHour = value;
        }
    }

    public byte Minutes
    {
        get => mMinutes;
        set
        {
            if (value >= MINUTES_IN_HOUR)
                throw new ArgumentException();
            mMinutes = value;
        }
    }
        

    public static GameTime Parse(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentNullException(nameof(input));

        var dateAndTime = input.Split(' ');
        if (dateAndTime.Length != 2)
            throw new FormatException();

        var dateSplited = dateAndTime[0].Split(DATE_DELIMITER);
        if (dateSplited.Length != 3)
            throw new FormatException();
            
        var timeSplited = dateAndTime[1].Split(TIME_DELIMITER);
        if (timeSplited.Length != 2)
            throw new FormatException();

        return new GameTime
        {
            Day = byte.Parse(dateSplited[0]),
            Season = (Season) byte.Parse(dateSplited[1]),
            Year = uint.Parse(dateSplited[2]),
            Hour = byte.Parse(timeSplited[0]),
            Minutes = byte.Parse(timeSplited[1])
        };
    }

    public static bool TryParse(string input, out GameTime value)
    {
        value = Empty;
        if (string.IsNullOrEmpty(input))
            return false;
            
        var dateAndTime = input.Split(' ');
        if (dateAndTime.Length != 2)
            return false;

        var dateSplited = dateAndTime[0].Split(DATE_DELIMITER);
        if (dateSplited.Length != 3)
            return false;

        var timeSplited = dateAndTime[1].Split(TIME_DELIMITER);
        if (timeSplited.Length != 2)
            return false;

        if (!byte.TryParse(dateSplited[0], out var day) ||
            !byte.TryParse(dateSplited[1], out var season) ||
            !uint.TryParse(dateSplited[2], out var year) ||
            !byte.TryParse(timeSplited[0], out var hour) ||
            !byte.TryParse(timeSplited[1], out var minutes) ||
            day > DAYS_IN_SEASON ||
            season > SeasonsCount ||
            hour > HOURS_IN_DAY ||
            minutes > MINUTES_IN_HOUR)
            return false;

        value = new GameTime
        {
            Day = day,
            Hour = hour,
            Minutes = minutes,
            Season = (Season) season,
            Year = year
        };

        return true;
    }

    public static GameTime Empty => new() { Day = 0, Hour = 0, Minutes = 0, Season = 0, Year = 0 };

    public override string ToString() => 
        $"{Day:00}{DATE_DELIMITER.ToString()}{Season:D}{DATE_DELIMITER.ToString()}{Year.ToString()} {Hour.ToString()}{TIME_DELIMITER.ToString()}{Minutes.ToString()}";

    public bool Equals(GameTime other) => 
        Year == other.Year && Season == other.Season && Day == other.Day && Hour == other.Hour && Minutes == other.Minutes;
    public override bool Equals(object obj) => 
        obj is GameTime other && Equals(other);
        
    public override int GetHashCode() => HashCode.Combine(Year, Season, Day, Hour, Minutes);
        
    public static bool operator ==(GameTime left, GameTime right) => left.Equals(right);
    public static bool operator !=(GameTime left, GameTime right) => !left.Equals(right);
    public static bool operator >(GameTime left, GameTime right) => left.TotalMinutes > right.TotalMinutes;
    public static bool operator <(GameTime left, GameTime right) => left.TotalMinutes < right.TotalMinutes;
    public static bool operator >=(GameTime left, GameTime right) => left.TotalMinutes >= right.TotalMinutes;
    public static bool operator <=(GameTime left, GameTime right) => left.TotalMinutes <= right.TotalMinutes;

    public static GameTime operator +(GameTime left, GameTime right)
    {
        var result = new GameTime();

        var value = left.mMinutes + right.mMinutes;
        var plus = value >= MINUTES_IN_HOUR;
        result.mMinutes = plus ? (byte)(value - MINUTES_IN_HOUR) : (byte)value;

        value = left.mHour + right.mHour + (plus ? 1 : 0);
        plus = value >= HOURS_IN_DAY;
        result.mHour = plus ? (byte)(value - HOURS_IN_DAY) : (byte)value;

        value = left.mDay + right.mDay + (plus ? 1 : 0);
        plus = value >= DAYS_IN_SEASON;
        result.mDay = plus ? (byte)(value - DAYS_IN_SEASON) : (byte)value;

        value = (byte)left.mSeason + (byte)right.mSeason + (plus ? 1 : 0);
        plus = value >= SeasonsCount;
        result.mSeason = (Season)(plus ? (byte)(value - SeasonsCount) : (byte)value);
        result.Year = (uint)(left.Year + right.Year + (plus ? 1 : 0));

        return result;
    }

    public static GameTime operator -(GameTime left, GameTime right)
    {
        var result = new GameTime();

        var value = left.mMinutes - right.mMinutes;
        var minus = value < 0;
        result.mMinutes = minus ? (byte)0 : (byte)value;

        value = left.mHour - right.mHour - (minus ? 1 : 0);
        minus = value < 0;
        result.mHour = minus ? (byte)0 : (byte)value;

        value = left.mDay - right.mDay - (minus ? 1 : 0);
        minus = value < 0;
        result.mDay = minus ? (byte)0 : (byte)value;

        value = (byte)left.mSeason - (byte)right.mSeason - (minus ? 1 : 0);
        minus = value < 0;
        result.mSeason = minus ? 0 : (Season)value;
        result.Year = (uint)(left.Year - right.Year - (minus ? 1 : 0));

        return result;
    }
}