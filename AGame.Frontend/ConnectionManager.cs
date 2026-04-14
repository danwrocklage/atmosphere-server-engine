using System.Collections.Concurrent;
using ACore.Abstractions.Transport;

namespace AGame.Frontend;

public class ConnectionManager
{
    private class Description
    {
        public IConnection Connection { get; set; }
        public DateTime LastActiveAt { get; set; }
    }

    private readonly ConcurrentDictionary<Guid, Description> mConnectionDescriptions = new();

    public void Add(Guid entityId, IConnection connection)
    {
        if (connection == null) 
            throw new ArgumentNullException(nameof(connection));

        mConnectionDescriptions.TryAdd(entityId,
            new Description {Connection = connection, LastActiveAt = DateTime.UtcNow});
    }

    public void Update(Guid entityId)
    {
        if(!mConnectionDescriptions.TryGetValue(entityId, out var description))
            return;
        
        description.LastActiveAt = DateTime.UtcNow;
    }
}