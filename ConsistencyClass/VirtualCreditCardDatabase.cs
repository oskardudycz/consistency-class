using System.Collections.Concurrent;

namespace ConsistencyClass;

internal class VirtualCreditCardDatabase
{
    private readonly ConcurrentDictionary<CardId, VirtualCreditCard> ownerships = new();

    public void Save(VirtualCreditCard card)
    {
        ownerships[card.Id] = card;
    }

    public VirtualCreditCard Find(CardId cardId)
    {
        return ownerships.TryGetValue(cardId, out var card)
            ? card
            : throw new InvalidOperationException($"Could not find card with id {cardId}");
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
