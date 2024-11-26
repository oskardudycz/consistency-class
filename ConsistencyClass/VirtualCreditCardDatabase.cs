using System.Collections.Concurrent;

namespace ConsistencyClass;

internal class VirtualCreditCardDatabase
{
    private readonly ConcurrentDictionary<CardId, List<EventEnvelope>> cards = new();

    public void Save(VirtualCreditCard card)
    {
        if (!cards.TryGetValue(card.Id, out var stream))
        {
            stream = new List<EventEnvelope>();
            cards[card.Id] = stream;
        }

        var version = stream.Count;

        var newEvents = card.DequeuePendingEvents()
            .Select(e => EventEnvelope.From(card.Id.Id.ToString(), e, ++version))
            .ToList();

        stream.AddRange(newEvents);
    }

    public VirtualCreditCard Find(CardId cardId)
    {
        if (!cards.TryGetValue(cardId, out var stream))
            throw new InvalidOperationException($"Could not find card with id {cardId}");

        var events = stream
            .Select(envelope => envelope.Data)
            .OfType<VirtualCreditCardEvent>()
            .ToList();

        return VirtualCreditCard.Recreate(events);
    }
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

internal class OwnershipDatabase
{
    private readonly ConcurrentDictionary<CardId, Ownership> ownerships = new();

    public void Save(CardId cardId, Ownership ownership)
    {
        ownerships[cardId] = ownership;
    }

    public Ownership Find(CardId cardId)
    {
        return ownerships.TryGetValue(cardId, out var ownership) ? ownership : Ownership.Empty();
    }
}
