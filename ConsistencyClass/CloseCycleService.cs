namespace ConsistencyClass;

internal class CloseCycleService(VirtualCreditCardDatabase virtualCreditCardDatabase)
{
    public Result Close(CardId cardId)
    {
        var card = virtualCreditCardDatabase.Find(cardId);
        var expectedVersion = card.Version;

        var result = card.CloseCycle();

        return result == Result.Success
            ? virtualCreditCardDatabase.Save(card, expectedVersion)
            : result;
    }
}
