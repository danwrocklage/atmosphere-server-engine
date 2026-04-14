using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace AGame.Time;

/// <summary>
/// Simple <see cref="GameTime"/> binary serialization to BSON
/// </summary>
internal class GameTimeBsonSerializer : StructSerializerBase<GameTime>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, GameTime value)
    {
        var source = new byte[8];
        var buffer = new Span<byte>(source)
        {
            [0] = value.Minutes,
            [1] = value.Hour,
            [2] = value.Day,
            [3] = (byte) value.Season
        };
        BitConverter.TryWriteBytes(buffer[4..], value.Year);
        
        context.Writer.WriteStartDocument();
        context.Writer.WriteName("_value");
        context.Writer.WriteBytes(source);
        context.Writer.WriteEndDocument();
    }

    public override GameTime Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        context.Reader.ReadStartDocument();
        context.Reader.ReadName(new Utf8NameDecoder());
        
        var rawValue = context.Reader.ReadBytes();
        var result = new GameTime
        {
            Minutes = rawValue[0],
            Hour = rawValue[1],
            Day = rawValue[2],
            Season = (Season) rawValue[3],
            Year = BitConverter.ToUInt32(rawValue, 4)
        };

        context.Reader.ReadEndDocument();

        return result;
    }
}