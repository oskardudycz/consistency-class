namespace ConsistencyClass;

internal class VirtualCreditCardDatabase(EventStore eventStore)
{
    public Result Save(VirtualCreditCard card, int expectedVersion)
    {
        var streamId = card.Id.ContractId.ToString();

        return eventStore.AppendToStream(
            streamId,
            card.DequeuePendingEvents(),
            expectedVersion
        );
    }

    public VirtualCreditCard Find(CardId cardId)
    {
        var streamId = cardId.ContractId.ToString();

        var events = eventStore.ReadEvents<VirtualCreditCardEvent>(streamId);

        return VirtualCreditCard.Recreate(events);
    }
}

internal class BillingCycleDatabase(EventStore eventStore)
{
    public Result Save(BillingCycle cycle, int expectedVersion)
    {
        var streamId = cycle.Id.ToString();

        return eventStore.AppendToStream(
            streamId,
            cycle.DequeuePendingEvents(),
            expectedVersion
        );
    }

   public BillingCycle Find(BillingCycleId cycleId)
    {
        var streamId = cycleId.ToString();

        var events = eventStore.ReadEvents<BillingCycleEvent>(streamId);

        return BillingCycle.Recreate(events);
    }
}

internal class OwnershipDatabase
{
    private readonly DatabaseCollection<Ownership> ownerships = Database.Collection<Ownership>();

    public Result Save(CardId cardId, Ownership ownership, int expectedVersion) =>
        ownerships.Save(cardId.ToString(), ownership, expectedVersion);

    public Ownership Find(CardId cardId) =>
        ownerships.Find(cardId.ToString()) ?? Ownership.Empty();
}
