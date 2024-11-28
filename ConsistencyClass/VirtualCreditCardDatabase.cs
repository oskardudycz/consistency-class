namespace ConsistencyClass;

internal class VirtualCreditCardDatabase
{
    private readonly EventStore eventStore = new();

    public Result Save(VirtualCreditCard card)
    {
        var streamId = card.Id.Id.ToString();

        return eventStore.AppendToStream(
            streamId,
            card.DequeuePendingEvents()
        );
    }

    public VirtualCreditCard Find(CardId cardId)
    {
        var streamId = cardId.Id.ToString();

        var events = eventStore.ReadEvents<VirtualCreditCardEvent>(streamId);

        return VirtualCreditCard.Recreate(events);
    }
}

internal class OwnershipDatabase
{
    private readonly DatabaseCollection<Ownership> ownerships = Database.Collection<Ownership>();

    public Result Save(CardId cardId, Ownership ownership) =>
        ownerships.Save(cardId.Id.ToString(), ownership);

    public Ownership Find(CardId cardId) =>
        ownerships.Find(cardId.Id.ToString()) ?? Ownership.Empty();
}
