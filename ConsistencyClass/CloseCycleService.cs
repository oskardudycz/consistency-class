namespace ConsistencyClass;

internal class CloseCycleService(VirtualCreditCardDatabase virtualCreditCardDatabase)
{
    public Result Close(CardId cardId)
    {
        var card = virtualCreditCardDatabase.Find(cardId);
        var result = card.CloseCycle();
        virtualCreditCardDatabase.Save(card);
        return result;
    }
}
