namespace ConsistencyClass;

using System;
using System.Collections.Generic;
using System.Linq;

using static RecordWithVersion;

internal class EventStore
{
    private readonly DatabaseCollection<EventStream> streams = Database.Collection<EventStream>();
    public List<Action<object>> Subscribers { get; } = [];

    public List<T> ReadEvents<T>(string streamId) =>
        ExistingEventStreamOrEmpty(streamId)
            .EventsOfType<T>();

    public Result AppendToStream<T>(string streamId, List<T> events, int expectedVersion = IgnoreVersionCheck) where T: notnull
    {
        var stream = ExistingEventStreamOrEmpty(streamId);
        var version = stream.Events.Count;

        var newEvents = events.Select(e =>
            EventEnvelope.From(streamId, e, ++version)
        ).ToList();

        var result = streams.Save(streamId, stream.Append(newEvents), expectedVersion);

        if (result == Result.Success)
        {
            // Note: this typically happens asynchronously
            // to not impact accidentally storing events
            Publish(newEvents);
        }

        return result;
    }

    public void Subscribe(Action<object> subscriber) =>
        Subscribers.Add(subscriber);

    private void Publish(List<EventEnvelope> events)
    {
        foreach (var handler in Subscribers)
        {
            foreach (var evt in events)
            {
                handler(evt.Data);
            }
        }
    }

    private EventStream ExistingEventStreamOrEmpty(string streamId) =>
        streams.Find(streamId) ?? EventStream.Empty(streamId);
}

internal record EventStream(string Id, List<EventEnvelope> Events)
{
    public static EventStream Empty(string id) => new(id, new List<EventEnvelope>());

    public EventStream Append(List<EventEnvelope> events) =>
        new(Id, Events.Concat(events).ToList());

    public List<T> EventsOfType<T>() =>
        Events
            .Select(e => e.Data)
            .OfType<T>()
            .ToList();
}


internal record EventMetadata(
    string StreamId,
    string EventType,
    Guid EventId,
    int Version,
    DateTime OccurredAt
)
{
    public static EventMetadata From(Type eventType, string streamId, int version) =>
        new(
            streamId,
            eventType.FullName ?? eventType.Name,
            Guid.NewGuid(),
            version,
            DateTime.UtcNow
        );
}

internal record EventEnvelope(
    object Data,
    EventMetadata Metadata)
{
    public static EventEnvelope From(string streamId, object eventObj, int version)
    {
        return new EventEnvelope(
            eventObj,
            EventMetadata.From(eventObj.GetType(), streamId, version)
        );
    }
}

